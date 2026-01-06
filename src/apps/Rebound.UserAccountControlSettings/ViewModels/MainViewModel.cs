// Copyright (C) Ivirius(TM) Community 2020 - 2026. All Rights Reserved.
// Licensed under the MIT License.

using System;
using System.Diagnostics;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Win32;

namespace Rebound.UserAccountControlSettings.ViewModels;

internal partial class MainViewModel : ObservableObject
{
    [ObservableProperty]
    public partial double SliderValue { get; set; }

    [ObservableProperty]
    public partial string Title { get; set; }

    [ObservableProperty]
    public partial string Description { get; set; }

    [ObservableProperty]
    public partial string Recommandation { get; set; }

    private const string RegistryKeyPath = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Policies\System";

    public static void RunPowerShellCommand(string command)
    {
        using var process = new Process
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
            return 0; // Error or UAC setting not found
        }
        catch (Exception ex)
        {
            return 0; // Error
        }
    }

    internal MainViewModel()
    {
        SliderValue = GetUACState() + 1;
    }

    partial void OnSliderValueChanged(double oldValue, double newValue)
    {
        switch (newValue)
        {
            case 1:
                {
                    Title = "Never notify me when:";
                    Description = "-   Apps try to install software or make changes to my computer\n-   I make changes to Windows settings";
                    Recommandation = "Not recommended.";
                    break;
                }
            case 2:
                {
                    Title = "Notify me only when apps try to make changes to my computer (do not dim my desktop)";
                    Description = "-   Don't notify me when I make changes to Windows settings";
                    Recommandation = "Not recommended. Choose this only if it takes a long time to dim the desktop on your computer.";
                    break;
                }
            case 3:
                {
                    Title = "Notify me only when apps try to make changes to my computer (default)";
                    Description = "-   Don't notify me when I make changes to Windows settings";
                    Recommandation = "Recommended if you use familiar apps and visit familiar websites.";
                    break;
                }
            case 4:
                {
                    Title = "Always notify me when:";
                    Description = "-   Apps try to install software or make changes to my computer\n-   I make changes to Windows settings";
                    Recommandation = "Recommended if you routinely install new software and visit unfamiliar websites.";
                    break;
                }
        }
    }
}