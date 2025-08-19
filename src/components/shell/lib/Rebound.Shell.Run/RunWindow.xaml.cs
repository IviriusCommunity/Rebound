// Copyright (C) Ivirius(TM) Community 2020 - 2025. All Rights Reserved.
// Licensed under the MIT License.

using System;
using System.Diagnostics;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.Input;
using Microsoft.UI.Input;
using Microsoft.UI.Xaml;
using Rebound.Helpers;
using Rebound.Helpers.Windowing;
using Windows.Storage.Pickers;
using Windows.System;
using Windows.UI.Core;
using WinUIEx;

namespace Rebound.Shell.Run;

public sealed partial class RunWindow : WindowEx
{
    private readonly Action? onClosedCallback;

    public RunViewModel ViewModel { get; } = new();

    public RunWindow(Action? onClosed = null)
    {
        onClosedCallback = onClosed;
        InitializeComponent();
        var scale = Display.GetScale(this);
        this.Move((int)(25 * scale), (int)(Display.GetDisplayRect(this).Height - (48 + 25) * scale - Height * scale));
        this.TurnOffDoubleClick();
        ExtendsContentIntoTitleBar = true;
    }

    private void WindowEx_Closed(object sender, WindowEventArgs args)
    {
        onClosedCallback?.Invoke();
    }

    private async void WindowEx_Activated(object sender, Microsoft.UI.Xaml.WindowActivatedEventArgs args)
    {
        if (args.WindowActivationState != WindowActivationState.Deactivated)
        {
            this.SetWindowIcon($"{AppContext.BaseDirectory}\\Assets\\RunBox.ico");
            await Task.Delay(100);
            InputBox.Focus(FocusState.Programmatic);
        }
    }

    [RelayCommand]
    public void Cancel()
    {
        Close();
    }

    [RelayCommand]
    public async Task BrowseAsync()
    {
        var openPicker = new FileOpenPicker();

        // Initialize the file picker with the current window handle
        WinRT.Interop.InitializeWithWindow.Initialize(openPicker, this.GetWindowHandle());

        // Let the user choose starting location
        openPicker.SuggestedStartLocation = PickerLocationId.ComputerFolder;
        openPicker.ViewMode = PickerViewMode.List;

        // Allow common executable types
        openPicker.FileTypeFilter.Add(".exe");
        openPicker.FileTypeFilter.Add(".bat");
        openPicker.FileTypeFilter.Add(".cmd");
        openPicker.FileTypeFilter.Add(".com");
        openPicker.FileTypeFilter.Add(".msc");
        openPicker.FileTypeFilter.Add("*");

        // Open the picker
        var file = await openPicker.PickSingleFileAsync();
        if (file != null)
        {
            ViewModel.Path = $"\"{file.Path}\""; // Wrap in quotes in case path has spaces
        }
    }

    [GeneratedRegex("^\"([^\"]+)\"(.*)")]
    private static partial Regex QuotedPathRegex();

    [RelayCommand]
    public async Task RunAsync(bool runAsAdmin)
    {
        if (string.IsNullOrWhiteSpace(ViewModel.Path))
            return;

        var input = Environment.ExpandEnvironmentVariables(ViewModel.Path.Trim());

        string exePath;
        string args;

        var quotedMatch = QuotedPathRegex().Match(input);
        if (quotedMatch.Success)
        {
            exePath = quotedMatch.Groups[1].Value.Trim();
            args = quotedMatch.Groups[2].Value.Trim();
        }
        else
        {
            int spaceIndex = input.IndexOf(' ');
            if (spaceIndex == -1)
            {
                exePath = input;
                args = string.Empty;
            }
            else
            {
                exePath = input.Substring(0, spaceIndex);
                args = input.Substring(spaceIndex + 1);
            }
        }

        if (Uri.IsWellFormedUriString(exePath, UriKind.Absolute))
        {
            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = exePath,
                    UseShellExecute = true
                });
                return;
            }
            catch { return; }
        }

        if (ViewModel.Path.StartsWith("%"))
        {
            await Launcher.LaunchUriAsync(new Uri(Environment.ExpandEnvironmentVariables(ViewModel.Path.Trim())));
            Close();
            return;
        }

        if (File.Exists(exePath))
        {
            try
            {
                var psi = new ProcessStartInfo
                {
                    FileName = exePath,
                    Arguments = args,
                    UseShellExecute = true
                };

                // Only allow RunAs on executables
                if (runAsAdmin && exePath.EndsWith(".exe", StringComparison.OrdinalIgnoreCase))
                    psi.Verb = "runas";

                Process.Start(psi);
                return;
            }
            catch { return; }
        }

        try
        {
            var psi = new ProcessStartInfo
            {
                FileName = exePath,
                Arguments = args,
                UseShellExecute = true,
                Verb = runAsAdmin ? "runas" : null
            };

            Process.Start(psi);
        }
        catch
        {
            return;
        }

        Close();
    }

    private async void TextBox_KeyDown(object sender, Microsoft.UI.Xaml.Input.KeyRoutedEventArgs e)
    {
        if (e.Key == VirtualKey.Enter)
        {
            var downState = CoreVirtualKeyStates.Down;
            var isShiftPressed = (InputKeyboardSource.GetKeyStateForCurrentThread(VirtualKey.Menu) & downState) == downState;
            var isCtrlPressed = (InputKeyboardSource.GetKeyStateForCurrentThread(VirtualKey.Control) & downState) == downState;
            e.Handled = true;
            RunButton.Focus(FocusState.Pointer);
            await Task.Delay(50);
            await RunAsync(isShiftPressed && isCtrlPressed ? true : ViewModel.RunAsAdmin);
        }
    }
}