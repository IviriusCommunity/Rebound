using System;
using System.Diagnostics;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Documents;
using Microsoft.UI.Xaml.Media;
using Microsoft.Win32;
using Rebound.Control;
using WinUIEx;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace Rebound;
/// <summary>
/// An empty window that can be used on its own or navigated to within a Frame.
/// </summary>
public sealed partial class UACWindow : WindowEx
{
    public UACWindow()
    {
        InitializeComponent();
        IsMaximizable = false;
        this.SetWindowSize(750, 500);
        this.CenterOnScreen();
        SystemBackdrop = new MicaBackdrop();
        this.SetIcon($"{AppContext.BaseDirectory}\\Assets\\AppIcons\\imageres_78.ico");
        Helpers.Win32.SetDarkMode(this, App.Current);
        Title = "User Account Control Settings";
        IsResizable = false;
        UACConfigurator.UpdateUACSlider(UACSlider);
        if (UACSlider.Value == 0)
        {
            UACInfoBar.Title = "Never notify me when:";
            UACBlock.Inlines.Clear();
            UACBlock.Inlines.Add(new Run()
            {
                Text = "-   Apps try to install software or make changes to my computer"
            });
            UACBlock.Inlines.Add(new LineBreak());
            UACBlock.Inlines.Add(new Run()
            {
                Text = "-   I make changes to Windows settings"
            });
            RecommendedBar.Severity = InfoBarSeverity.Error;
            RecommendedBar.Title = "Not recommended.";
        }
    }

    private void UACSlider_ValueChanged(object sender, RangeBaseValueChangedEventArgs e)
    {
        if (UACSlider.Value == 0)
        {
            UACInfoBar.Title = "Never notify me when:";
            UACBlock.Inlines.Clear();
            UACBlock.Inlines.Add(new Run()
            {
                Text = "-   Apps try to install software or make changes to my computer"
            });
            UACBlock.Inlines.Add(new LineBreak());
            UACBlock.Inlines.Add(new Run()
            {
                Text = "-   I make changes to Windows settings"
            });
            RecommendedBar.Severity = InfoBarSeverity.Error;
            RecommendedBar.Title = "Not recommended.";
        }
        if (UACSlider.Value == 1)
        {
            UACInfoBar.Title = "Notify me only when apps try to make changes to my computer (do not dim my desktop)";
            UACBlock.Inlines.Clear();
            UACBlock.Inlines.Add(new Run()
            {
                Text = "-   Don't notify me when I make changes to Windows settings"
            });
            RecommendedBar.Severity = InfoBarSeverity.Warning;
            RecommendedBar.Title = "Not recommended. Choose this only if it takes a long time to dim the desktop on your computer.";
        }
        if (UACSlider.Value == 2)
        {
            UACInfoBar.Title = "Notify me only when apps try to make changes to my computer (default)";
            UACBlock.Inlines.Clear();
            UACBlock.Inlines.Add(new Run()
            {
                Text = "-   Don't notify me when I make changes to Windows settings"
            });
            RecommendedBar.Severity = InfoBarSeverity.Success;
            RecommendedBar.Title = "Recommended if you use familiar apps and visit familiar websites.";
        }
        if (UACSlider.Value == 3)
        {
            UACInfoBar.Title = "Always notify me when:";
            UACBlock.Inlines.Clear();
            UACBlock.Inlines.Add(new Run()
            {
                Text = "-   Apps try to install software or make changes to my computer"
            });
            UACBlock.Inlines.Add(new LineBreak());
            UACBlock.Inlines.Add(new Run()
            {
                Text = "-   I make changes to Windows settings"
            });
            RecommendedBar.Severity = InfoBarSeverity.Success;
            RecommendedBar.Title = "Recommended if you routinely install new software and visit unfamiliar websites.";
        }
    }

    private void Button_Click(object sender, RoutedEventArgs e) => Close();

    private void Button_Click_1(object sender, RoutedEventArgs e)
    {
        switch (UACSlider.Value)
        {
            case 3:
                {
                    UACConfigurator.SetAlwaysNotify();
                    Close();
                    break;
                }
            case 2:
                {
                    UACConfigurator.SetNotifyWithDim();
                    Close();
                    break;
                }
            case 1:
                {
                    UACConfigurator.SetNotifyWithoutDim();
                    Close();
                    break;
                }
            case 0:
                {
                    UACConfigurator.SetNeverNotify();
                    Close();
                    break;
                }
        }
    }
}
public class UACConfigurator
{
    private const string RegistryKeyPath = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Policies\System";

    private static void SetRegistryValue(string valueName, int value)
    {
        try
        {
            using var key = Registry.LocalMachine.CreateSubKey(RegistryKeyPath);
            key?.SetValue(valueName, value, RegistryValueKind.DWord);
        }
        catch (Exception ex)
        {
            // Handle exceptions (e.g., lack of permissions)
            Console.WriteLine("An error occurred while modifying UAC settings: " + ex.Message);
        }
    }

    public static void RunPowerShellCommand(string command)
    {
        var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = "powershell.exe",
                Arguments = $"-Command \"{command}\"",
                UseShellExecute = true,
                CreateNoWindow = true,
                WindowStyle = ProcessWindowStyle.Hidden,
                Verb = "runas"
            }
        };

        _ = process.Start();
        process.WaitForExit();
    }

    public static void SetAlwaysNotify()
    {
        var command = @"
            Set-ItemProperty -Path 'HKLM:\SOFTWARE\Microsoft\Windows\CurrentVersion\Policies\System' -Name 'EnableLUA' -Value 1;
            Set-ItemProperty -Path 'HKLM:\SOFTWARE\Microsoft\Windows\CurrentVersion\Policies\System' -Name 'ConsentPromptBehaviorAdmin' -Value 2;
            Set-ItemProperty -Path 'HKLM:\SOFTWARE\Microsoft\Windows\CurrentVersion\Policies\System' -Name 'PromptOnSecureDesktop' -Value 1;
        ";
        RunPowerShellCommand(command);
    }

    public static void SetNotifyWithDim()
    {
        var command = @"
            Set-ItemProperty -Path 'HKLM:\SOFTWARE\Microsoft\Windows\CurrentVersion\Policies\System' -Name 'EnableLUA' -Value 1;
            Set-ItemProperty -Path 'HKLM:\SOFTWARE\Microsoft\Windows\CurrentVersion\Policies\System' -Name 'ConsentPromptBehaviorAdmin' -Value 5;
            Set-ItemProperty -Path 'HKLM:\SOFTWARE\Microsoft\Windows\CurrentVersion\Policies\System' -Name 'PromptOnSecureDesktop' -Value 1;
        ";
        RunPowerShellCommand(command);
    }

    public static void SetNotifyWithoutDim()
    {
        var command = @"
            Set-ItemProperty -Path 'HKLM:\SOFTWARE\Microsoft\Windows\CurrentVersion\Policies\System' -Name 'EnableLUA' -Value 1;
            Set-ItemProperty -Path 'HKLM:\SOFTWARE\Microsoft\Windows\CurrentVersion\Policies\System' -Name 'ConsentPromptBehaviorAdmin' -Value 5;
            Set-ItemProperty -Path 'HKLM:\SOFTWARE\Microsoft\Windows\CurrentVersion\Policies\System' -Name 'PromptOnSecureDesktop' -Value 0;
        ";
        RunPowerShellCommand(command);
    }

    public static void SetNeverNotify()
    {
        var command = @"
            Set-ItemProperty -Path 'HKLM:\SOFTWARE\Microsoft\Windows\CurrentVersion\Policies\System' -Name 'EnableLUA' -Value 1;
            Set-ItemProperty -Path 'HKLM:\SOFTWARE\Microsoft\Windows\CurrentVersion\Policies\System' -Name 'ConsentPromptBehaviorAdmin' -Value 0;
            Set-ItemProperty -Path 'HKLM:\SOFTWARE\Microsoft\Windows\CurrentVersion\Policies\System' -Name 'PromptOnSecureDesktop' -Value 0;
        ";
        RunPowerShellCommand(command);
    }

    private static int GetUACState()
    {
        try
        {
            using var key = Registry.LocalMachine.OpenSubKey(RegistryKeyPath);
            if (key != null)
            {
                // Read the EnableLUA value
                var enableLUAValue = key.GetValue("EnableLUA");
                var consentPromptBehaviorAdminValue = key.GetValue("ConsentPromptBehaviorAdmin");
                var promptOnSecureDesktopValue = key.GetValue("PromptOnSecureDesktop");

                if (enableLUAValue != null)
                {
                    var enableLUA = Convert.ToInt32(enableLUAValue);
                    if (enableLUA == 1)
                    {
                        var consentPromptBehaviorAdmin = consentPromptBehaviorAdminValue != null ? Convert.ToInt32(consentPromptBehaviorAdminValue) : -1;
                        var promptOnSecureDesktop = promptOnSecureDesktopValue != null ? Convert.ToInt32(promptOnSecureDesktopValue) : -1;

                        // Determine the UAC state
                        if (consentPromptBehaviorAdmin == 2)
                        {
                            return 3; // 2: Dim Desktop, 1: No Dim Desktop
                        }
                        else if (consentPromptBehaviorAdmin == 5)
                        {
                            return promptOnSecureDesktop == 1 ? 2 : 1; // 2: Dim Desktop, 1: No Dim Desktop
                        }
                    }
                    else
                    {
                        return 0; // UAC is disabled (Never Notify)
                    }
                }
            }
            return -1; // Error or UAC setting not found
        }
        catch (Exception ex)
        {
            Console.WriteLine("An error occurred while checking UAC status: " + ex.Message);
            return -1; // Error
        }
    }

    public static void UpdateUACSlider(Slider uacSlider)
    {
        var uacState = GetUACState();

        // Update the UACSlider based on the UAC state
        uacSlider.Value = uacState switch
        {
            0 => 0,// Never Notify
            1 => 1,// Notify without Dim Desktop
            2 => 2,// Notify with Dim Desktop
            3 => 3,// Always Notify
            _ => (double)-1,// Error or Undefined State
        };
    }
}
