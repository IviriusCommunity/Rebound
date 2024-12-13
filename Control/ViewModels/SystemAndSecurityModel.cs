using System;
using System.Diagnostics;
using System.Linq;
using System.Management;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.Win32;
using Windows.System;

#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously

namespace Rebound.Control.ViewModels;

public static class SystemAndSecurityModel
{
    public static async Task<bool> AreUpdatesPending()
    {
        try
        {
            var searcher = new ManagementObjectSearcher(@"root\ccm\SoftwareUpdates\UpdatesStore", "SELECT * FROM CCM_SoftwareUpdate");
            foreach (var update in searcher.Get().Cast<ManagementObject>())
            {
                var status = (int)update["ComplianceState"]; // 1 means pending, 0 means up-to-date
                if (status == 1)
                {
                    return true;
                }
            }
            return false;
        }
        catch
        {
            return false;
        }
    }

    public static async Task<bool> IsDriveEncrypted(string driveLetter)
    {
        try
        {
            var searcher = new ManagementObjectSearcher(@"root\CIMV2\Security\MicrosoftVolumeEncryption", "SELECT * FROM Win32_EncryptableVolume");
            foreach (var volume in searcher.Get().Cast<ManagementObject>())
            {
                var drive = volume["DriveLetter"] as string;
                if (!string.IsNullOrEmpty(drive) && drive.StartsWith(driveLetter, StringComparison.OrdinalIgnoreCase))
                {
                    var protectionStatus = (int)volume["ProtectionStatus"]; // 1 = BitLocker On
                    return protectionStatus == 1;
                }
            }
            return false;
        }
        catch
        {
            return false;
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct USER_MODALS_INFO_0
    {
        public uint usrmod0_min_passwd_len;
        public uint usrmod0_max_passwd_age;
        public uint usrmod0_min_passwd_age;
        public uint usrmod0_force_logoff;
        public uint usrmod0_password_hist_len;
    }

    [DllImport("Netapi32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
    private static extern int NetUserModalsGet(string? servername, int level, out IntPtr bufptr);

    [DllImport("Netapi32.dll", SetLastError = true)]
    private static extern int NetApiBufferFree(IntPtr Buffer);

    public static async Task<bool> IsPasswordComplex()
    {
        var pBuffer = IntPtr.Zero;
        try
        {
            var result = NetUserModalsGet(null, 0, out pBuffer); // Get local password policy
            if (result == 0 && pBuffer != IntPtr.Zero)
            {
                var info = (USER_MODALS_INFO_0?)Marshal.PtrToStructure(pBuffer, typeof(USER_MODALS_INFO_0));
                return info?.usrmod0_min_passwd_len >= 8; // Check for minimum password length of 8 (example)
            }
            return false;
        }
        finally
        {
            if (pBuffer != IntPtr.Zero)
            {
                _ = NetApiBufferFree(pBuffer);
            }
        }
    }

    public static async Task<bool> CheckDefenderStatus(StackPanel SecurityBars)
    {
        try
        {
            // Query WMI for Windows Defender status
            var searcher = new ManagementObjectSearcher(
                @"\\.\root\SecurityCenter2",
                "SELECT * FROM AntivirusProduct");

            var isProtectionEnabled = false;
            foreach (var queryObj in searcher.Get().Cast<ManagementObject>())
            {
                var sev = InfoBarSeverity.Informational;
                var msg = string.Empty;
                var e = await DecodeProductState((int)(uint)queryObj["productState"]);

                switch (e[..1])
                {
                    case "A":
                        sev = InfoBarSeverity.Error;
                        msg = "This antivirus service is disabled.";
                        break;
                    case "B":
                        switch (e.Substring(2, 1))
                        {
                            case "T":
                                sev = InfoBarSeverity.Success;
                                msg = "You're protected. (Real time protection is on.)";
                                break;
                            default:
                                sev = InfoBarSeverity.Informational;
                                msg = "You're protected. (Real time protection is off.)";
                                break;
                        }
                        break;
                    case "C":
                        sev = InfoBarSeverity.Warning;
                        msg = "This antivirus service is snoozed.";
                        break;
                    case "D":
                        sev = InfoBarSeverity.Warning;
                        msg = "Your subscription for your 3rd party antivirus service provider has expired. Please contact them for a new subscription or download another antivirus program.";
                        break;
                    case "E":
                        sev = InfoBarSeverity.Error;
                        msg = "An error occured.";
                        break;
                }

                switch (e.Substring(1, 1))
                {
                    case "T":
                        sev = InfoBarSeverity.Error;
                        msg = "Your computer is infected. Please take action immediately.";
                        break;
                    default:
                        break;
                }

                var exists = false;
                foreach (var bar in SecurityBars.Children.Cast<InfoBar>())
                {
                    if (bar.Title == queryObj["displayName"].ToString() + ":")
                    {
                        exists = true;
                        break;
                    }
                }
                if (exists != true)
                {
                    var x = new InfoBar()
                    {
                        IsOpen = true,
                        IsClosable = false,
                        Severity = sev,
                        Title = queryObj["displayName"].ToString() + ":",
                        Message = msg
                    };
                    SecurityBars.Children.Add(x);
                    if (queryObj["displayName"].ToString() == "Windows Defender")
                    {
                        var y = new Button()
                        {
                            Content = "Open",
                            Margin = new Thickness(0, 0, 0, 15)
                        };
                        y.Click += async (s, e) => { _ = await Launcher.LaunchUriAsync(new Uri("windowsdefender://")); };
                        x.Content = y;
                    }
                    if (sev is InfoBarSeverity.Success or InfoBarSeverity.Informational)
                    {
                        isProtectionEnabled = true;
                    }
                }
            }
            return isProtectionEnabled;
        }
        catch (ManagementException e)
        {
            Debug.WriteLine("An error occurred: " + e.Message);
        }
        return false;
    }

    public static async Task<double> UACStatus()
    {
        try
        {
            using var key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Policies\System");
            if (key != null)
            {
                // Read the EnableLUA value
                var enableLUAValue = key.GetValue("EnableLUA");
                var consentPromptBehaviorAdminValue = key.GetValue("ConsentPromptBehaviorAdmin");
                var consentPromptBehaviorUserValue = key.GetValue("ConsentPromptBehaviorUser");
                var promptOnSecureDesktopValue = key.GetValue("PromptOnSecureDesktop");

                if (enableLUAValue != null)
                {
                    var enableLUA = Convert.ToInt32(enableLUAValue);
                    if (enableLUA == 1)
                    {
                        // UAC is enabled
                        var consentPromptBehaviorAdmin = consentPromptBehaviorAdminValue != null ? Convert.ToInt32(consentPromptBehaviorAdminValue) : -1;
                        var promptOnSecureDesktop = promptOnSecureDesktopValue != null ? Convert.ToInt32(promptOnSecureDesktopValue) : -1;

                        double value = 0;

                        // Determine the UAC level
                        if (consentPromptBehaviorAdmin == 2)
                        {
                            value = 1; // UAC enabled and desktop is dimmed
                        }
                        else if (consentPromptBehaviorAdmin == 5)
                        {
                            if (promptOnSecureDesktop == 1)
                            {
                                value = 0.75; // UAC always prompts for elevation (UAC always on)
                            }
                            else
                            {
                                value = 0.5; // UAC always prompts for elevation (UAC always on)
                            }
                        }
                        else
                        {
                            value = 0; // UAC enabled and desktop is not dimmed
                        }
                        return value;
                    }
                    else
                    {
                        return 0; // UAC is disabled
                    }
                }
                else
                {
                    return -10; // Error: UAC setting not found
                }
            }
            else
            {
                return -100; // Error: Registry key not found
            }
        }
        catch
        {
            return -1000; // Error: Exception occurred
        }
    }

    public static async Task<string> DecodeProductState(int productState)
    {
        // Define the bit masks
        const int ProductStateMask = 0xF000;
        /*const int SignatureStatusMask = 0x00F0;
        const int ProductOwnerMask = 0x0F00;*/

        // Extract bits using bitwise AND and shift right
        var productStateValue = (productState & ProductStateMask) >> 12;
        /*var signatureStatusValue = (productState & SignatureStatusMask) >> 4;
        var productOwnerValue = (productState & ProductOwnerMask) >> 8;*/

        // Decode the values
        var productStateStr = productStateValue switch
        {
            0x0 => "A",        // Off
            0x1 => "B",        // On
            0x2 => "C",        // Snoozed
            0x3 => "D",        // Expired
            _ => "E"           // Unknown
        };

        /*var signatureStatusStr = signatureStatusValue switch
        {
            0x0 => "UpToDate",
            0x1 => "OutOfDate",
            _ => "Unknown"
        };

        var productOwnerStr = productOwnerValue switch
        {
            0x0 => "Non-Microsoft",
            0x1 => "Microsoft",
            _ => "Unknown"
        };*/

        // Define the bit masks based on known values
        /*const int EnabledMask = 0x1;         // Bit 0*/
        const int RealTimeProtectionMask = 0x2; // Bit 1
        /*const int SampleSubmissionMask = 0x4;  // Bit 2
        const int UpToDateMask = 0x8;          // Bit 3*/
        const int MalwareDetectedMask = 0x10;  // Bit 4

        // Extract bits using bitwise AND
        /*bool isEnabled = (productStateValue & EnabledMask) != 0;*/
        var isRealTimeProtectionEnabled = (productStateValue & RealTimeProtectionMask) != 0;
        /*bool isSampleSubmissionEnabled = (productStateValue & SampleSubmissionMask) != 0;
        bool isUpToDate = (productStateValue & UpToDateMask) != 0;*/
        var isMalwareDetected = (productStateValue & MalwareDetectedMask) != 0;

        // Format the result
        return $"{productStateStr}{(isMalwareDetected == true ? "T" : "F")}{(isRealTimeProtectionEnabled == false ? "T" : "F")}";
    }
}