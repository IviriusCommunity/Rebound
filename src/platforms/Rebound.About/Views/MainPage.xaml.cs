// Copyright (C) Ivirius(TM) Community 2020 - 2025. All Rights Reserved.
// Licensed under the MIT License.

using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;
using System.ServiceProcess;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.Input;
using Microsoft.UI;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Documents;
using Microsoft.UI.Xaml.Media;
using Rebound.About.ViewModels;
using Rebound.Core.Helpers;
using Rebound.Helpers;
using Rebound.Helpers.Environment;
using Windows.ApplicationModel.DataTransfer;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.Security;
using Windows.Win32.System.Com;

namespace Rebound.About.Views;

public sealed partial class MainPage : Page
{
    private MainViewModel ViewModel { get; } = new();

    public MainPage()
    {
        InitializeComponent();
        Load();

        // I'm too lazy to test RE'd COM somewhere else
        LoadDefragCOM();
    }

    // Define the CLSID and IID structs
    public static readonly Guid IID_IDefragmentSimple2 = new("5a43b3be-3deb-11ed-b878-0242ac120002");
    public static readonly Guid IID_IOperationTracker = new("81a4d1fa-4fc8-4e1f-88da-cc7edf7482ee");

    public unsafe partial struct IDefragClient : IComIID
    {
        public void** lpVtbl;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public HRESULT ChangeNotification(UInt64 notificationId, UInt32 notificationType, void* notificationData)
        {
            return (HRESULT)((delegate* unmanaged[MemberFunction]<IDefragClient*, UInt64, UInt32, void*, int>)(lpVtbl[3]))((IDefragClient*)Unsafe.AsPointer(ref this), notificationId, notificationType, notificationData);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public IUnknown* GetControllingUnknown()
        {
            return (IUnknown*)((delegate* unmanaged[MemberFunction]<IDefragClient*, int>)(lpVtbl[4]))((IDefragClient*)Unsafe.AsPointer(ref this));
        }

        [GuidRVAGen.Guid("c958543e-b3a0-46ee-8085-4f111910d401")]
        public static partial ref readonly Guid Guid { get; }
    }

    public unsafe partial struct IDefragEnginePriv : IComIID
    {
        public void** lpVtbl;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public HRESULT Analyze(
            char* volumeName,
            Guid* param1,
            Guid* param2)
        {
            return (HRESULT)((delegate* unmanaged[MemberFunction]<IDefragEnginePriv*, char*, Guid*, Guid*, uint>)(lpVtbl[5]))
                ((IDefragEnginePriv*)Unsafe.AsPointer(ref this), volumeName, param1, param2);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public HRESULT BootOptimize(
            ushort* volumeName,
            Guid* param1,
            Guid* param2)
        {
            return (HRESULT)((delegate* unmanaged[MemberFunction]<IDefragEnginePriv*, ushort*, Guid*, Guid*, uint>)(lpVtbl[6]))
                ((IDefragEnginePriv*)Unsafe.AsPointer(ref this), volumeName, param1, param2);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public HRESULT Cancel(Guid id)
        {
            return (HRESULT)((delegate* unmanaged[MemberFunction]<IDefragEnginePriv*, Guid, uint>)(lpVtbl[7]))
                ((IDefragEnginePriv*)Unsafe.AsPointer(ref this), id);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public HRESULT DefragmentFull(
            ushort* volumeName,
            int flags,
            Guid* param1,
            Guid* param2)
        {
            return (HRESULT)((delegate* unmanaged[MemberFunction]<IDefragEnginePriv*, ushort*, int, Guid*, Guid*, uint>)(lpVtbl[8]))
                ((IDefragEnginePriv*)Unsafe.AsPointer(ref this), volumeName, flags, param1, param2);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public HRESULT DefragmentFile(
            ushort* fileName,
            Guid* param)
        {
            return (HRESULT)((delegate* unmanaged[MemberFunction]<IDefragEnginePriv*, ushort*, Guid*, uint>)(lpVtbl[9]))
                ((IDefragEnginePriv*)Unsafe.AsPointer(ref this), fileName, param);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public HRESULT DefragmentSimple(
            ulong param1,
            void* param2)
        {
            return (HRESULT)((delegate* unmanaged[MemberFunction]<IDefragEnginePriv*, ulong, void*, uint>)(lpVtbl[10]))
                ((IDefragEnginePriv*)Unsafe.AsPointer(ref this), param1, param2);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public HRESULT GetPossibleShrinkSpace(
            ulong param1,
            Guid* param2,
            ulong* param3)
        {
            return (HRESULT)((delegate* unmanaged[MemberFunction]<IDefragEnginePriv*, ulong, Guid*, ulong*, uint>)(lpVtbl[11]))
                ((IDefragEnginePriv*)Unsafe.AsPointer(ref this), param1, param2, param3);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public HRESULT Consolidate(
            ulong param1,
            Guid* param2,
            ulong param3,
            ulong* param4)
        {
            return (HRESULT)((delegate* unmanaged[MemberFunction]<IDefragEnginePriv*, ulong, Guid*, ulong, ulong*, uint>)(lpVtbl[12]))
                ((IDefragEnginePriv*)Unsafe.AsPointer(ref this), param1, param2, param3, param4);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public HRESULT Shrink(
            ulong param1,
            Guid* param2,
            void* param3,
            Guid* param4,
            ulong* param5)
        {
            return (HRESULT)((delegate* unmanaged[MemberFunction]<IDefragEnginePriv*, ulong, Guid*, void*, Guid*, ulong*, uint>)(lpVtbl[13]))
                ((IDefragEnginePriv*)Unsafe.AsPointer(ref this), param1, param2, param3, param4, param5);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public HRESULT Retrim(
            ulong param1,
            Guid* param2,
            uint param3,
            ulong param4,
            ulong* param5)
        {
            return (HRESULT)((delegate* unmanaged[MemberFunction]<IDefragEnginePriv*, ulong, Guid*, uint, ulong, ulong*, uint>)(lpVtbl[14]))
                ((IDefragEnginePriv*)Unsafe.AsPointer(ref this), param1, param2, param3, param4, param5);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public HRESULT Slabify(
            ulong param1,
            Guid* param2,
            uint param3,
            ulong param4,
            ulong* param5)
        {
            return (HRESULT)((delegate* unmanaged[MemberFunction]<IDefragEnginePriv*, ulong, Guid*, uint, ulong, ulong*, uint>)(lpVtbl[15]))
                ((IDefragEnginePriv*)Unsafe.AsPointer(ref this), param1, param2, param3, param4, param5);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public HRESULT SlabifyRetrim(
            ulong param1,
            Guid* param2,
            uint param3,
            ulong param4,
            ulong* param5)
        {
            return (HRESULT)((delegate* unmanaged[MemberFunction]<IDefragEnginePriv*, ulong, Guid*, uint, ulong, ulong*, uint>)(lpVtbl[16]))
                ((IDefragEnginePriv*)Unsafe.AsPointer(ref this), param1, param2, param3, param4, param5);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public HRESULT SlabAnalyze(
            ulong param1,
            Guid* param2,
            ulong param3,
            ulong* param4)
        {
            return (HRESULT)((delegate* unmanaged[MemberFunction]<IDefragEnginePriv*, ulong, Guid*, ulong, ulong*, uint>)(lpVtbl[17]))
                ((IDefragEnginePriv*)Unsafe.AsPointer(ref this), param1, param2, param3, param4);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public LRESULT GetStatus(
            void** param1,
            void** param2,
            uint* param3,
            ulong* param4)
        {
            return (LRESULT)((delegate* unmanaged[MemberFunction]<IDefragEnginePriv*, void**, void**, uint*, ulong*, long>)
                (lpVtbl[18]))((IDefragEnginePriv*)Unsafe.AsPointer(ref this), param1, param2, param3, param4);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public LRESULT GetVolumeStatistics(
            ulong param1,
            void** param2,
            void** param3)
        {
            return (LRESULT)((delegate* unmanaged[MemberFunction]<IDefragEnginePriv*, ulong, void**, void**, long>)(lpVtbl[19]))
                ((IDefragEnginePriv*)Unsafe.AsPointer(ref this), param1, param2, param3);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public HRESULT Register(IDefragClient* client, Guid* param)
        {
            return (HRESULT)((delegate* unmanaged[MemberFunction]<IDefragEnginePriv*, IDefragClient*, Guid*, int>)(lpVtbl[20]))((IDefragEnginePriv*)Unsafe.AsPointer(ref this), client, param);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public LRESULT Unregister(long param1, long* param2)
        {
            return (LRESULT)((delegate* unmanaged[MemberFunction]<IDefragEnginePriv*, long, long*, long>)(lpVtbl[21]))
                ((IDefragEnginePriv*)Unsafe.AsPointer(ref this), param1, param2);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public HRESULT WaitForCompletion(
            long param1,
            void** param2,
            long* param3)
        {
            return (HRESULT)((delegate* unmanaged[MemberFunction]<IDefragEnginePriv*, long, void**, long*, int>)(lpVtbl[22]))
                ((IDefragEnginePriv*)Unsafe.AsPointer(ref this), param1, param2, param3);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public HRESULT DisableAutomaticSleep(long param1, void** param2)
        {
            return (HRESULT)((delegate* unmanaged[MemberFunction]<IDefragEnginePriv*, long, void**, int>)(lpVtbl[23]))
                ((IDefragEnginePriv*)Unsafe.AsPointer(ref this), param1, param2);
        }

        /// <summary>
        /// This function doesn't have clear variable names to be used easily.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public LRESULT TierOptimize(
            ulong param1,
            uint* param2,
            ulong* param3)
        {
            return (LRESULT)((delegate* unmanaged[MemberFunction]<IDefragEnginePriv*, ulong, uint*, ulong*, long>)(lpVtbl[24]))
                ((IDefragEnginePriv*)Unsafe.AsPointer(ref this), param1, param2, param3);
        }

        /// <summary>
        /// This function doesn't have clear variable names to be used easily.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public LRESULT BootOptimize2(
            ulong param1,
            Guid* param2,
            void** param3,
            ulong param4,
            uint* param5)
        {
            return (LRESULT)((delegate* unmanaged[MemberFunction]<IDefragEnginePriv*, ulong, Guid*, void**, ulong, uint*, long>)(lpVtbl[25]))
                ((IDefragEnginePriv*)Unsafe.AsPointer(ref this), param1, param2, param3, param4, param5);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public HRESULT Shutdown(long param)
        {
            return (HRESULT)((delegate* unmanaged[MemberFunction]<IDefragEnginePriv*, long, int>)(lpVtbl[26]))((IDefragEnginePriv*)Unsafe.AsPointer(ref this), param);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public HRESULT NotifyVolumeChange(long param)
        {
            return (HRESULT)((delegate* unmanaged[MemberFunction]<IDefragEnginePriv*, long, int>)(lpVtbl[27]))((IDefragEnginePriv*)Unsafe.AsPointer(ref this), param);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public HRESULT WaitForEvents()
        {
            return (HRESULT)((delegate* unmanaged[MemberFunction]<IDefragEnginePriv*, int>)(lpVtbl[28]))((IDefragEnginePriv*)Unsafe.AsPointer(ref this));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public IUnknown* GetControllingUnknown()
        {
            return (IUnknown*)((delegate* unmanaged[MemberFunction]<IDefragEnginePriv*, int>)(lpVtbl[29]))((IDefragEnginePriv*)Unsafe.AsPointer(ref this));
        }

        [GuidRVAGen.Guid("0C401E84-3083-4764-B6B5-A0DE8FEDD40C")]
        public static partial ref readonly Guid Guid { get; }
    }

    public unsafe void LoadDefragCOM()
    {
        // Initialize COM for this thread
        var hrInit = PInvoke.CoInitializeEx(null, COINIT.COINIT_APARTMENTTHREADED);
        if (hrInit.Failed)
        {
            Debug.WriteLine($"CoInitializeEx failed: 0x{hrInit.Value:X8}");
            return;
        }

        object pAuthList;
        object pa2;

        try
        {
            unsafe
            {
                // Dummy native functions: just raw pointers to simple functions (or null)
                // You can create them as static unmanaged methods with [UnmanagedCallersOnly] or just IntPtr.Zero for no-op.

                // Raw unmanaged function implementations:

                [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvStdcall) })]
                static int Dummy_QueryInterface(IDefragClient* self, Guid* riid, void** ppvObject)
                {
                    *ppvObject = null;
                    return unchecked((int)0x80004002); // E_NOINTERFACE
                }

                [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvStdcall) })]
                static uint Dummy_AddRef(IDefragClient* self) => 1;

                [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvStdcall) })]
                static uint Dummy_Release(IDefragClient* self) => 1;

                [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvStdcall) })]
                static int Dummy_ChangeNotification(IDefragClient* self, ulong notificationId, uint notificationType, void* notificationData)
                {
                    return 0; // S_OK
                }

                [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvStdcall) })]
                static IUnknown* Dummy_GetControllingUnknown(IDefragClient* self)
                {
                    return null;
                }

                PInvoke.CoInitializeSecurity(
                    PSECURITY_DESCRIPTOR.Null,
                    -1,
                    null,
                    RPC_C_AUTHN_LEVEL.RPC_C_AUTHN_LEVEL_PKT_PRIVACY,
                    RPC_C_IMP_LEVEL.RPC_C_IMP_LEVEL_IMPERSONATE,
                    null,
                    EOLE_AUTHENTICATION_CAPABILITIES.EOAC_NONE
                    );

                var service = new ServiceController("defragsvc");
                if (service.Status != ServiceControllerStatus.Running)
                {
                    service.Start();
                    service.WaitForStatus(ServiceControllerStatus.Running, TimeSpan.FromSeconds(10));
                }

                IDefragEnginePriv* enginePtr = null;

                var clsid_DefragEngine = CLSID.CLSID_DefragEngine;
                var iid_IDefragEnginePriv = IDefragEnginePriv.Guid;

                // CoCreateInstance to create the COM object instance
                hrInit = PInvoke.CoCreateInstance(
                    clsid_DefragEngine,
                    null,
                    CLSCTX.CLSCTX_LOCAL_SERVER,
                    &iid_IDefragEnginePriv,
                    (void**)&enginePtr);

                if (hrInit.Failed)
                {
                    Debug.WriteLine($"CoCreateInstance failed: 0x{hrInit.Value:X8}");
                    return;
                }

                Debug.WriteLine($"CoCreateInstance succeeded. Interface pointer: 0x{(nint)enginePtr:X}");

                var instanceGuid = Guid.NewGuid();

                Guid partitionGuid = new Guid("4d5f1423-15bf-4e63-9db1-d365ba0d1470");
                Guid diskGUID = new Guid("6077191f-0022-40df-b12b-7771d45d519f");

                HRESULT hr;

                fixed (char* pVol = "\\\\?\\Volume{4d5f1423-15bf-4e63-9db1-d365ba0d1470}\\")
                {
                    hr = enginePtr->Analyze(pVol, &partitionGuid, &diskGUID);
                }

                Debug.WriteLine(hr);

                // Allocate unmanaged memory for vtable (5 methods: QueryInterface, AddRef, Release, ChangeNotification, GetControllingUnknown)
                void** vtable = (void**)NativeMemory.Alloc((nuint)(5 * sizeof(void*)));

                // Assign raw function pointers using delegate* unmanaged
                delegate* unmanaged[Stdcall]<IDefragClient*, Guid*, void**, int> qiPtr = &Dummy_QueryInterface;
                delegate* unmanaged[Stdcall]<IDefragClient*, uint> addRefPtr = &Dummy_AddRef;
                delegate* unmanaged[Stdcall]<IDefragClient*, uint> releasePtr = &Dummy_Release;
                delegate* unmanaged[Stdcall]<IDefragClient*, ulong, uint, void*, int> changeNotificationPtr = &Dummy_ChangeNotification;
                delegate* unmanaged[Stdcall]<IDefragClient*, IUnknown*> getCtrlUnknownPtr = &Dummy_GetControllingUnknown;

                vtable[0] = (void*)qiPtr;
                vtable[1] = (void*)addRefPtr;
                vtable[2] = (void*)releasePtr;
                vtable[3] = (void*)changeNotificationPtr;
                vtable[4] = (void*)getCtrlUnknownPtr;

                // Allocate unmanaged memory for the IDefragClient struct itself
                IDefragClient* client = (IDefragClient*)NativeMemory.Alloc((nuint)sizeof(IDefragClient));
                client->lpVtbl = vtable;

                // Call Register passing pointer to your fake client
                var hr2 = enginePtr->Register(client, &instanceGuid);

                // Clean up later
                NativeMemory.Free(client);
                NativeMemory.Free(vtable);

                Debug.WriteLine(hr2);

                var hr3 = enginePtr->Cancel(instanceGuid);

                Debug.WriteLine(hr3);
            }
        }
        finally
        {

        }
    }

    public void Load()
    {
        if (SettingsHelper.GetValue("FetchMode", "rebound", false))
        {
            FetchArea.Visibility = Microsoft.UI.Xaml.Visibility.Visible;
            var accentBrush = (SolidColorBrush)App.Current.Resources["AccentFillColorDefaultBrush"];
            FetchTextBlock.Inlines.Add(new Run()
            {
                Foreground = accentBrush,
                FontWeight = Microsoft.UI.Text.FontWeights.Bold,
                Text = ViewModel.CurrentUser + "\n"
            });
            FetchTextBlock.Inlines.Add(new Run()
            {
                Foreground = new SolidColorBrush(Colors.White),
                Text = new string('=', ViewModel.CurrentUser.Length) + "\n"
            });
            FetchTextBlock.Inlines.Add(new Run()
            {
                Foreground = accentBrush,
                FontWeight = Microsoft.UI.Text.FontWeights.Bold,
                Text = "OS: "
            });
            FetchTextBlock.Inlines.Add(new Run()
            {
                Foreground = new SolidColorBrush(Colors.White),
                Text = ViewModel.WindowsVersionName + "\n"
            });
            FetchTextBlock.Inlines.Add(new Run()
            {
                Foreground = accentBrush,
                FontWeight = Microsoft.UI.Text.FontWeights.Bold,
                Text = "Windows Version: "
            });
            FetchTextBlock.Inlines.Add(new Run()
            {
                Foreground = new SolidColorBrush(Colors.White),
                Text = ViewModel.DetailedWindowsVersion + "\n"
            });
            if (ViewModel.IsReboundOn)
            {
                FetchTextBlock.Inlines.Add(new Run()
                {
                    Foreground = accentBrush,
                    FontWeight = Microsoft.UI.Text.FontWeights.Bold,
                    Text = "Rebound Version: "
                });
                FetchTextBlock.Inlines.Add(new Run()
                {
                    Foreground = new SolidColorBrush(Colors.White),
                    Text = ReboundVersion.REBOUND_VERSION + "\n"
                });
            }
            FetchTextBlock.Inlines.Add(new Run()
            {
                Foreground = accentBrush,
                FontWeight = Microsoft.UI.Text.FontWeights.Bold,
                Text = "Resolution: "
            });
            FetchTextBlock.Inlines.Add(new Run()
            {
                Foreground = new SolidColorBrush(Colors.White),
                Text = DisplayArea.GetFromWindowId(App.MainAppWindow.AppWindow.Id, DisplayAreaFallback.Primary).WorkArea.Width + "x" + DisplayArea.GetFromWindowId(App.MainAppWindow.AppWindow.Id, DisplayAreaFallback.Primary).WorkArea.Height + "\n"
            });
            FetchTextBlock.Inlines.Add(new Run()
            {
                Foreground = accentBrush,
                FontWeight = Microsoft.UI.Text.FontWeights.Bold,
                Text = "CPU: "
            });
            FetchTextBlock.Inlines.Add(new Run()
            {
                Foreground = new SolidColorBrush(Colors.White),
                Text = ViewModel.CPUName + "\n"
            });
            FetchTextBlock.Inlines.Add(new Run()
            {
                Foreground = accentBrush,
                FontWeight = Microsoft.UI.Text.FontWeights.Bold,
                Text = "GPU: "
            });
            FetchTextBlock.Inlines.Add(new Run()
            {
                Foreground = new SolidColorBrush(Colors.White),
                Text = ViewModel.GPUName + "\n"
            });
            FetchTextBlock.Inlines.Add(new Run()
            {
                Foreground = accentBrush,
                FontWeight = Microsoft.UI.Text.FontWeights.Bold,
                Text = "RAM: "
            });
            FetchTextBlock.Inlines.Add(new Run()
            {
                Foreground = new SolidColorBrush(Colors.White),
                Text = ViewModel.RAM + "\n"
            });
        }
    }

    [RelayCommand]
    private void CopyWindowsVersion() => CopyToClipboard(ViewModel.DetailedWindowsVersion);

    [RelayCommand]
    private void CopyLicenseOwners() => CopyToClipboard(ViewModel.LicenseOwners);

    [RelayCommand]
    private static void CopyReboundVersion() => CopyToClipboard(Helpers.Environment.ReboundVersion.REBOUND_VERSION);

    [RelayCommand]
    private void CloseWindow() => App.MainAppWindow.Close();

    [RelayCommand]
    public async Task ToggleSidebarAsync()
    {
        await Task.Delay(50);
        if (ViewModel.IsSidebarOn)
        {
            for (var i = 0; i <= 100; i += 3)
            {
                await Task.Delay(2);
                var radians = i * Math.PI / 180; // Convert degrees to radians
                App.MainAppWindow.Width = 520 + 200 * Math.Sin(radians);
            }
            App.MainAppWindow.Width = 720;
        }
        else
        {
            for (var i = 100; i >= 0; i -= 3)
            {
                await Task.Delay(2);
                var radians = i * Math.PI / 180; // Convert degrees to radians
                App.MainAppWindow.Width = 520 + 200 * Math.Sin(radians);
            }
            App.MainAppWindow.Width = 520;
        }
    }

    [RelayCommand]
    public async Task ToggleReboundAsync()
    {
        await Task.Delay(50);
        if (ViewModel.IsReboundOn)
        {
            for (var i = 0; i <= 100; i += 3)
            {
                await Task.Delay(2);
                var radians = i * Math.PI / 180; // Convert degrees to radians
                App.MainAppWindow.Height = 500 + 140 * Math.Sin(radians);
            }
            App.MainAppWindow.Height = 640;
        }
        else
        {
            for (var i = 100; i >= 0; i -= 3)
            {
                await Task.Delay(2);
                var radians = i * Math.PI / 180; // Convert degrees to radians
                App.MainAppWindow.Height = 500 + 140 * Math.Sin(radians);
            }
            App.MainAppWindow.Height = 500;
        }
    }

    private static void CopyToClipboard(string content)
    {
        var package = new DataPackage();
        package.SetText(content);
        Clipboard.SetContent(package);
    }

    private void TextBlock_Tapped(object sender, Microsoft.UI.Xaml.Input.TappedRoutedEventArgs e)
    {
        Process.GetCurrentProcess().Kill();
    }
}