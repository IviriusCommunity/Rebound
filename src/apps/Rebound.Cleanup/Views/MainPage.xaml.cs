// Copyright (C) Ivirius(TM) Community 2020 - 2025. All Rights Reserved.
// Licensed under the MIT License.

using CommunityToolkit.Mvvm.Input;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Rebound.Cleanup.DiskAnalyzer.Ntfs;
using Rebound.Cleanup.ViewModels;
using Rebound.Core;
using Rebound.Core.Native.Wrappers;
using System;
using System.ComponentModel;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using TerraFX.Interop.Windows;
using WinUIEx;
using static TerraFX.Interop.Windows.Windows;

namespace Rebound.Cleanup.Views;

internal sealed partial class MainPage : Page
{
    private MainViewModel ViewModel { get; } = new MainViewModel();

    public MainPage()
    {
        InitializeComponent();
        Loaded += MainPage_Loaded;
    }

    private async void MainPage_Loaded(object sender, RoutedEventArgs e)
    {
        Loaded -= MainPage_Loaded;

        await Task.Yield(); // Ensure the UI is fully loaded before proceeding

        await CleanupReloadItems().ConfigureAwait(false);
    }

    [RelayCommand]
    public async Task RefreshAsync()
        => await CleanupReloadItems().ConfigureAwait(false);

    [RelayCommand]
    public static void Close() => App.MainWindow.Close();

    private async Task CleanupReloadItems()
    {
        await ViewModel.RefreshCleanupListAsync(ViewModel.DriveItems[ViewModel.SelectedDriveIndex]).ConfigureAwait(true);
    }

    private unsafe void Stuff()
    {
        using var volume = NtfsVolume.Open('C');
        HANDLE hVolume = volume._handle;

        // ==========================================================
        // STEP 1: Query Exact Volume Bounds (No More Guessing)
        // ==========================================================
        NTFS_VOLUME_DATA_BUFFER volData = default;
        uint bytesReturned = 0;

        int volSuccess = DeviceIoControl(
            hVolume,
            FSCTL.FSCTL_GET_NTFS_VOLUME_DATA,
            null, 0,
            &volData, (uint)sizeof(NTFS_VOLUME_DATA_BUFFER),
            &bytesReturned,
            null
        );

        if (volSuccess == 0)
            throw new Win32Exception((int)GetLastError());

        uint recordSize = volData.BytesPerFileRecordSegment;
        uint maxRecords = (uint)(volData.MftValidDataLength / recordSize);

        // Allocate unified unmanaged memory slab
        nuint allocationSize = (nuint)(maxRecords * (uint)sizeof(DiskNode));
        DiskNode* flatTree = (DiskNode*)NativeMemory.AllocZeroed(allocationSize);

        if (flatTree == null)
            return;

        long completedRecordsCounter = 0;
        int isScanningFinished = 0;

        // ==========================================================
        // STEP 2: Start the Live Progress Thread
        // ==========================================================
        Thread progressThread = new Thread(() =>
        {
            while (Volatile.Read(ref isScanningFinished) == 0)
            {
                long completed = Volatile.Read(ref completedRecordsCounter);
                double percentage = ((double)completed / maxRecords) * 100.0;

                ReboundLogger.WriteToLog(
                    "Indexing",
                    $"Progress: {percentage:F2}% ({completed:N0} / {maxRecords:N0} records)",
                    LogMessageSeverity.Warning
                );
                Thread.Sleep(250); // Log 4 times a second to keep output clean
            }
        })
        { IsBackground = true };
        progressThread.Start();

        var sw = System.Diagnostics.Stopwatch.StartNew();

        try
        {
            // ==========================================================
            // STEP 3: Multi-Threaded Parallel Extraction (Saturate CPU)
            // ==========================================================
            int recordStrideChunk = 4000;
            int totalChunks = (int)Math.Ceiling((double)maxRecords / recordStrideChunk);

            Parallel.For(0, totalChunks, new ParallelOptions { MaxDegreeOfParallelism = Environment.ProcessorCount }, chunkIndex =>
            {
                uint startRecord = (uint)(chunkIndex * recordStrideChunk);
                uint endRecord = Math.Min(startRecord + (uint)recordStrideChunk, maxRecords);

                uint outputBufferSize = (uint)sizeof(NTFS_FILE_RECORD_OUTPUT_BUFFER) + recordSize;
                byte* stackBuffer = stackalloc byte[(int)outputBufferSize];
                NTFS_FILE_RECORD_INPUT_BUFFER input = default;

                long localCompletedCount = 0;

                for (uint recordId = startRecord; recordId < endRecord; recordId++)
                {
                    input.FileReferenceNumber = (long)recordId;
                    uint returned = 0;

                    int success = DeviceIoControl(hVolume, FSCTL.FSCTL_GET_NTFS_FILE_RECORD, &input, (uint)sizeof(NTFS_FILE_RECORD_INPUT_BUFFER), stackBuffer, outputBufferSize, &returned, null);
                    localCompletedCount++;

                    if (success == 0) continue;

                    byte* recordPayloadPtr = stackBuffer + sizeof(NTFS_FILE_RECORD_OUTPUT_BUFFER);
                    NtfsFileRecordHeader* header = (NtfsFileRecordHeader*)recordPayloadPtr;
                    if (header->Magic != 0x454C4946) continue;

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

                    flatTree[recordId].ParentId = parentRecordId;
                    flatTree[recordId].Size = fileSize;
                    flatTree[recordId].IsDirectory = isDirectory;

                    // Atomic list injection link
                    if (parentRecordId > 0 && parentRecordId < maxRecords && parentRecordId != recordId)
                    {
                        ulong* firstChildPtr = &flatTree[parentRecordId].FirstChildId;
                        ulong oldFirstChild;
                        do
                        {
                            oldFirstChild = Volatile.Read(ref *firstChildPtr);
                            flatTree[recordId].NextSiblingId = oldFirstChild;
                        }
                        while (Interlocked.CompareExchange(ref *(long*)firstChildPtr, (long)recordId, (long)oldFirstChild) != (long)oldFirstChild);
                    }
                }

                Interlocked.Add(ref completedRecordsCounter, localCompletedCount);
            });

            // Kill progress monitor thread
            Volatile.Write(ref isScanningFinished, 1);
            progressThread.Join();

            // ==========================================================
            // STEP 4: The WizTree Size Roll-Up Pass
            // ==========================================================
            for (int i = (int)maxRecords - 1; i >= 0; i--)
            {
                ulong parent = flatTree[i].ParentId;
                long size = flatTree[i].Size;

                if (parent > 0 && parent < maxRecords && parent != (ulong)i)
                {
                    flatTree[parent].Size += size;
                }
            }

            sw.Stop();

            // ==========================================================
            // STEP 5: Print Complete Performance Stats
            // ==========================================================
            double totalDriveGb = flatTree[5].Size / (1024.0 * 1024.0 * 1024.0);
            ReboundLogger.WriteToLog("Success", $"Indexed {maxRecords:N0} records in {sw.ElapsedMilliseconds} ms!", LogMessageSeverity.Warning);
            ReboundLogger.WriteToLog("Verified", $"Total tracked size of C:\\ is {totalDriveGb:N2} GB", LogMessageSeverity.Warning);
        }
        finally
        {
            NativeMemory.Free(flatTree);
        }
    }

    private async void CheckBox_Checked(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        await Task.Delay(50).ConfigureAwait(true);

        var selectedItems = 0;
        foreach (var item in ViewModel.CleanItems)
        {
            if (item.IsChecked)
            {
                selectedItems++;
            }
        }
        var totalItems = ViewModel.CleanItems.Count; // Store the count in a variable
        switch (selectedItems)
        {
            case 0:
                ViewModel.IsEverythingSelected = false;
                break;
            case var count when count == totalItems: // Use a pattern matching case
                ViewModel.IsEverythingSelected = true;
                break;
            default:
                ViewModel.IsEverythingSelected = null;
                break;
        }
    }
}