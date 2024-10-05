using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using Microsoft.Win32;
using System.Management;
using Windows.Foundation;
using Windows.Foundation.Collections;
using System.Reflection.Metadata.Ecma335;
using System.Diagnostics;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace ReboundSysInfo.Views;
/// <summary>
/// An empty page that can be used on its own or navigated to within a Frame.
/// </summary>
public sealed partial class SystemInformationPage : Page
{
    public SystemInformationPage()
    {
        this.InitializeComponent();
        var manufacturer = Microsoft.Win32.Registry.LocalMachine.OpenSubKey("HARDWARE\\DESCRIPTION\\System\\BIOS").GetValue("SystemManufacturer");
        var model = Microsoft.Win32.Registry.LocalMachine.OpenSubKey("HARDWARE\\DESCRIPTION\\System\\BIOS").GetValue("SystemProductName");
        wallBox.UriSource = new Uri(GetCurrentWallpaper());
        deviceName.Text = (string)model;
        manufacturerName.Text = (string)manufacturer;
        DeviceName.Text = (string)model + $" ({Environment.MachineName})";
        cpu.Text = GetCPUSpecs("Name");
        ram.Text = GetRAMAmount();
        WindowsVersion.Text = Environment.OSVersion.ToString();
        CPUModel.Text = GetCPUSpecs("Name");
        CPUCores.Text = GetCPUSpecs("NoCores");
        CPUUtil.Text = GetCPUSpecs("UtilPercent");
        RAMAmount.Text = GetRAMAmount();
        RAMTypeText.Text = RAMType;
        RAMUtil.Text = GetRAMUtil();
    }

    public static string GetCPUSpecs(string param) {
        var cpu =
    new ManagementObjectSearcher("select * from Win32_Processor")
    .Get()
    .Cast<ManagementObject>()
    .First();
        if (param == "Name")
        {
            var ProcessorName = (string)cpu["Name"];

            ProcessorName =
               ProcessorName
               .Replace("(TM)", "™")
               .Replace("(tm)", "™")
               .Replace("(R)", "®")
               .Replace("(r)", "®")
               .Replace("(C)", "©")
               .Replace("(c)", "©")
               .Replace("    ", " ")
               .Replace("  ", " ");
            return ProcessorName;
        }
        else if (param == "NoCores")
        {
            var ProcessorCores = cpu["NumberOfCores"].ToString();
            return ProcessorCores;
        }
        else if (param == "UtilPercent") {
            PerformanceCounter cpuCounter;
            cpuCounter = new PerformanceCounter("Processor", "% Processor Time", "_Total");
            var cpu_util = cpuCounter.NextValue() + "%";
            System.Threading.Thread.Sleep(10);
            cpu_util = cpuCounter.NextValue() + "%";
            return cpu_util;
        }
        else {
            return "";
        }
    }

    public string GetRAMAmount()
    {

        ObjectQuery objectQuery = new ObjectQuery("SELECT * FROM Win32_OperatingSystem");
        ManagementObjectSearcher managementObjectSearcher = new ManagementObjectSearcher(objectQuery);
        ManagementObjectCollection managementObjectCollection = managementObjectSearcher.Get();
        var amount = "";
        foreach (ManagementObject managementObject in managementObjectCollection)
        {
            var MemorySize = (UInt64)managementObject["TotalVisibleMemorySize"] / 1048576;
            amount = $"{MemorySize}GB RAM";
        }
        return amount;
    }

    public string GetRAMUtil()
    {
        PerformanceCounter ramCounter;
        ramCounter = new PerformanceCounter("Memory", "% Committed Bytes In Use", null);
        var ram_util = ramCounter.NextValue() + "%";
        System.Threading.Thread.Sleep(10);
        ram_util = ramCounter.NextValue() + "%";
        return ram_util;
    }

    public static string RAMType
    {
        get
        {
            int type = 0;

            ConnectionOptions connection = new ConnectionOptions();
            connection.Impersonation = ImpersonationLevel.Impersonate;
            ManagementScope scope = new ManagementScope("\\\\.\\root\\CIMV2", connection);
            scope.Connect();
            ObjectQuery query = new ObjectQuery("SELECT * FROM Win32_PhysicalMemory");
            ManagementObjectSearcher searcher = new ManagementObjectSearcher(scope, query);
            foreach (ManagementObject queryObj in searcher.Get())
            {
                type = Convert.ToInt32(queryObj["MemoryType"]);
            }

            return TypeString(type);
        }
    }

    private static string TypeString(int type)
    {
        string outValue = string.Empty;

        switch (type)
        {
            case 0x0: outValue = "Unknown"; break;
            case 0x1: outValue = "Other"; break;
            case 0x2: outValue = "DRAM"; break;
            case 0x3: outValue = "Synchronous DRAM"; break;
            case 0x4: outValue = "Cache DRAM"; break;
            case 0x5: outValue = "EDO"; break;
            case 0x6: outValue = "EDRAM"; break;
            case 0x7: outValue = "VRAM"; break;
            case 0x8: outValue = "SRAM"; break;
            case 0x9: outValue = "RAM"; break;
            case 0xa: outValue = "ROM"; break;
            case 0xb: outValue = "Flash"; break;
            case 0xc: outValue = "EEPROM"; break;
            case 0xd: outValue = "FEPROM"; break;
            case 0xe: outValue = "EPROM"; break;
            case 0xf: outValue = "CDRAM"; break;
            case 0x10: outValue = "3DRAM"; break;
            case 0x11: outValue = "SDRAM"; break;
            case 0x12: outValue = "SGRAM"; break;
            case 0x13: outValue = "RDRAM"; break;
            case 0x14: outValue = "DDR"; break;
            case 0x15: outValue = "DDR2"; break;
            case 0x16: outValue = "DDR2 FB-DIMM"; break;
            case 0x17: outValue = "Undefined 23"; break;
            case 0x18: outValue = "DDR3"; break;
            case 0x19: outValue = "FBD2"; break;
            case 0x1a: outValue = "DDR4"; break;
            default: outValue = "Undefined"; break;
        }

        return outValue;
    }

    private string GetCurrentWallpaper()

    {

          // The current wallpaper path is stored in the registry at HKEY_CURRENT_USER\\Control Panel\\Desktop\\WallPaper

          RegistryKey rkWallPaper = Registry.CurrentUser.OpenSubKey("Control Panel\\Desktop", false);

          string WallpaperPath = rkWallPaper.GetValue("WallPaper").ToString();

         rkWallPaper.Close();

          // Return the current wallpaper path

          return WallpaperPath;

    }
}
