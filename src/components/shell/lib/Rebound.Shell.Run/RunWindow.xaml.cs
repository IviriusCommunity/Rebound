// Copyright (C) Ivirius(TM) Community 2020 - 2025. All Rights Reserved.
// Licensed under the MIT License.

using System;
using System.Diagnostics;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.Input;
using Microsoft.UI.Xaml;
using Rebound.Helpers;
using Windows.Storage.Pickers;
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
        this.Move((int)(25 * scale), (int)(Display.GetDPIAwareDisplayRect(this).Height - (48 + 25) * scale - Height * scale));
        ExtendsContentIntoTitleBar = true;
    }

    private void WindowEx_Closed(object sender, WindowEventArgs args)
    {
        onClosedCallback?.Invoke();
    }

    private void WindowEx_Activated(object sender, WindowActivatedEventArgs args)
    {
        this.SetTaskBarIcon(Icon.FromFile($"{AppContext.BaseDirectory}\\Assets\\RunBox.ico"));
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
    public void Run()
    {
        if (string.IsNullOrWhiteSpace(ViewModel.Path))
            return;

        var input = ViewModel.Path.Trim();
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
                if (ViewModel.RunAsAdmin && exePath.EndsWith(".exe", StringComparison.OrdinalIgnoreCase))
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
                Verb = ViewModel.RunAsAdmin ? "runas" : null
            };

            Process.Start(psi);
        }
        catch
        {

        }
    }

    private void TextBox_KeyDown(object sender, Microsoft.UI.Xaml.Input.KeyRoutedEventArgs e)
    {
        if (e.Key == Windows.System.VirtualKey.Enter)
        {
            Run();
        }
    }
}