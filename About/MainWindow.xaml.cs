using System;
using System.Collections;
using System.Diagnostics;
using System.Linq;
using System.Management;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;
using Microsoft.Win32;
using Windows.ApplicationModel.DataTransfer;
using WinUIEx;
using static System.Runtime.InteropServices.ComWrappers;

#nullable enable

namespace Rebound.About;
public sealed partial class MainWindow : WindowEx
{
    private MainViewModel ViewModel;

    public MainWindow()
    {
        this.InitializeComponent();
        ViewModel = new MainViewModel();
        AppWindow.DefaultTitleBarShouldMatchAppModeTheme = true;
        IsMaximizable = false;
        IsMinimizable = false;
        MinWidth = 650;
        this.MoveAndResize(25, 25, 650, 690);
        Title = "About Windows";
        IsResizable = false;
        SystemBackdrop = new MicaBackdrop();
        this.SetIcon($"{AppContext.BaseDirectory}\\Assets\\Rebound.ico");
        User.Text = GetCurrentUserName();
        Version.Text = GetDetailedWindowsVersion();
        LegalStuff.Text = ViewModel.GetLegalInfo();
        Load();
    }

    public async void Load()
    {
        await Task.Delay(100);

        this.SetWindowSize(WinverPanel.ActualWidth + 60, 690);
    }

    public static string GetDetailedWindowsVersion()
    {
        try
        {
            // Open the registry key
            using var key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows NT\CurrentVersion");
            if (key != null)
            {
                // Retrieve build number and revision
                var versionName = key.GetValue("DisplayVersion", "Unknown") as string;
                var buildNumber = key.GetValue("CurrentBuildNumber", "Unknown") as string;
                var buildLab = key.GetValue("UBR", "Unknown");

                return $"Version {versionName} (OS Build {buildNumber}.{buildLab})";
            }
        }
        catch (Exception ex)
        {
            return $"Error retrieving OS version details: {ex.Message}";
        }

        return "Registry key not found";
    }
    [GeneratedComInterface(StringMarshalling = StringMarshalling.Utf16)]
    [Guid("3faca0d2-e7f1-4e9c-82a6-404fd6e0aab8")]
    internal partial interface IWMIQuery
    {
        [return: MarshalAs(UnmanagedType.LPWStr)]
        string QueryWMI([MarshalAs(UnmanagedType.LPWStr)] string query);
    }


    [GeneratedComClass]
    internal partial class WMIQuery : IWMIQuery
    {
        public string QueryWMI(string query)
        {
            string result = string.Empty;
            ManagementObjectSearcher searcher = new ManagementObjectSearcher(query);
            ManagementObjectCollection collection = searcher.Get();

            foreach (ManagementObject obj in collection)
            {
                result += obj["Name"] + "\n";
            }

            return result;
        }
    }

    // Define GUIDs for WMI interfaces
    private Guid CLSID_WbemLocator = new Guid("4590F811-1D3A-11D0-891F-00AA004B2E24");
    private Guid IID_IWbemLocator = new Guid("DC12A687-737F-11CF-884D-00AA004B2E24");
    private Guid IID_IWbemServices = new Guid("F27D4D00-4C28-11D0-A2B8-00A0C90A8F39");

    [DllImport("ole32.dll")]
    private static extern int CoInitializeEx(IntPtr pvReserved, int dwCoInit);

    [DllImport("ole32.dll")]
    private static extern void CoUninitialize();

    [DllImport("ole32.dll")]
    private static extern int CoCreateInstance(ref Guid rclsid, IntPtr pUnkOuter, uint dwClsContext, ref Guid riid, out IntPtr ppv);

    [DllImport("oleaut32.dll")]
    private static extern int VariantClear(ref IntPtr pvar);

    private const int COINIT_MULTITHREADED = 0x0;
    private const uint CLSCTX_INPROC_SERVER = 1;

    // IWbemLocator interface
    [ComImport, Guid("DC12A687-737F-11CF-884D-00AA004B2E24")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    public interface IWbemLocator
    {
        [PreserveSig]
        int ConnectServer(
            string bstrNamespace,
            string bstrUser,
            string bstrPassword,
            int lFlags,
            int lLocale,
            string bstrAuthority,
            IntPtr pCtx,
            ref IntPtr ppServices);
    }

    // IWbemServices interface
    [ComImport, Guid("F27D4D00-4C28-11D0-A2B8-00A0C90A8F39")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    public interface IWbemServices
    {
        [PreserveSig]
        int ExecQuery(
            string strQueryLanguage,
            string strQuery,
            int lFlags,
            IntPtr pCtx,
            ref IntPtr ppEnumerator);

        [PreserveSig]
        int Next(
            int lTimeout,
            int uCount,
            ref IntPtr ppObjects,
            ref int plNumReturned);
    }

    // IWbemClassObject interface
    [ComImport, Guid("F6D90B10-7C3C-11D1-8B10-00C04FB6E1D6")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    public interface IWbemClassObject
    {
        [PreserveSig]
        int Get(string strName, int lFlags, ref object pVal, IntPtr pType, IntPtr pFlavor);
    }

    private void PerformWmiQuery()
    {
        try
        {
            IntPtr wbemLocatorPtr = IntPtr.Zero;
            int hr = CoCreateInstance(ref CLSID_WbemLocator, IntPtr.Zero, CLSCTX_INPROC_SERVER, ref IID_IWbemLocator, out wbemLocatorPtr);
            if (hr != 0)
            {
                Debug.WriteLine($"CoCreateInstance failed: 0x{hr:X}");
                return;
            }

            var wbemLocator = Marshal.GetObjectForIUnknown(wbemLocatorPtr) as IWbemLocator;
            if (wbemLocator == null)
            {
                Debug.WriteLine("Failed to get IWbemLocator.");
                return;
            }

            // Connect to the WMI service
            IntPtr wbemServicesPtr = IntPtr.Zero;
            hr = wbemLocator.ConnectServer(
                "ROOT\\CIMV2",  // WMI namespace
                null,           // Username (null means default)
                null,           // Password (null means default)
                0,              // Locale
                0,              // Security flags
                null,           // Authority (null means default)
                IntPtr.Zero,    // Context (null means default)
                ref wbemServicesPtr
            );

            if (hr != 0)
            {
                Debug.WriteLine($"ConnectServer failed: 0x{hr:X}");
                return;
            }

            var wbemServices = Marshal.GetObjectForIUnknown(wbemServicesPtr) as IWbemServices;
            if (wbemServices == null)
            {
                Debug.WriteLine("Failed to get IWbemServices.");
                return;
            }

            // Execute the query
            var query = "SELECT * FROM Win32_OperatingSystem";
            IntPtr pEnumerator = IntPtr.Zero;

            hr = wbemServices.ExecQuery(
                "WQL",      // Query language (WQL in this case)
                query,      // Query string
                0,          // Flags
                IntPtr.Zero, // Context (null means default)
                ref pEnumerator
            );

            if (hr != 0)
            {
                Debug.WriteLine($"ExecQuery failed: 0x{hr:X}");
                return;
            }

            // Iterate through the results
            IntPtr pObj = IntPtr.Zero;
            while ((hr = wbemServices.Next(0, 1, ref pObj, ref hr)) == 0)
            {
                var managementObject = Marshal.GetObjectForIUnknown(pObj) as IWbemClassObject;
                if (managementObject != null)
                {
                    object caption = null;
                    object version = null;
                    object buildNumber = null;

                    hr = managementObject.Get("Caption", 0, ref caption, IntPtr.Zero, IntPtr.Zero);
                    hr = managementObject.Get("Version", 0, ref version, IntPtr.Zero, IntPtr.Zero);
                    hr = managementObject.Get("BuildNumber", 0, ref buildNumber, IntPtr.Zero, IntPtr.Zero);

                    Debug.WriteLine($"OS Caption: {caption}");
                    Debug.WriteLine($"OS Version: {version}");
                    Debug.WriteLine($"OS BuildNumber: {buildNumber}");
                }
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"An error occurred while performing the WMI query: {ex.Message}");
        }
    }

    public static string GetCurrentUserName()
    {
        try
        {
            // Open the registry key
            using var key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows NT\CurrentVersion");
            if (key != null)
            {
                // Retrieve current username
                var owner = key.GetValue("RegisteredOwner", "Unknown") as string;
                var owner2 = key.GetValue("RegisteredOrganization", "Unknown") as string;

                return owner + "\n" + owner2;
            }
        }
        catch (Exception ex)
        {
            return $"Error retrieving OS version details: {ex.Message}";
        }

        return "Registry key not found";
    }

    private async void Button_Click(object sender, RoutedEventArgs e)
    {
        var info = new ProcessStartInfo()
        {
            FileName = "winver",
            UseShellExecute = false,
            CreateNoWindow = true
        };

        var proc = Process.Start(info);

        Close();
    }

    private void Button_Click_1(object sender, RoutedEventArgs e) => Close();

    private void Button_Click_2(object sender, RoutedEventArgs e)
    {
        var content = $@"==========================
---Microsoft {ViewModel.WindowsVersionTitle}---
==========================

{GetDetailedWindowsVersion()}
(c) Microsoft Corporation. All rights reserved.

{ViewModel.WindowsVersionName}

This product is licensed under the [Microsoft Software License Terms] (https://support.microsoft.com/en-us/windows/microsoft-software-license-terms-e26eedad-97a2-5250-2670-aad156b654bd) to: {GetCurrentUserName()}

==========================
--------Rebound 11--------
==========================

{ReboundVer.Text}

Rebound 11 is a Windows mod that does not interfere with the system. The current Windows installation contains additional apps to run Rebound 11.";
        var package = new DataPackage();
        package.SetText(content);
        Clipboard.SetContent(package);
    }
}
