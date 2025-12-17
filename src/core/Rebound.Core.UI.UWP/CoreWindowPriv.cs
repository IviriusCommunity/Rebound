// Copyright (C) Ivirius(TM) Community 2020 - 2025. All Rights Reserved.
// Licensed under the MIT License.

using Microsoft.UI.Dispatching;
using System.Reflection.Metadata;
using System.Runtime.CompilerServices;
using TerraFX.Interop.Windows;
using TerraFX.Interop.WinRT;
using Windows.UI.Core;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Hosting;
using WinRT;
using static TerraFX.Interop.Windows.Windows;
using HWND = TerraFX.Interop.Windows.HWND;

namespace Rebound.Core.UI;

internal static class CoreWindowPriv
{
    internal static HWND _coreHwnd;
    internal static CoreWindow? _coreWindow;
    internal static ComPtr<ICoreWindowInterop> _coreWindowInterop;

    static unsafe CoreWindowPriv()
    {
        UIThreadQueue.QueueAction(() =>
        {
            // Obtain the CoreWindow for the XAML island
            _coreWindow = CoreWindow.GetForCurrentThread();
            var coreRaw = ((IUnknown*)((IWinRTObject)_coreWindow).NativeObject.ThisPtr);
            ThrowIfFailed(coreRaw->QueryInterface(__uuidof<ICoreWindowInterop>(), (void**)_coreWindowInterop.GetAddressOf()));
            ThrowIfFailed(_coreWindowInterop.Get()->get_WindowHandle((HWND*)Unsafe.AsPointer(ref _coreHwnd)));
            SynchronizationContext.SetSynchronizationContext(new DispatcherQueueSynchronizationContext(DispatcherQueue.GetForCurrentThread()));
            return Task.CompletedTask;
        });
    }
}
