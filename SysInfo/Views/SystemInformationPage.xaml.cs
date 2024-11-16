using System.Diagnostics;
using System.Management;
using Microsoft.Win32;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace Rebound.SysInfo.Views;
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

    public static string GetCPUSpecs(string param)
    {
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
        else if (param == "UtilPercent")
        {
            PerformanceCounter cpuCounter;
            cpuCounter = new PerformanceCounter("Processor", "% Processor Time", "_Total");
            _ = cpuCounter.NextValue() + "%";
            System.Threading.Thread.Sleep(10);
            var cpu_util = cpuCounter.NextValue() + "%";
            return cpu_util;
        }
        else
        {
            return "";
        }
    }

    public string GetRAMAmount()
    {

        var objectQuery = new ObjectQuery("SELECT * FROM Win32_OperatingSystem");
        var managementObjectSearcher = new ManagementObjectSearcher(objectQuery);
        var managementObjectCollection = managementObjectSearcher.Get();
        var amount = "";
        foreach (ManagementObject managementObject in managementObjectCollection)
        {
            var MemorySize = (ulong)managementObject["TotalVisibleMemorySize"] / 1048576;
            amount = $"{MemorySize}GB RAM";
        }
        return amount;
    }

    public string GetRAMUtil()
    {
        PerformanceCounter ramCounter;
        ramCounter = new PerformanceCounter("Memory", "% Committed Bytes In Use", null);
        _ = ramCounter.NextValue() + "%";
        System.Threading.Thread.Sleep(10);
        var ram_util = ramCounter.NextValue() + "%";
        return ram_util;
    }

    public static string RAMType
    {
        get
        {
            var type = 0;

            var connection = new ConnectionOptions
            {
                Impersonation = ImpersonationLevel.Impersonate
            };
            var scope = new ManagementScope("\\\\.\\root\\CIMV2", connection);
            scope.Connect();
            var query = new ObjectQuery("SELECT * FROM Win32_PhysicalMemory");
            var searcher = new ManagementObjectSearcher(scope, query);
            foreach (ManagementObject queryObj in searcher.Get())
            {
                type = Convert.ToInt32(queryObj["MemoryType"]);
            }

            return TypeString(type);
        }
    }

    private static string TypeString(int type)
    {
        var outValue = type switch
        {
            0x0 => "Unknown",
            0x1 => "Other",
            0x2 => "DRAM",
            0x3 => "Synchronous DRAM",
            0x4 => "Cache DRAM",
            0x5 => "EDO",
            0x6 => "EDRAM",
            0x7 => "VRAM",
            0x8 => "SRAM",
            0x9 => "RAM",
            0xa => "ROM",
            0xb => "Flash",
            0xc => "EEPROM",
            0xd => "FEPROM",
            0xe => "EPROM",
            0xf => "CDRAM",
            0x10 => "3DRAM",
            0x11 => "SDRAM",
            0x12 => "SGRAM",
            0x13 => "RDRAM",
            0x14 => "DDR",
            0x15 => "DDR2",
            0x16 => "DDR2 FB-DIMM",
            0x17 => "Undefined 23",
            0x18 => "DDR3",
            0x19 => "FBD2",
            0x1a => "DDR4",
            _ => "Undefined",
        };
        return outValue;
    }

    private string GetCurrentWallpaper()

    {

        // The current wallpaper path is stored in the registry at HKEY_CURRENT_USER\\Control Panel\\Desktop\\WallPaper

        var rkWallPaper = Registry.CurrentUser.OpenSubKey("Control Panel\\Desktop", false);

        var WallpaperPath = rkWallPaper.GetValue("WallPaper").ToString();

        rkWallPaper.Close();

        // Return the current wallpaper path

        return WallpaperPath;

    }
}
