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
                3 => InvokeOptimize(volumePath, (DefragOptions*)options),
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

    private unsafe HRESULT InvokeOptimize(ushort* volumePath, DefragOptions* options)
    {
        HRESULT hr;

        var engine = (IDefragEngine*)m_spUnknownInterface.Get();

        Guid operationGuid = Guid.NewGuid();

        fixed (ulong* pTrackingRaw = m_trackingArray1)
        {
            Guid* trackingGuid = (Guid*)pTrackingRaw;

            // Native code uses two paths: original + normalized
            ushort* normalizedPath = volumePath;

            if (options->UseSimpleOptimization != 0)
            {
                void* simplePtr;

                // SIMPLE OPTIMIZE PATH
                fixed (Guid* guid = &IDefragmentSimple2.Guid)
                {
                    hr = engine->QueryInterface(
                        guid,
                        &simplePtr);
                }
                if (hr.Failed)
                {
                    // If extended flags requested → fail (native behavior)
                    if (options->ExtendedFlags != 0)
                        return hr;

                    // Legacy fallback
                    return engine->SimpleOptimize(
                        volumePath,
                        null,
                        trackingGuid);
                }

                using var simpleIface = new ComPtr<IUnknown>((IUnknown*)simplePtr);
                var simple = (IDefragmentSimple2*)simpleIface.Get();

                return simple->StartSimpleOptimize(
                    volumePath,
                    (int)options->Priority,
                    normalizedPath,
                    &operationGuid,
                    trackingGuid);
            }
            else
            {
                // FULL OPTIMIZE PATH
                hr = engine->QueryInterface(
                    IDefragmentFull2.Guid,
                    out void* fullPtr);

                if (hr.Failed)
                {
                    if (options->ExtendedFlags != 0)
                        return hr;

                    return engine->FullDefragment(
                        volumePath,
                        null,
                        trackingGuid);
                }

                using var fullIface = new ComPtr<IUnknown>((IUnknown*)fullPtr);
                var full = (IDefragmentFull2*)fullIface.Get();

                return full->StartFullOptimize(
                    volumePath,
                    options->Priority,
                    normalizedPath,
                    &operationGuid,
                    trackingGuid);
            }
        }
    }

    private unsafe HRESULT InvokeConsolidateFreeSpace(ushort* volumePath, DefragOptions options)
    {
        var engine = (IDefragEngine*)m_spUnknownInterface.Get();
        ulong[] volumeInfo = new ulong[2];

        fixed (ulong* pTracking = m_trackingArray1)
        fixed (ulong* pVolInfo = volumeInfo)
        {
            var hr = engine->ConsolidateFreeSpace(
                volumePath,
                pVolInfo,
                (Guid*)pTracking);

            if (hr.Succeeded)
            {
                // Set operation tracking with engine
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

    private unsafe HRESULT InvokeRescan(ushort* volumePath, DefragOptions options)
    {
        var engine = (IDefragEngine*)m_spUnknownInterface.Get();
        ulong[] volumeInfo = new ulong[2];

        fixed (ulong* pTracking = m_trackingArray1)
        fixed (ulong* pVolInfo = volumeInfo)
        {
            return engine->Rescan(
                volumePath,
                options.RescanFlags,
                pVolInfo,
                (Guid*)pTracking);
        }
    }

    private unsafe HRESULT InvokeSlabConsolidation(ushort* volumePath, DefragOptions options)
    {
        var engine = (IDefragEngine*)m_spUnknownInterface.Get();
        ulong[] volumeInfo = new ulong[2];

        fixed (ulong* pTracking = m_trackingArray1)
        fixed (ulong* pVolInfo = volumeInfo)
        {
            return engine->SlabConsolidation(
                volumePath,
                options.Priority,
                pVolInfo,
                (Guid*)pTracking);
        }
    }

    private unsafe HRESULT InvokeTierOptimization(ushort* volumePath, DefragOptions options)
    {
        var engine = (IDefragEngine*)m_spUnknownInterface.Get();
        ulong[] volumeInfo = new ulong[2];

        fixed (ulong* pTracking = m_trackingArray1)
        fixed (ulong* pVolInfo = volumeInfo)
        {
            return engine->TierOptimization(
                volumePath,
                options.Priority,
                pVolInfo,
                (Guid*)pTracking);
        }
    }

    private unsafe HRESULT InvokeRetrim(ushort* volumePath, DefragOptions options)
    {
        var engine = (IDefragEngine*)m_spUnknownInterface.Get();
        ulong[] volumeInfo = new ulong[2];

        fixed (ulong* pTracking = m_trackingArray1)
        fixed (ulong* pVolInfo = volumeInfo)
        {
            return engine->Retrim(
                volumePath,
                options.Priority,
                pVolInfo,
                (Guid*)pTracking);
        }
    }

    private unsafe HRESULT InvokeThinProvisioning(ushort* volumePath, DefragOptions options)
    {
        var engine = (IDefragEngine*)m_spUnknownInterface.Get();
        ulong[] volumeInfo = new ulong[2];

        fixed (ulong* pTracking = m_trackingArray1)
        fixed (ulong* pVolInfo = volumeInfo)
        {
            return engine->ThinProvisioning(
                volumePath,
                pVolInfo,
                (Guid*)pTracking);
        }
    }

    private unsafe HRESULT InvokeTieredStorageOptimization(ushort* volumePath, DefragOptions options)
    {
        var engine = (IDefragEngine*)m_spUnknownInterface.Get();

        // Build tiered storage parameters structure
        TieredStorageParams tierParams = new()
        {
            VolumePath = volumePath,
            Priority = options.Priority,
            MinFileSize = options.TierMinFileSize,
            MaxFileSize = options.TierMaxFileSize,
            TierFlags = options.TierFlags,
            HotTierThreshold = options.HotTierThreshold,
            ColdTierThreshold = options.ColdTierThreshold
        };

        fixed (ulong* pTracking = m_trackingArray1)
        {
            tierParams.OperationId = (Guid*)pTracking;

            return engine->TieredStorageOptimization(&tierParams, (Guid*)pTracking);
        }
    }

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

    public unsafe HRESULT ChangeNotification(
        ulong sequenceNumber,
        uint statusCount,
        DefragStatus* statusArray)
    {
        if (statusArray == null || statusCount == 0)
            return HRESULT.E_INVALIDARG;

        lock (m_criticalSection)
        {
            // Check if this is an old notification (already processed)
            if (sequenceNumber < m_sequenceNumber)
            {
                m_cRef = 1; // Mark as processed
                return new HRESULT(1);
            }

            // Handle out-of-order notifications with retry logic
            uint retryCount = 0;
            while (m_sequenceNumber < sequenceNumber && retryCount < 5)
            {
                Monitor.Exit(m_criticalSection);
                Thread.Sleep(100);
                Monitor.Enter(m_criticalSection);
                retryCount++;
            }

            // Update sequence number
            m_sequenceNumber = sequenceNumber + 1;

            // Check if operation tracking has changed
            bool trackingChanged =
                m_trackingArray1[0] != m_trackingArray2[0] ||
                m_trackingArray1[1] != m_trackingArray2[1];

            if (!trackingChanged)
            {
                m_cRef = unchecked((int)0x89000005); // E_NO_CHANGE
                return new HRESULT(unchecked((int)0x89000005));
            }

            // Process each status update
            bool statusUpdated = false;

            for (uint i = 0; i < statusCount; i++)
            {
                var status = &statusArray[i];

                // Check if this status matches our tracked operation
                if (status->OperationId.Data1 != (uint)m_trackingArray1[0] ||
                    status->OperationId.Data2 != (ushort)m_trackingArray1[1])
                    continue;

                // Handle phase/status changes
                if (m_currentPhase != status->Phase ||
                    m_currentStatus != status->CurrentStatus ||
                    m_subStatus != status->SubStatus)
                {
                    // Print phase change if in detailed mode
                    if (m_detailedProgress != 0)
                    {
                        PrintPhaseChange(status);
                    }

                    // Special handling for analysis completion
                    if (m_verboseOutput == 0 && status->Phase == 13) // Analysis complete
                    {
                        if (m_jsonOutputMode == 0 || status->Statistics->IsAnalysisComplete == 0)
                        {
                            PrintMessageFromID(0xD4); // "Analysis complete" message
                            InternalPrintStatistics(status->Statistics, 2);
                        }
                        else
                        {
                            // JSON mode with analysis data
                            Console.WriteLine($"    \"AnalysisTimeMs\": {status->Statistics->AnalysisTimeMs},");
                        }
                    }

                    // Update tracking state
                    m_currentPhase = status->Phase;
                    m_currentStatus = status->CurrentStatus;
                    m_subStatus = status->SubStatus;
                    m_percentComplete = status->PercentComplete;
                }

                // Print file statistics if configured
                if (m_jsonOutputMode != 0)
                {
                    if (m_verboseOutput == 0 || status->Phase != 13)
                    {
                        PrintFileStatistics(status->UnfragmentedFiles);
                    }
                    else
                    {
                        PrintFraggedFileStatistics(status->FragmentedFiles);
                    }
                }

                // Update operation tracking
                m_trackingArray2[0] = status->OperationId.Data1;
                m_trackingArray2[1] = status->OperationId.Data2;
                m_trackingArray2[2] = status->OperationId.Data3;
                m_trackingArray2[3] = (ulong)status->OperationId.Data4;

                // Call status callback if provided
                if ((nint)CompletionEvent.Value is not 0 and not -1)
                {
                    var callback = (IDefragCallback*)CompletionEvent.Value;
                    callback->OnStatusUpdate(status->Result);
                }

                statusUpdated = true;
            }

            // Set processed flag
            if (statusUpdated)
            {
                if (m_cRef == 0)
                {
                    m_cRef = 1;
                }
            }

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
    private unsafe void PrintPhaseChange(DefragStatus* status)
    {
        uint messageId = 0;
        HRESULT hr = GetMessageFromPhase(status->Phase, &messageId);

        if (hr.Failed)
            return;

        string message = LoadStringResource(messageId);

        if (m_jsonOutputMode == 0)
        {
            if (status->Result < 0)
            {
                // Error format
                Console.WriteLine($"\t{message} [FAILED: 0x{status->Result:X8}]");
            }
            else
            {
                // Normal format
                Console.WriteLine($"\r\t{message}");
            }
        }
    }

    private unsafe HRESULT GetMessageFromPhase(int phase, uint* messageId)
    {
        *messageId = phase switch
        {
            0 => 0x100,  // Initializing
            1 => 0x101,  // Analyzing
            2 => 0x102,  // Defragmenting
            5 => 0x105,  // Consolidating
            7 => 0x107,  // Optimizing
            10 => 0x10A, // Retrimming
            13 => 0x10D, // Complete
            _ => 0
        };

        return *messageId != 0 ? HRESULT.S_OK : HRESULT.E_FAIL;
    }

    private string LoadStringResource(uint resourceId)
    {
        // This would load from embedded resources
        // For now, return placeholder strings
        return resourceId switch
        {
            0x100 => "Initializing...",
            0x101 => "Analyzing volume...",
            0x102 => "Defragmenting files...",
            0x105 => "Consolidating free space...",
            0x107 => "Optimizing...",
            0x10A => "Performing TRIM operation...",
            0x10D => "Operation complete",
            0xD4 => "\nAnalysis Report:",
            _ => $"Phase {resourceId}"
        };
    }

    #endregion

    private unsafe void PrintStatisticsJson(DefragStatistics* stats)
    {
        Console.WriteLine("{");
        Console.WriteLine($"    \"OptimizationStatistics\": {{");
        Console.WriteLine($"        \"FilesCount\": {stats->FilesCount},");
        Console.WriteLine($"        \"FragmentedFilesCount\": {stats->FragmentedFilesCount},");
        Console.WriteLine($"        \"TotalFilesBytes\": {stats->TotalFilesBytes},");
        Console.WriteLine($"        \"TotalMoveBytes\": {stats->TotalMoveBytes},");
        Console.WriteLine($"        \"TotalMoveCounts\": {stats->TotalMoveCounts},");
        Console.WriteLine($"        \"TotalSkipBytes\": {stats->TotalSkipBytes},");
        Console.WriteLine($"        \"TotalSkipCounts\": {stats->TotalSkipCounts},");
        Console.WriteLine($"        \"TotalFailBytes\": {stats->TotalFailBytes},");
        Console.WriteLine($"        \"TotalFailCounts\": {stats->TotalFailCounts},");
        Console.WriteLine($"        \"TotalMoveTimeMs\": {stats->TotalMoveTimeMs}");
        Console.WriteLine($"    }}");
        Console.WriteLine("}");
    }

    private unsafe void PrintStatisticsNormal(DefragStatistics* stats, uint operationType)
    {
        uint bytesPerCluster = stats->BytesPerCluster;
        ulong totalClusters = stats->TotalClusters;
        ulong freeClusters = stats->FreeClusters;
        ulong usedClusters = stats->UsedClusters;

        char decimalSeparator = '.';
        var locale = System.Globalization.CultureInfo.CurrentCulture;
        if (locale.NumberFormat.NumberDecimalSeparator.Length > 0)
        {
            decimalSeparator = locale.NumberFormat.NumberDecimalSeparator[0];
        }

        // Check if this is analysis or optimization
        if (m_jsonOutputMode == 0 || stats->IsAnalysisComplete == 0)
        {
            // Volume information
            Console.WriteLine(LoadStringResource(0x105)); // Volume report header

            Console.WriteLine(LoadStringResource(0x100)); // Total volume size
            Console.WriteLine($"    {FormatNumberString((ulong)bytesPerCluster * totalClusters, decimalSeparator)}");

            if (m_verboseOutput != 0)
            {
                Console.WriteLine(LoadStringResource(0x101)); // Cluster size
                Console.WriteLine($"    {FormatNumberString(bytesPerCluster, decimalSeparator)}");

                if (m_verboseOutput != 0)
                {
                    Console.WriteLine(LoadStringResource(0x102)); // Used space
                    Console.WriteLine($"    {FormatNumberString((ulong)bytesPerCluster * usedClusters, decimalSeparator)}");
                }
            }

            Console.WriteLine(LoadStringResource(0x103)); // Free space
            Console.WriteLine($"    {FormatNumberString((ulong)bytesPerCluster * (totalClusters - usedClusters), decimalSeparator)}");
        }
        else
        {
            // Optimization summary
            Console.WriteLine(LoadStringResource(0x156)); // Optimization header
            Console.WriteLine($"    {FormatNumberString((ulong)bytesPerCluster * (stats->ExcessFragmentation + stats->TotalFragmentation), decimalSeparator)}");
            Console.WriteLine(LoadStringResource(0x157));
            Console.WriteLine($"    {stats->TotalFragmentedFiles + stats->TotalUnfragmentedFiles}");
            Console.WriteLine(LoadStringResource(0x158));
            Console.WriteLine($"    {stats->MovableFiles + stats->UnmovableFiles}");
            Console.WriteLine(LoadStringResource(0x159));
        }

        // Operation-specific statistics
        if (operationType >= 6 && operationType <= 9)
        {
            PrintOptimizationStatistics(stats);
        }
    }

    private unsafe void PrintOptimizationStatistics(DefragStatistics* stats)
    {
        char decimalSeparator = '.';

        if (stats->OptimizationEnabled != 0)
        {
            Console.WriteLine(LoadStringResource(0x12F)); // "Optimization Statistics:"
            Console.WriteLine(LoadStringResource(0x130)); // Header separator

            if (m_verboseOutput != 0)
            {
                Console.WriteLine(LoadStringResource(0x131)); // Files moved
                Console.WriteLine($"    {FormatNumberString((ulong)stats->BytesPerCluster * stats->ClustersMoved, decimalSeparator)}");

                if (m_verboseOutput != 0)
                {
                    Console.WriteLine(LoadStringResource(0x132)); // Total bytes moved
                    Console.WriteLine($"    {FormatNumberString((ulong)stats->BytesPerCluster * stats->TotalBytesMoved, decimalSeparator)}");
                }
            }

            Console.WriteLine(LoadStringResource(0x133)); // Files fragmented
            Console.WriteLine($"    {stats->FragmentedFileCount}");
        }

        // Slab consolidation stats
        if ((operationType - 6 & 0xFFFFFFFD) == 0) // Operations 6 or 8
        {
            Console.WriteLine(LoadStringResource(0x138)); // "Slab Consolidation:"
            Console.WriteLine(LoadStringResource(0x142)); // Total slabs
            Console.WriteLine($"    {stats->TotalSlabs}");

            if (m_verboseOutput != 0)
            {
                Console.WriteLine(LoadStringResource(0x139)); // Slabs consolidated
                Console.WriteLine($"    {stats->SlabsConsolidated}");

                if (m_verboseOutput != 0)
                {
                    Console.WriteLine(LoadStringResource(0x13A)); // Slab moves
                    Console.WriteLine($"    {stats->SlabMoves}");

                    if (m_verboseOutput != 0)
                    {
                        Console.WriteLine(LoadStringResource(0x13B)); // Slab failures
                        Console.WriteLine($"    {stats->SlabFailures}");
                    }
                }
            }

            Console.WriteLine(LoadStringResource(0x13C)); // Space recovered
            Console.WriteLine($"    {FormatNumberString((ulong)stats->BytesPerCluster * stats->SpaceRecovered, decimalSeparator)}");
        }

        // Tier optimization stats
        if (operationType >= 7 && operationType <= 8)
        {
            Console.WriteLine(LoadStringResource(0x134)); // "Tier Optimization:"

            if (m_verboseOutput != 0)
            {
                Console.WriteLine(LoadStringResource(0x135)); // Files moved to fast tier
                Console.WriteLine($"    {stats->FilesMovedToFastTier}");

                if (m_verboseOutput != 0)
                {
                    Console.WriteLine(LoadStringResource(0x136)); // Files moved to slow tier
                    Console.WriteLine($"    {stats->FilesMovedToSlowTier}");
                }
            }

            Console.WriteLine(LoadStringResource(0x137)); // Total bytes tiered
            Console.WriteLine($"    {FormatNumberString((ulong)stats->BytesPerCluster * stats->BytesTiered, decimalSeparator)}");
        }
    }

    private unsafe HRESULT PrintFraggedFileStatistics(FileStatisticsList* fileList)
    {
        if (fileList == null)
            return HRESULT.E_INVALIDARG;

        Console.WriteLine("    \"FragmentedFileStatistics\": [");

        var current = fileList->Head;
        bool first = true;

        while (current != null)
        {
            if (!first)
                Console.WriteLine(",");

            // Convert file path to JSON-safe string
            string filePath = ConvertToJsonString(current->FilePath);

            Console.WriteLine("        {");
            Console.WriteLine($"            \"FilePath\": \"{filePath}\",");
            Console.WriteLine($"            \"Status\": \"0x{current->Status:X8}\",");
            Console.WriteLine($"            \"DurationMs\": {current->DurationMs},");
            Console.WriteLine($"            \"TotalBytes\": {current->TotalBytes},");
            Console.WriteLine($"            \"MoveBytes\": {current->MoveBytes},");
            Console.WriteLine($"            \"MoveCounts\": {current->MoveCounts},");
            Console.WriteLine($"            \"MoveTimeMs\": {current->MoveTimeMs},");
            Console.WriteLine($"            \"SkipBytes\": {current->SkipBytes},");
            Console.WriteLine($"            \"SkipCounts\": {current->SkipCounts},");
            Console.WriteLine($"            \"FailBytes\": {current->FailBytes},");
            Console.WriteLine($"            \"FailCounts\": {current->FailCounts},");
            Console.WriteLine($"            \"MoveIops\": {current->MoveIops},");
            Console.WriteLine($"            \"MoveMbps\": {current->MoveMbps}");
            Console.Write("        }");

            first = false;
            current = current->Next;
        }

        Console.WriteLine("\n    ],");
        return HRESULT.S_OK;
    }

    private unsafe string ConvertToJsonString(ushort* path)
    {
        if (path == null)
            return "";

        // This calls ToJsonFormatString from native code
        // We'll implement a managed version
        string str = Marshal.PtrToStringUni((IntPtr)path) ?? "";

        // Escape JSON special characters
        return str
            .Replace("\\", "\\\\")
            .Replace("\"", "\\\"")
            .Replace("\n", "\\n")
            .Replace("\r", "\\r")
            .Replace("\t", "\\t");
    }

    private string FormatNumberString(ulong number, char decimalSeparator)
    {
        // Format with thousands separators and decimal point
        string formatted = number.ToString("N0");

        // Replace decimal separator if needed
        if (decimalSeparator != '.')
        {
            formatted = formatted.Replace('.', decimalSeparator);
        }

        return formatted;
    }

    private void PrintMessageFromID(uint messageId)
    {
        string message = LoadStringResource(messageId);
        Console.WriteLine(message);
    }

    public unsafe struct DefragOptions
    {
        public int UseSimpleOptimization;
        public uint Priority;
        public ulong ExtendedFlags;
        public ulong RescanFlags;
        public ulong TierFlags;
        public ulong TierMinFileSize;
        public ulong TierMaxFileSize;
        public uint HotTierThreshold;
        public uint ColdTierThreshold;
    }

    public unsafe struct TieredStorageParams
    {
        public ushort* VolumePath;
        public uint Priority;
        public ulong MinFileSize;
        public ulong MaxFileSize;
        public ulong TierFlags;
        public uint HotTierThreshold;
        public uint ColdTierThreshold;
        public Guid* OperationId;
    }

    public unsafe struct DefragStatus
    {
        public Guid OperationId;
        public int Phase;
        public int CurrentStatus;
        public int SubStatus;
        public int PercentComplete;
        public int Result;
        public DefragStatistics* Statistics;
        public FileStatisticsList* FragmentedFiles;
        public FileStatisticsList* UnfragmentedFiles;
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
        public ulong UsedClusters;
        public uint FragmentationPercent;
        public uint FilesProcessed;
        public double AverageFragmentsPerFile;
        public int IsAnalysisComplete;
        public ulong AnalysisTimeMs;
        public ulong ExcessFragmentation;
        public ulong TotalFragmentation;
        public uint TotalFragmentedFiles;
        public uint TotalUnfragmentedFiles;
        public uint MovableFiles;
        public uint UnmovableFiles;
        public int OptimizationEnabled;
        public ulong ClustersMoved;
        public ulong TotalBytesMoved;
        public uint FragmentedFileCount;
        public uint TotalSlabs;
        public uint SlabsConsolidated;
        public uint SlabMoves;
        public uint SlabFailures;
        public ulong SpaceRecovered;
        public uint FilesMovedToFastTier;
        public uint FilesMovedToSlowTier;
        public ulong BytesTiered;
    }

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