// Copyright (C) Ivirius(TM) Community 2020 - 2025. All Rights Reserved.
// Licensed under the MIT License.
using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.System.Com;

namespace Rebound.Core.Defrag;

/// <summary>
/// Tracks and manages Windows defragmentation operations
/// Reverse engineered from defrag.exe
/// </summary>
internal unsafe class COperationTracker : IDisposable
{
    #region Class Structure (from RE analysis)

    // +0x00: COM vtable pointers (IOperationTracker, IDefragClient)
    private ComPtr<IOperationTracker> ThisPtr;

    // +0x10: Magic validation signature
    private const uint MAGIC_SIGNATURE = 0x4c634664;
    private uint m_dwMagic = MAGIC_SIGNATURE;

    // +0x18: COM reference count
    private int m_cRef = 1;

    // +0x28: Reference trace list pointer
    private void* m_pRefTraceList;

    // +0x30, +0x40, +0x50: Three 16-element tracking arrays
    private readonly ulong[] m_trackingArray1 = new ulong[16]; // Current operation ID
    private readonly ulong[] m_trackingArray2 = new ulong[16]; // Previous operation ID
    private readonly ulong[] m_trackingArray3 = new ulong[16]; // Additional tracking

    // +0x60, +0x68, +0x70: Event handles for synchronization
    public HANDLE EventHandle1 { get; set; }  // Manual-reset event
    public HANDLE EventHandle2 { get; set; }  // Auto-reset event
    public HANDLE EventHandle3 { get; set; }  // Manual-reset event

    // +0x78: Life tick monitoring thread
    public HANDLE ThreadHandle { get; set; }

    // +0x84: Current operation phase
    private int m_currentPhase;

    // +0x88: Current operation status
    private int m_currentStatus;

    // +0x8C: Operation substatus
    private int m_subStatus;

    // +0x90: Percent complete (for progress tracking)
    private int m_percentComplete;

    // +0x98: ATL::CComPtr<IDefragEngine> - COM interface pointer
    private ComPtr<IUnknown> m_spUnknownInterface = default;

    // +0xA0/+0xA8: CBsString - BSTR string wrapper (operation name/path)
    private string m_operationName = string.Empty;

    // +0xB8: Critical section for thread safety (in C# we use lock)
    private readonly object m_criticalSection = new();

    // +0xC0: CRITICAL_SECTION (native - 40 bytes, we use lock above)

    // +0xE8: Sequence number for change notifications
    private ulong m_sequenceNumber;

    // +0xF0: Verbose output flag
    private int m_verboseOutput;

    // +0xF4: Detailed progress flag
    private int m_detailedProgress;

    // +0xF8: Additional event handle
    private HANDLE m_hAdditionalEvent;

    // +0x100: Shutdown/cancel event
    public HANDLE ShutdownEvent { get; set; }

    // +0x108: Completion event (signaled when operation finishes)
    public HANDLE CompletionEvent { get; set; }

    // +0x118: String list (file paths being processed)
    private void* m_pStringList;

    // +0x120: JSON output mode flag
    private int m_jsonOutputMode;

    #endregion

    #region Lifecycle Methods

    public static HRESULT CreateInstance(void* param1, void* param2, out IOperationTracker* tracker)
    {
        tracker = null;

        // This would create the actual COM object
        // In real implementation, use ATL::CComObject<COperationTracker>::CreateInstance

        var instance = new COperationTracker();
        var hr = instance.Initialize(param1, param2);

        if (hr.Failed)
            return hr;

        // Query for IOperationTracker interface
        tracker = (IOperationTracker*)Marshal.GetComInterfaceForObject(instance, typeof(IOperationTracker));

        return HRESULT.S_OK;
    }

    public HRESULT FinalConstruct()
    {
        // Zero tracking arrays
        Array.Clear(m_trackingArray1);
        Array.Clear(m_trackingArray2);
        Array.Clear(m_trackingArray3);
        
        // Create synchronization events
        EventHandle1 = new(PInvoke.CreateEvent(null, true, false, null).DangerousGetHandle());  // Manual-reset
        if ((nint)EventHandle1.Value == IntPtr.Zero)
            return new HRESULT(Marshal.GetHRForLastWin32Error());

        EventHandle2 = new(PInvoke.CreateEvent(null, false, false, null).DangerousGetHandle()); // Auto-reset
        if ((nint)EventHandle2.Value == IntPtr.Zero)
            return new HRESULT(Marshal.GetHRForLastWin32Error());

        EventHandle3 = new(PInvoke.CreateEvent(null, true, false, null).DangerousGetHandle());  // Manual-reset
        if ((nint)EventHandle3.Value == IntPtr.Zero)
            return new HRESULT(Marshal.GetHRForLastWin32Error());

        return HRESULT.S_OK;
    }

    public void Dispose()
    {
        // Signal completion event
        if ((nint)CompletionEvent.Value != IntPtr.Zero && (nint)CompletionEvent.Value != new IntPtr(-1))
        {
            PInvoke.SetEvent(CompletionEvent);
        }

        // Close all handles
        CloseHandleSafe(ref ShutdownEvent);
        CloseHandleSafe(ref ThreadHandle);
        CloseHandleSafe(ref EventHandle3);
        CloseHandleSafe(ref EventHandle2);
        CloseHandleSafe(ref EventHandle1);

        // Release COM interfaces
        m_spUnknownInterface.Dispose();

        // Reset magic
        m_dwMagic = 0;
    }

    private void CloseHandleSafe(ref HANDLE handle)
    {
        if ((nint)handle.Value != IntPtr.Zero && (nint)handle.Value != new IntPtr(-1))
        {
            PInvoke.CloseHandle(handle);
            handle = default;
        }
    }

    #endregion

    #region Registration

    public HRESULT Initialize(void* param_1, void* param_2)
    {
        var hr = InternalRegister();
        if (hr.Failed)
            return hr;

        // Query for IOperationTracker interface
        ComPtr<IUnknown> thisInterface = default;
        void** rawPtr = null;
        fixed (Guid* guidPtr = &IOperationTracker.Guid)
        {
            hr = ThisPtr.Get()->QueryInterface(guidPtr, rawPtr);
        }
        if (hr.Failed)
            return hr;

        thisInterface.Attach((IUnknown*)rawPtr);

        // Create monitoring thread
        delegate* unmanaged[Stdcall]<void*, uint> tickThread = &LifeTickThread;

        ThreadHandle = new(PInvoke.CreateThread(
            null, 0, tickThread, rawPtr, 0, out _).DangerousGetHandle());

        if ((nint)ThreadHandle.Value == IntPtr.Zero)
            return new HRESULT(Marshal.GetHRForLastWin32Error());

        // Store parameters
        CloseHandleSafe(ref ShutdownEvent);
        ShutdownEvent = new HANDLE((IntPtr)param_1);
        CompletionEvent = new HANDLE((IntPtr)param_2);

        return HRESULT.S_OK;
    }

    private HRESULT InternalRegister()
    {
        // Release old interface
        if (!m_spUnknownInterface.IsNull)
        {
            m_spUnknownInterface.Dispose();
        }

        // Create defrag engine COM object
        var hr = PInvoke.CoCreateInstance(
            typeof(IDefragEnginePriv).GUID,
            null,
            CLSCTX.CLSCTX_INPROC_SERVER,
            typeof(IDefragEnginePriv).GUID,
            out void* engine);

        if (hr.Failed)
            return hr;

        m_spUnknownInterface.Attach((IUnknown*)engine);

        // Register this tracker
        var defragEngine = (IDefragEnginePriv*)engine;
        fixed (ulong* pTracking = m_trackingArray2)
        {
            return defragEngine->RegisterTracker(this, pTracking);
        }
    }

    public HRESULT Unregister()
    {
        // Get IDefragClient interface
        ComPtr<IDefragClient> client = default;
        var hr = ThisPtr.Get()->GetControllingUnknown()->QueryInterface(
            IDefragClient.Guid, out void* clientPtr);

        if (hr.Failed)
            return hr;

        client.Attach((IDefragClient*)clientPtr);
        PInvoke.CoDisconnectObject((IUnknown*)clientPtr, 0);

        // Unregister from engine
        if (!m_spUnknownInterface.IsNull)
        {
            var engine = (IDefragEnginePriv*)m_spUnknownInterface.Get();
            fixed (ulong* pTracking = m_trackingArray1)
            {
                engine->UnregisterTracker((uint*)pTracking);
            }
        }

        return HRESULT.S_OK;
    }

    #endregion

    #region Operation Control

    public HRESULT InvokeOperation(uint operationType, ushort* volumePath, void* options)
    {
        lock (m_criticalSection)
        {
            // Check if already running
            if ((nint)CompletionEvent.Value != 0)
                return new HRESULT(unchecked((int)0x8900000A));

            // Reset state
            Array.Clear(m_trackingArray1);
            m_sequenceNumber = 0;
            PInvoke.ResetEvent(EventHandle1);

            // Store volume path
            if (volumePath != null)
            {
                m_operationName = Marshal.PtrToStringUni((IntPtr)volumePath) ?? "";
            }

            // Invoke specific operation
            return operationType switch
            {
                1 => InvokeAnalyze(volumePath),
                2 => InvokeDefragment(volumePath),
                3 => InvokeOptimize(volumePath),
                4 => InvokeConsolidateFreeSpace(volumePath),
                5 => InvokeRescan(volumePath),
                6 => InvokeSlabConsolidation(volumePath),
                7 => InvokeTierOptimization(volumePath),
                8 => InvokeRetrim(volumePath),
                9 => InvokeThinProvisioning(volumePath),
                10 => InvokeTieredStorageOptimization(volumePath),
                _ => new HRESULT(unchecked((int)0x89000007))
            };
        }
    }

    private HRESULT InvokeAnalyze(ushort* volumePath)
    {
        var engine = (IDefragEnginePriv*)m_spUnknownInterface.Get();
        ulong[] volumeInfo = new ulong[2];

        fixed (ulong* pTracking = m_trackingArray1)
        fixed (ulong* pVolInfo = volumeInfo)
        {
            return engine->Analyze((char*)volumePath, (Guid*)pVolInfo, (Guid*)pTracking);
        }
    }

    private HRESULT InvokeDefragment(ushort* volumePath)
    {
        var engine = (IDefragEnginePriv*)m_spUnknownInterface.Get();
        ulong[] volumeInfo = new ulong[2];

        fixed (ulong* pTracking = m_trackingArray1)
        fixed (ulong* pVolInfo = volumeInfo)
        {
            var hr = engine->StartDefragment(volumePath, pVolInfo, (Guid*)pTracking);
            if (hr.Succeeded)
            {
                fixed (uint* pData = new uint[] {
                    (uint)m_trackingArray1[0], (uint)m_trackingArray1[1],
                    (uint)m_trackingArray1[2], (uint)m_trackingArray1[3]
                })
                {
                    engine->SetOperationTracking(pData);
                }
            }
            return hr;
        }
    }

    private unsafe HRESULT InvokeOptimize(IDefragEngine* engine, ushort* volumePath, DefragOptions options)
    {
        // Query for appropriate optimization interface
        if (options.UseSimpleOptimization)
        {
            ComPtr<IDefragmentSimple2> simpleDefrag = default;
            var hr = engine->QueryInterface(IDefragmentSimple2.Guid, out void* ptr);
            if (hr.Failed)
                return hr;

            simpleDefrag.Attach((IDefragmentSimple2*)ptr);

            // Start simple optimization
            ulong[] volumeInfo = new ulong[2];
            fixed (ulong* pTracking = m_trackingArray1)
            fixed (ulong* pVolInfo = volumeInfo)
            {
                return simpleDefrag.Get()->StartSimpleOptimize(
                    volumePath, options.Priority, options.Flags, (Guid*)pTracking);
            }
        }
        else
        {
            ComPtr<IDefragmentFull2> fullDefrag = default;
            var hr = engine->QueryInterface(IDefragmentFull2.Guid, out void* ptr);
            if (hr.Failed)
                return hr;

            fullDefrag.Attach((IDefragmentFull2*)ptr);

            // Start full optimization
            ulong[] volumeInfo = new ulong[2];
            fixed (ulong* pTracking = m_trackingArray1)
            fixed (ulong* pVolInfo = volumeInfo)
            {
                return fullDefrag.Get()->StartFullOptimize(
                    volumePath, options.Priority, options.Flags, (Guid*)pTracking);
            }
        }
    }

    private HRESULT InvokeConsolidateFreeSpace(ushort* volumePath) => HRESULT.E_NOTIMPL;
    private HRESULT InvokeRescan(ushort* volumePath) => HRESULT.E_NOTIMPL;
    private HRESULT InvokeSlabConsolidation(ushort* volumePath) => HRESULT.E_NOTIMPL;
    private HRESULT InvokeTierOptimization(ushort* volumePath) => HRESULT.E_NOTIMPL;
    private HRESULT InvokeRetrim(ushort* volumePath) => HRESULT.E_NOTIMPL;
    private HRESULT InvokeThinProvisioning(ushort* volumePath) => HRESULT.E_NOTIMPL;
    private HRESULT InvokeTieredStorageOptimization(ushort* volumePath) => HRESULT.E_NOTIMPL;

    public HRESULT InternalCancelOperation()
    {
        if (!m_spUnknownInterface.IsNull)
        {
            var engine = (IDefragEnginePriv*)m_spUnknownInterface.Get();
            fixed (ulong* pTracking = m_trackingArray1)
            {
                return engine->CancelOperation((uint*)pTracking);
            }
        }
        return HRESULT.S_OK;
    }

    public HRESULT Shutdown(int forceCancel)
    {
        lock (m_criticalSection)
        {
            if (forceCancel != 0)
            {
                InternalCancelOperation();
            }

            // Mark as shutting down
            CompletionEvent = new HANDLE(new IntPtr(1));

            // Copy final tracking data
            Array.Copy(m_trackingArray2, m_trackingArray1, 4);

            // Signal shutdown event
            PInvoke.SetEvent(EventHandle2);

            // Call derived class shutdown
            var hr = ((IOperationTracker*)ThisPtr.Get())->Shutdown();

            // Wait for thread completion
            PInvoke.WaitForSingleObject(EventHandle3, 0xFFFFFFFF);

            return hr;
        }
    }

    public HRESULT TrackOperation()
    {
        HANDLE[] waitHandles = { EventHandle1, EventHandle2, EventHandle3 };
        bool completed = false;

        while (!completed)
        {
            uint waitResult = PInvoke.WaitForMultipleObjects(
                (uint)waitHandles.Length, waitHandles, false, 0xFFFFFFFF);

            switch (waitResult)
            {
                case 0: // Operation complete
                    if (m_currentStatus < 0)
                        return new HRESULT(m_currentStatus);
                    completed = true;
                    break;

                case 1: // Status update
                    ((IOperationTracker*)ThisPtr.Get())->OnStatusUpdate();
                    break;

                case 2: // Shutdown requested
                    InternalCancelOperation();
                    ((IOperationTracker*)ThisPtr.Get())->OnStatusUpdate();
                    return new HRESULT(unchecked((int)0x8900000A));

                case 3: // Cancel requested
                    InternalCancelOperation();
                    ((IOperationTracker*)ThisPtr.Get())->OnStatusUpdate();
                    return new HRESULT(unchecked((int)0x8900000A));
            }
        }

        return HRESULT.S_OK;
    }

    #endregion

    #region Status Query

    public HRESULT GetStatus(ushort* volumePath, out void** statusArray)
    {
        statusArray = null;
        uint count = 0;

        var engine = (IDefragEnginePriv*)m_spUnknownInterface.Get();
        return engine->GetStatus(volumePath, &count, statusArray);
    }

    public HRESULT GetAllStatus(out uint count, out void** statusArray)
    {
        count = 0;
        statusArray = null;

        var engine = (IDefragEnginePriv*)m_spUnknownInterface.Get();
        return engine->GetAllStatus(null, &count, statusArray);
    }

    public HRESULT GetOperationID(out Guid operationId)
    {
        fixed (ulong* pTracking = m_trackingArray1)
        {
            operationId = *(Guid*)pTracking;
        }
        return HRESULT.S_OK;
    }

    public HRESULT GetFriendlyName(ushort* volumePath, out ushort** friendlyName)
    {
        friendlyName = null;

        // Get volume description and return as friendly name
        // This would call SxGetVolumeDescription from the native code

        return HRESULT.S_OK;
    }

    #endregion

    #region Change Notifications

    public HRESULT ChangeNotification(ulong sequenceNumber, uint statusCount, void* statusArray)
    {
        if (statusArray == null || statusCount == 0)
            return HRESULT.E_INVALIDARG;

        lock (m_criticalSection)
        {
            // Check for old notification
            if (sequenceNumber < m_sequenceNumber)
                return new HRESULT(1);

            // Wait for sequence if out of order
            uint retries = 0;
            while (m_sequenceNumber < sequenceNumber && retries < 5)
            {
                Monitor.Exit(m_criticalSection);
                Thread.Sleep(100);
                Monitor.Enter(m_criticalSection);
                retries++;
            }

            m_sequenceNumber = sequenceNumber + 1;

            // Process status updates
            // ... (complex notification processing)

            return HRESULT.S_OK;
        }
    }

    public HRESULT DetachTracker(int result)
    {
        m_currentStatus = result;

        // Copy tracking data
        Array.Copy(m_trackingArray2, m_trackingArray1, 4);

        // Signal completion
        PInvoke.SetEvent(EventHandle1);

        return HRESULT.S_OK;
    }

    #endregion

    #region Statistics Printing

    public HRESULT PrintVolumeStatistics(uint operationType)
    {
        lock (m_criticalSection)
        {
            // Get statistics from engine
            var engine = (IDefragEnginePriv*)m_spUnknownInterface.Get();

            // Print statistics based on mode
            if (m_jsonOutputMode != 0)
            {
                Console.WriteLine("{");
                Console.WriteLine("  \"OptimizationStatistics\": {");
                // ... JSON output
                Console.WriteLine("  }");
                Console.WriteLine("}");
            }
            else
            {
                // Normal text output
                Console.WriteLine("Volume Statistics:");
                // ... text output
            }

            return HRESULT.S_OK;
        }
    }

    #endregion

    #region Life Tick Thread

    [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvStdcall) })]
    public static uint LifeTickThread(void* param)
    {
        if (param == null)
            return 0;

        var tracker = (COperationTracker*)param;

        // Monitor operation health
        while (true)
        {
            uint waitResult = PInvoke.WaitForSingleObject(tracker.EventHandle2, 5000);

            if (waitResult == 0) // Signaled - shutdown
                break;

            if (waitResult == 0x102) // Timeout - check health
            {
                // Query engine to verify it's still alive
                ComPtr<IUnknown> testInterface = default;
                var hr = tracker.m_spUnknownInterface.Get()->QueryInterface(
                    typeof(IStream).GUID, out void* ptr);

                if (hr.Value != unchecked((int)0x80004002)) // Not RPC_E_DISCONNECTED
                {
                    PInvoke.SetEvent(tracker.EventHandle1);
                    break;
                }
            }
        }

        // Release interface
        var vtbl = *(void***)param;
        var release = (delegate* unmanaged[Stdcall]<void*, void>)(vtbl[2]);
        release(param);

        return 0;
    }

    public HRESULT WatchLifeTick()
    {
        if (m_spUnknownInterface.IsNull)
        {
            PInvoke.SetEvent(EventHandle1);
            return HRESULT.S_OK;
        }

        bool done = false;
        while (!done)
        {
            uint waitResult = PInvoke.WaitForSingleObject(EventHandle2, 5000);

            if (waitResult == 0)
                break;

            if (waitResult == 0x102) // Timeout
            {
                // Check if engine is still alive
                ComPtr<IUnknown> test = default;
                var hr = m_spUnknownInterface.Get()->QueryInterface(
                    typeof(IStream).GUID, out void* ptr);

                if (hr.Value != unchecked((int)0x80004002))
                {
                    PInvoke.SetEvent(EventHandle1);
                    done = true;
                }
            }
        }

        return HRESULT.S_OK;
    }

    #endregion
}

// Supporting structures (would be in separate files)
public unsafe struct DefragOptions
{
    public bool JsonOutput;
    public bool UseSimpleOptimization;
    public uint Priority;
    public ulong Flags;
    public ulong RescanFlags;
}

public unsafe struct DefragStatistics
{
    public uint FilesCount;
    public uint FragmentedFilesCount;
    public ulong TotalFilesBytes;
    public ulong TotalMoveBytes;
    public ulong TotalMoveCounts;
    public ulong TotalSkipBytes;
    public ulong TotalSkipCounts;
    public ulong TotalFailBytes;
    public ulong TotalFailCounts;
    public ulong TotalMoveTimeMs;
    public uint BytesPerCluster;
    public ulong TotalClusters;
    public ulong FreeClusters;
    public uint FragmentationPercent;
    public uint FilesProcessed;
    public double AverageFragmentsPerFile;
}

public unsafe struct DefragStatus
{
    public Guid OperationId;
    public int Phase;
    public int Result;
    public DefragStatistics* Statistics;
}

public unsafe struct FileStatisticsList
{
    public FileStatisticsNode* Head;
}

public unsafe struct FileStatisticsNode
{
    public ushort* FilePath;
    public ulong TotalBytes;
    public ulong TotalCounts;
    public ulong MoveBytes;
    public ulong MoveCounts;
    public ulong MoveTimeMs;
    public ulong SkipBytes;
    public ulong SkipCounts;
    public int Status;
    public ulong DurationMs;
    public FileStatisticsNode* Next;
}