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

namespace Rebound.Control.ViewModels;

public static class SystemAndSecurityModel
{
    [ComImport, Guid("F7898AF5-CAC4-4632-A2EC-DA06E5111AF2"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    public interface INetFwPolicy2
    {
        int CurrentProfileTypes
        {
            get;
        }
        bool get_FirewallEnabled(NET_FW_PROFILE_TYPE2 profileType);
        void put_FirewallEnabled(NET_FW_PROFILE_TYPE2 profileType, bool enabled);
        bool get_ExcludedInterfaces(NET_FW_PROFILE_TYPE2 profileType);
        void put_ExcludedInterfaces(NET_FW_PROFILE_TYPE2 profileType, bool excludedInterfaces);
        bool get_BlockAllInboundTraffic(NET_FW_PROFILE_TYPE2 profileType);
        void put_BlockAllInboundTraffic(NET_FW_PROFILE_TYPE2 profileType, bool block);
        bool get_NotificationsDisabled(NET_FW_PROFILE_TYPE2 profileType);
        void put_NotificationsDisabled(NET_FW_PROFILE_TYPE2 profileType, bool disabled);
        bool get_UnicastResponsesToMulticastBroadcastDisabled(NET_FW_PROFILE_TYPE2 profileType);
        void put_UnicastResponsesToMulticastBroadcastDisabled(NET_FW_PROFILE_TYPE2 profileType, bool disabled);
    }

    public enum NET_FW_PROFILE_TYPE2
    {
        NET_FW_PROFILE2_DOMAIN = 1,
        NET_FW_PROFILE2_PRIVATE = 2,
        NET_FW_PROFILE2_PUBLIC = 4
    }

    public static async Task<bool> AreUpdatesPending()
    {
        try
        {
            ManagementObjectSearcher searcher = new ManagementObjectSearcher(@"root\ccm\SoftwareUpdates\UpdatesStore", "SELECT * FROM CCM_SoftwareUpdate");
            foreach (ManagementObject update in searcher.Get())
            {
                int status = (int)update["ComplianceState"]; // 1 means pending, 0 means up-to-date
                if (status == 1)
                    return true;
            }
            return false;
        }
        catch (Exception ex)
        {
            Console.WriteLine("Error checking updates: " + ex.Message);
            return false;
        }
    }

    public static async Task<bool> IsDriveEncrypted(string driveLetter)
    {
        try
        {
            ManagementObjectSearcher searcher = new ManagementObjectSearcher(@"root\CIMV2\Security\MicrosoftVolumeEncryption", "SELECT * FROM Win32_EncryptableVolume");
            foreach (ManagementObject volume in searcher.Get())
            {
                string drive = volume["DriveLetter"] as string;
                if (!string.IsNullOrEmpty(drive) && drive.StartsWith(driveLetter, StringComparison.OrdinalIgnoreCase))
                {
                    int protectionStatus = (int)volume["ProtectionStatus"]; // 1 = BitLocker On
                    return protectionStatus == 1;
                }
            }
            return false;
        }
        catch (Exception ex)
        {
            Console.WriteLine("Error checking encryption: " + ex.Message);
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

    [DllImport("Netapi32.dll", SetLastError = true, CharSet = CharSet.Auto)]
    private static extern int NetUserModalsGet(string servername, int level, out IntPtr bufptr);

    [DllImport("Netapi32.dll", SetLastError = true)]
    private static extern int NetApiBufferFree(IntPtr Buffer);

    public static async Task<bool> IsPasswordComplex()
    {
        IntPtr pBuffer = IntPtr.Zero;
        try
        {
            int result = NetUserModalsGet(null, 0, out pBuffer); // Get local password policy
            if (result == 0 && pBuffer != IntPtr.Zero)
            {
                USER_MODALS_INFO_0 info = (USER_MODALS_INFO_0)Marshal.PtrToStructure(pBuffer, typeof(USER_MODALS_INFO_0));
                return info.usrmod0_min_passwd_len >= 8; // Check for minimum password length of 8 (example)
            }
            return false;
        }
        finally
        {
            if (pBuffer != IntPtr.Zero)
                NetApiBufferFree(pBuffer);
        }
    }

    public static async Task<bool> CheckDefenderStatus(StackPanel SecurityBars)
    {
        try
        {
            // Query WMI for Windows Defender status
            ManagementObjectSearcher searcher = new ManagementObjectSearcher(
                @"\\.\root\SecurityCenter2",
                "SELECT * FROM AntivirusProduct");

            bool isProtectionEnabled = false;
            foreach (ManagementObject queryObj in searcher.Get())
            {
                Debug.WriteLine("Name: {0}", queryObj["displayName"]);
                Debug.WriteLine("Product State: {0}", queryObj["productState"]);
                Debug.WriteLine("Timestamp: {0}", DateTime.Now);
                InfoBarSeverity sev = InfoBarSeverity.Informational;
                string e = await DecodeProductState((int)((uint)queryObj["productState"]));
                if (e.Substring(0, 1) == "A") sev = InfoBarSeverity.Error;
                if (e.Substring(0, 1) == "B") sev = InfoBarSeverity.Success;
                if (e.Substring(0, 1) == "C") sev = InfoBarSeverity.Warning;
                if (e.Substring(0, 1) == "D") sev = InfoBarSeverity.Warning;
                if (e.Substring(0, 1) == "E") sev = InfoBarSeverity.Informational;
                string msg = string.Empty;
                if (e.Substring(0, 1) == "A") msg = "This antivirus service is disabled.";
                if (e.Substring(0, 1) == "B") msg = "You're protected.";
                if (e.Substring(0, 1) == "C") msg = "This antivirus service is snoozed.";
                if (e.Substring(0, 1) == "D") msg = "Your subscription for your 3rd party antivirus service provider has expired. Please contact them for a new subscription or download another antivirus program.";
                if (e.Substring(0, 1) == "E") msg = "Unknown.";
                bool exists = false;
                foreach (InfoBar bar in SecurityBars.Children.Cast<InfoBar>())
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
                        y.Click += async (s, e) => { await Launcher.LaunchUriAsync(new Uri("windowsdefender://")); };
                        x.Content = y;
                    }
                    if (sev == InfoBarSeverity.Success) isProtectionEnabled = true;
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
            using (RegistryKey key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Policies\System"))
            {
                if (key != null)
                {
                    // Read the EnableLUA value
                    object enableLUAValue = key.GetValue("EnableLUA");
                    object consentPromptBehaviorAdminValue = key.GetValue("ConsentPromptBehaviorAdmin");
                    object consentPromptBehaviorUserValue = key.GetValue("ConsentPromptBehaviorUser");
                    object promptOnSecureDesktopValue = key.GetValue("PromptOnSecureDesktop");

                    if (enableLUAValue != null)
                    {
                        int enableLUA = Convert.ToInt32(enableLUAValue);
                        if (enableLUA == 1)
                        {
                            // UAC is enabled
                            int consentPromptBehaviorAdmin = consentPromptBehaviorAdminValue != null ? Convert.ToInt32(consentPromptBehaviorAdminValue) : -1;
                            int promptOnSecureDesktop = promptOnSecureDesktopValue != null ? Convert.ToInt32(promptOnSecureDesktopValue) : -1;

                            double value = 0;

                            // Determine the UAC level
                            if (consentPromptBehaviorAdmin == 2)
                            {
                                value = 1; // UAC enabled and desktop is dimmed
                            }
                            else if (consentPromptBehaviorAdmin == 5)
                            {
                                if (promptOnSecureDesktop == 1) value = 0.75; // UAC always prompts for elevation (UAC always on)
                                else value = 0.5; // UAC always prompts for elevation (UAC always on)
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
        }
        catch (Exception ex)
        {
            Debug.WriteLine("An error occurred while checking UAC status: " + ex.Message);
            return -1000; // Error: Exception occurred
        }
    }

    public static async Task<string> DecodeProductState(int productState)
    {
        // Define the bit masks
        const int ProductStateMask = 0xF000;
        const int SignatureStatusMask = 0x00F0;
        const int ProductOwnerMask = 0x0F00;

        // Extract bits using bitwise AND and shift right
        int productStateValue = (productState & ProductStateMask) >> 12;
        int signatureStatusValue = (productState & SignatureStatusMask) >> 4;
        int productOwnerValue = (productState & ProductOwnerMask) >> 8;

        // Decode the values
        string productStateStr = productStateValue switch
        {
            0x0 => "A",        // Off
            0x1 => "B",        // On
            0x2 => "C",        // Snoozed
            0x3 => "D",        // Expired
            _ => "E"           // Unknown
        };

        string signatureStatusStr = signatureStatusValue switch
        {
            0x0 => "UpToDate",
            0x1 => "OutOfDate",
            _ => "Unknown"
        };

        string productOwnerStr = productOwnerValue switch
        {
            0x0 => "Non-Microsoft",
            0x1 => "Microsoft",
            _ => "Unknown"
        };

        // Define the bit masks based on known values
        const int EnabledMask = 0x1;          // Bit 0
        const int RealTimeProtectionMask = 0x2; // Bit 1
        const int SampleSubmissionMask = 0x4;  // Bit 2
        const int UpToDateMask = 0x8;          // Bit 3
        const int MalwareDetectedMask = 0x10;  // Bit 4

        // Extract bits using bitwise AND
        bool isEnabled = (productStateValue & EnabledMask) != 0;
        bool isRealTimeProtectionEnabled = (productStateValue & RealTimeProtectionMask) != 0;
        bool isSampleSubmissionEnabled = (productStateValue & SampleSubmissionMask) != 0;
        bool isUpToDate = (productStateValue & UpToDateMask) != 0;
        bool isMalwareDetected = (productStateValue & MalwareDetectedMask) != 0;

        //return Convert.ToString(productState, 2);

        // Format the result
        return $"{productStateStr}";
    }
}