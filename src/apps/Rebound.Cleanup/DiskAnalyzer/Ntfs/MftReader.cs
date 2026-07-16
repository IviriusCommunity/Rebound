// Copyright (C) Ivirius(TM) Community 2020 - 2026. All Rights Reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using TerraFX.Interop.Windows;
using static TerraFX.Interop.Windows.Windows;

namespace Rebound.Cleanup.DiskAnalyzer.Ntfs;

internal sealed unsafe class ParallelMftScanner
{
    private const uint FSCTL_GET_NTFS_VOLUME_DATA = 0x00090064;
    private const uint FSCTL_GET_NTFS_FILE_RECORD = 0x00090068;

    public static void Execute()
    {
        using var volume = NtfsVolume.Open('C');
        HANDLE hVolume = volume._handle; // Assuming NtfsVolume exposes raw native HANDLE

        // ==========================================================
        // 1. QUERY EXACT VOLUME DATA BOUNDS
        // ==========================================================
        NTFS_VOLUME_DATA_BUFFER volData = default;
        uint bytesReturned = 0;
        int volSuccess = DeviceIoControl(hVolume, FSCTL_GET_NTFS_VOLUME_DATA, null, 0, &volData, (uint)sizeof(NTFS_VOLUME_DATA_BUFFER), &bytesReturned, null);
        if (volSuccess == 0) throw new Win32Exception((int)GetLastError());

        uint recordSize = volData.BytesPerFileRecordSegment;
        uint maxRecords = (uint)(volData.MftValidDataLength / recordSize);

        // Allocate unified unmanaged memory heap slab
        nuint allocationSize = (nuint)(maxRecords * (uint)sizeof(DiskNode));
        DiskNode* flatTree = (DiskNode*)NativeMemory.AllocZeroed(allocationSize);
        if (flatTree == null) return;

        // Tracking primitives
        long completedRecordsCounter = 0;
        int isScanningFinished = 0;

        // ==========================================================
        // 2. LIVE PROGRESS TRACKING BACKGROUND THREAD
        // ==========================================================
        Thread progressThread = new Thread(() =>
        {
            while (Volatile.Read(ref isScanningFinished) == 0)
            {
                long completed = Volatile.Read(ref completedRecordsCounter);
                double percentage = ((double)completed / maxRecords) * 100.0;

                Console.Write($"\r[Rebound Disk Cleanup] Indexing Progress: {percentage:F2}% ({completed:N0} / {maxRecords:N0} records)...");
                Thread.Sleep(100);
            }
            Console.WriteLine("\r[Rebound Disk Cleanup] Indexing Progress: 100.00% Processing Finished!          ");
        })
        { IsBackground = true };
        progressThread.Start();

        try
        {
            // ==========================================================
            // 3. MULTI-THREADED PARALLEL BATCH EXTRACTION (PHASE 1)
            // ==========================================================
            // Break our records down into uniform block steps to maximize thread pool distribution
            int recordStrideChunk = 4000;
            int totalChunks = (int)Math.Ceiling((double)maxRecords / recordStrideChunk);

            Parallel.For(0, totalChunks, new ParallelOptions { MaxDegreeOfParallelism = Environment.ProcessorCount }, chunkIndex =>
            {
                uint startRecord = (uint)(chunkIndex * recordStrideChunk);
                uint endRecord = Math.Min(startRecord + (uint)recordStrideChunk, maxRecords);

                // Re-use a single stack buffer per thread worker for the API payload
                uint outputBufferSize = (uint)sizeof(NTFS_FILE_RECORD_OUTPUT_BUFFER) + recordSize;
                byte* stackBuffer = stackalloc byte[(int)outputBufferSize];
                NTFS_FILE_RECORD_INPUT_BUFFER input = default;

                long localCompletedCount = 0;

                for (uint recordId = startRecord; recordId < endRecord; recordId++)
                {
                    input.FileReferenceNumber = (long)recordId;
                    uint returned = 0;

                    int success = DeviceIoControl(hVolume, FSCTL_GET_NTFS_FILE_RECORD, &input, (uint)sizeof(NTFS_FILE_RECORD_INPUT_BUFFER), stackBuffer, outputBufferSize, &returned, null);
                    localCompletedCount++;

                    // If a record is unallocated or locked, skip it cleanly without breaking the chunk
                    if (success == 0) continue;

                    byte* recordPayloadPtr = stackBuffer + sizeof(NTFS_FILE_RECORD_OUTPUT_BUFFER);
                    NtfsFileRecordHeader* header = (NtfsFileRecordHeader*)recordPayloadPtr;
                    if (header->Magic != 0x454C4946) continue; // Must be "FILE"

                    byte* p = recordPayloadPtr + header->FirstAttributeOffset;
                    ulong parentRecordId = 0;
                    long fileSize = 0;
                    byte isDirectory = (byte)((header->Flags & 0x02) != 0 ? 1 : 0);

                    while (true)
                    {
                        NtfsAttributeHeader* attribute = (NtfsAttributeHeader*)p;
                        if (attribute->Type == 0xFFFFFFFF || attribute->Length == 0) break;

                        if (attribute->Type == 0x30) // $FILE_NAME
                        {
                            ushort payloadOffset = *(ushort*)((byte*)attribute + 20);
                            byte* payload = (byte*)attribute + payloadOffset;
                            parentRecordId = (*(ulong*)payload) & 0xFFFFFFFFFFFF;
                        }
                        else if (attribute->Type == 0x80) // $DATA
                        {
                            byte isNonResident = *((byte*)attribute + 8);
                            fileSize = (isNonResident == 0) ? *(uint*)((byte*)attribute + 16) : *(long*)((byte*)attribute + 48);
                        }
                        p += attribute->Length;
                    }

                    // Store elements into unmanaged memory pool
                    flatTree[recordId].ParentId = parentRecordId;
                    flatTree[recordId].Size = fileSize;
                    flatTree[recordId].IsDirectory = isDirectory;

                    // ==========================================================
                    // 4. ATOMIC HIERARCHY WIRE-UP (PHASE 2)
                    // ==========================================================
                    if (parentRecordId > 0 && parentRecordId < maxRecords && parentRecordId != recordId)
                    {
                        ulong* firstChildPtr = &flatTree[parentRecordId].FirstChildId;
                        ulong oldFirstChild;

                        // Concurrent atomic linked-list injection loop
                        do
                        {
                            oldFirstChild = Volatile.Read(ref *firstChildPtr);
                            flatTree[recordId].NextSiblingId = oldFirstChild;
                        }
                        while (Interlocked.CompareExchange(ref *(long*)firstChildPtr, (long)recordId, (long)oldFirstChild) != (long)oldFirstChild);
                    }
                }

                // Push thread local completion numbers to global tracker atomic primitive
                Interlocked.Add(ref completedRecordsCounter, localCompletedCount);
            });

            // Signal progress monitor thread loop to break cleanly
            Volatile.Write(ref isScanningFinished, 1);
            progressThread.Join();

            // ==========================================================
            // 5. CACHE-ALIGNED BACKWARDS SIZE ROLL-UP (PHASE 3)
            // ==========================================================
            // Executed linearly on a single core because it hits raw memory cache layout perfectly
            for (int i = (int)maxRecords - 1; i >= 0; i--)
            {
                ulong parent = flatTree[i].ParentId;
                long size = flatTree[i].Size;

                if (parent > 0 && parent < maxRecords && parent != (ulong)i)
                {
                    flatTree[parent].Size += size;
                }
            }

            // ==========================================================
            // VERIFICATION PRINT OUT
            // ==========================================================
            double totalDriveGb = flatTree[5].Size / (1024.0 * 1024.0 * 1024.0);
            Console.WriteLine($"\n[SUCCESS] Engine finished tracking directory allocation mapping framework!");
            Console.WriteLine($"[INFO] Parsed Drive Size: {totalDriveGb:N2} GB mapped across Master File Table index.");

        }
        finally
        {
            NativeMemory.Free(flatTree);
        }
    }
}