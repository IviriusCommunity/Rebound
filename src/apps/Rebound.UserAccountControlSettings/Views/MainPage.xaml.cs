// Copyright (C) Ivirius(TM) Community 2020 - 2025. All Rights Reserved.
// Licensed under the MIT License.

using CommunityToolkit.Mvvm.Input;
using Rebound.UserAccountControlSettings.ViewModels;
using Windows.UI.Xaml.Controls;

namespace Rebound.UserAccountControlSettings.Views;

internal sealed partial class MainPage : Page
{
    internal MainViewModel ViewModel { get; } = new();

    public MainPage()
    {
        InitializeComponent();
    }

    [RelayCommand]
    public static void Close() => App.MainWindow?.Close();

    [RelayCommand]
    public void Apply()
    {
        var command = string.Empty;
        switch (ViewModel.SliderValue)
        {
            case 1:
                command = @"
                    Set-ItemProperty -Path 'HKLM:\SOFTWARE\Microsoft\Windows\CurrentVersion\Policies\System' -Name 'EnableLUA' -Value 1;
                    Set-ItemProperty -Path 'HKLM:\SOFTWARE\Microsoft\Windows\CurrentVersion\Policies\System' -Name 'ConsentPromptBehaviorAdmin' -Value 0;
                    Set-ItemProperty -Path 'HKLM:\SOFTWARE\Microsoft\Windows\CurrentVersion\Policies\System' -Name 'PromptOnSecureDesktop' -Value 0;
                ";
                break;
            case 2:
                command = @"
                    Set-ItemProperty -Path 'HKLM:\SOFTWARE\Microsoft\Windows\CurrentVersion\Policies\System' -Name 'EnableLUA' -Value 1;
                    Set-ItemProperty -Path 'HKLM:\SOFTWARE\Microsoft\Windows\CurrentVersion\Policies\System' -Name 'ConsentPromptBehaviorAdmin' -Value 5;
                    Set-ItemProperty -Path 'HKLM:\SOFTWARE\Microsoft\Windows\CurrentVersion\Policies\System' -Name 'PromptOnSecureDesktop' -Value 0;
                ";
                break;
            case 3:
                command = @"
                    Set-ItemProperty -Path 'HKLM:\SOFTWARE\Microsoft\Windows\CurrentVersion\Policies\System' -Name 'EnableLUA' -Value 1;
                    Set-ItemProperty -Path 'HKLM:\SOFTWARE\Microsoft\Windows\CurrentVersion\Policies\System' -Name 'ConsentPromptBehaviorAdmin' -Value 5;
                    Set-ItemProperty -Path 'HKLM:\SOFTWARE\Microsoft\Windows\CurrentVersion\Policies\System' -Name 'PromptOnSecureDesktop' -Value 1;
                ";
                break;
            case 4:
                command = @"
                    Set-ItemProperty -Path 'HKLM:\SOFTWARE\Microsoft\Windows\CurrentVersion\Policies\System' -Name 'EnableLUA' -Value 1;
                    Set-ItemProperty -Path 'HKLM:\SOFTWARE\Microsoft\Windows\CurrentVersion\Policies\System' -Name 'ConsentPromptBehaviorAdmin' -Value 2;
                    Set-ItemProperty -Path 'HKLM:\SOFTWARE\Microsoft\Windows\CurrentVersion\Policies\System' -Name 'PromptOnSecureDesktop' -Value 1;
                ";
                break;
        }
        try
        {
            MainViewModel.RunPowerShellCommand(command);
            App.MainWindow?.Close();
        }
        catch
        {

        }
    }
}