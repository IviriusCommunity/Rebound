// Copyright (C) Ivirius(TM) Community 2020 - 2025. All Rights Reserved.
// Licensed under the MIT License.

using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.Input;
using Microsoft.UI;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Documents;
using Microsoft.UI.Xaml.Media;
using OwlCore.Storage.System.IO;
using Rebound.About.ViewModels;
using Rebound.Forge;
using Rebound.Helpers;
using Rebound.Helpers.Environment;
using Windows.ApplicationModel.DataTransfer;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.System.Com;
using Windows.Win32.System.Memory;
using WinUIEx;

namespace Rebound.About.Views;

public sealed partial class MainPage : Page
{
    private MainViewModel ViewModel { get; } = new();

    public MainPage()
    {
        InitializeComponent();
        Load();

        // I'm too lazy to test RE'd COM somewhere else
        //LoadDefragCOM();
    }

    // Define the CLSID and IID structs
    static readonly Guid CLSID_ProxyStub = new("87CB4E0D-2E2F-4235-BC0A-7C62308011F6");
    static readonly Guid CLSID_DefragTestClass = new("D20A3293-3341-4AE8-9AAF-8E397CB63C34");
    static readonly Guid IID_IDefragEngine = new("0C401E84-3083-4764-B6B5-A0DE8FEDD40C");
    static readonly Guid IID_IDefragClient = new("c958543e-b3a0-46ee-8085-4f111910d401");
    static readonly Guid IID_IAccessible = new("618736E0-3C3D-11CF-810C-00AA00389B71");

    static readonly Guid IID_Test = new("d20a3293-3341-4ae8-9aaf-8e397cb60000");
    static readonly Guid CLSID_Test = new("d20a3293-3341-4ae8-9aaf-8e397cb63c34");

    // You can replace these with real definitions once known
    [StructLayout(LayoutKind.Sequential)]
    public struct MIDL_DUMMY_STATUS
    {
        public int StatusCode;        // 0x00 (likely)
        public int Flags;             // 0x04 (likely)
        public IntPtr Reserved1;      // 0x08
        public IntPtr Reserved2;      // 0x10
        public IntPtr Reserved3;      // 0x18
        public IntPtr Reserved4;      // 0x20
        public IntPtr Reserved5;      // 0x28
        public IntPtr Reserved6;      // 0x30

        public SYSTEMTIME LastTrimTime; // 0x38 (confirmed)
    }
    public struct MIDL_DUMMY_STATISTICS { }
    public struct MIDL_DUMMY_OPTIMIZE_DATA { }

    [ComImport]
    [Guid("0c401e84-3083-4764-b6b5-a0de8fedd40c")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    public unsafe partial interface IDefragEnginePriv
    {
        // ULONG QueryInterface(ref Guid riid, out IntPtr ppvObject); 
        // (Inherited from IUnknown, no need to redeclare if you want)

        // ULONG AddRef();
        // ULONG Release();

        // HRESULT Analyze(ushort const* volumeName, Guid* param1, Guid* param2);
        int Analyze(ushort* volumeName, Guid* param1, Guid* param2);

        // HRESULT BootOptimize(ushort const* volumeName, Guid* param1, Guid* param2);
        int BootOptimize(ushort* volumeName, Guid* param1, Guid* param2);

        // HRESULT Cancel(Guid id);
        int Cancel(Guid id);

        // HRESULT DefragmentFull(ushort const* volumeName, int flags, Guid* param1, Guid* param2);
        int DefragmentFull(ushort* volumeName, int flags, Guid* param1, Guid* param2);

        // HRESULT DefragmentFile(ushort const* fileName, Guid* param);
        int DefragmentFile(ushort* fileName, Guid* param);

        // HRESULT DefragmentSimple(ushort const* volumeName, Guid* param1, Guid* param2);
        int DefragmentSimple(ushort* volumeName, Guid* param1, Guid* param2);

        // HRESULT GetPossibleShrinkSpace(ushort const* volumeName, Guid* param);
        int GetPossibleShrinkSpace(ushort* volumeName, Guid* param);

        // HRESULT Consolidate(ushort const* volumeName, Guid* param1, Guid* param2);
        int Consolidate(ushort* volumeName, Guid* param1, Guid* param2);

        // HRESULT Shrink(ushort const* volumeName, ulong size1, ulong size2, Guid* param);
        int Shrink(ushort* volumeName, ulong size1, ulong size2, Guid* param);

        // HRESULT Retrim(ushort const* volumeName, int flags, Guid* param1, Guid* param2);
        int Retrim(ushort* volumeName, int flags, Guid* param1, Guid* param2);

        // HRESULT Slabify(ushort const* volumeName, int flags, Guid* param1, Guid* param2);
        int Slabify(ushort* volumeName, int flags, Guid* param1, Guid* param2);

        // HRESULT SlabifyRetrim(ushort const* volumeName, int flags, Guid* param1, Guid* param2);
        int SlabifyRetrim(ushort* volumeName, int flags, Guid* param1, Guid* param2);

        // HRESULT SlabAnalyze(ushort const* volumeName, Guid* param1, Guid* param2);
        int SlabAnalyze(ushort* volumeName, Guid* param1, Guid* param2);

        // HRESULT GetStatus(ushort const* volumeName, uint* param1, __MIDL___MIDL_itf_dfengine_0000_0000_0005** param2);
        int GetStatus(ushort* volumeName, uint* param1, IntPtr* param2);

        // HRESULT GetVolumeStatistics(ushort const* volumeName, __MIDL___MIDL_itf_dfengine_0000_0000_0006* param);
        int GetVolumeStatistics(ushort* volumeName, IntPtr param);

        // HRESULT Register(IDefragClient* client, Guid* param);
        int Register(IntPtr client, Guid* param);

        // HRESULT Unregister(Guid id);
        int Unregister(Guid id);

        // HRESULT WaitForCompletion(Guid id, int* param);
        int WaitForCompletion(Guid id, int* param);

        // HRESULT DisableAutomaticSleep(Guid id);
        int DisableAutomaticSleep(Guid id);

        // HRESULT TierOptimize(__MIDL___MIDL_itf_dfengine_0000_0000_0007 param1, Guid* param2);
        int TierOptimize(IntPtr param1, Guid* param2);

        // HRESULT BootOptimize2(ushort const* param1, ushort const* param2, Guid* param3, Guid* param4);
        int BootOptimize2(ushort* param1, ushort* param2, Guid* param3, Guid* param4);

        // HRESULT Shutdown(void);
        int Shutdown();

        [PreserveSig]
        void NotifyVolumeChange();

        [PreserveSig]
        void WaitForEvents();

        // IUnknown* GetControllingUnknown(void);
        IntPtr GetControllingUnknown();
    }
    [StructLayout(LayoutKind.Sequential)]
    public struct SYSTEMTIME
    {
        public ushort wYear;         // 0x00
        public ushort wMonth;        // 0x02
        public ushort wDayOfWeek;    // 0x04
        public ushort wDay;          // 0x06
        public ushort wHour;         // 0x08
        public ushort wMinute;       // 0x0A
        public ushort wSecond;       // 0x0C
        public ushort wMilliseconds; // 0x0E
    }
    [ComImport]
    [Guid("d20a3293-3341-4ae8-9aaf-8e397cb63c34")]  // CLSID_DefragEngine
    [ClassInterface(ClassInterfaceType.None)]
    public class DefragEngine { }

    public unsafe void LoadDefragCOM()
    {
        // Initialize COM for this thread
        HRESULT hrInit = PInvoke.CoInitializeEx(null, COINIT.COINIT_APARTMENTTHREADED);
        if (hrInit.Failed)
        {
            Debug.WriteLine($"CoInitializeEx failed: 0x{hrInit.Value:X8}");
            return;
        }

        try
        {
            // Define your interface and CLSIDs (replace with actual GUIDs)
            Guid IID_IDefragEngine = new("0c401e84-3083-4764-b6b5-a0de8fedd40c"); // interface GUID
            Guid CLSID_DefragEngine = new("d20a3293-3341-4ae8-9aaf-8e397cb63c34"); // class GUID

            // Pointer for the interface
            IDefragEnginePriv* enginePtr = null;

            // CoCreateInstance to create the COM object instance
            hrInit = PInvoke.CoCreateInstance(
                &CLSID_DefragEngine,
                null,
                CLSCTX.CLSCTX_LOCAL_SERVER,
                &IID_IDefragEngine,
                (void**)&enginePtr);

            if (hrInit.Failed || enginePtr == null)
            {
                Debug.WriteLine($"CoCreateInstance failed: 0x{hrInit.Value:X8}");
                return;
            }

            Debug.WriteLine($"CoCreateInstance succeeded. Interface pointer: 0x{(nint)enginePtr:X}");

            /*// Call WaitForEvents (HRESULT return)
            enginePtr->NotifyVolumeChange();
            enginePtr->WaitForEvents();

            // Release the interface pointer when done
            Marshal.Release((IntPtr)enginePtr);*/
        }
        finally
        {
            // Uninitialize COM
            PInvoke.CoUninitialize();
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