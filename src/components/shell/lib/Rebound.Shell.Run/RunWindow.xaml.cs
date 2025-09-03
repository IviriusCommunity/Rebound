// Copyright (C) Ivirius(TM) Community 2020 - 2025. All Rights Reserved.
// Licensed under the MIT License.

using CommunityToolkit.Mvvm.Input;
using Microsoft.UI.Input;
using Windows.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Rebound.Core.Helpers;
using Rebound.Core.Helpers.Windowing;
using Rebound.Helpers;
using Rebound.Shell.ExperienceHost;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml;
using Windows.Storage.Pickers;
using Windows.System;
using Windows.UI.Core;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;
using WinRT.Interop;

namespace Rebound.Shell.Run;

public sealed partial class RunWindow : Page
{
    public static List<string> GetRunHistory(List<string>? defaultValue = null)
    {
        try
        {
            var localAppDataPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "Rebound");
            var filePath = Path.Combine(localAppDataPath, "RunHistory", $"history.xml");

            if (!Directory.Exists(localAppDataPath))
                Directory.CreateDirectory(localAppDataPath);

            if (!Directory.Exists(Path.Combine(localAppDataPath, "RunHistory")))
                Directory.CreateDirectory(Path.Combine(localAppDataPath, "RunHistory"));

            if (!File.Exists(filePath))
                return defaultValue ?? new List<string>();

            var doc = new XmlDocument();
            doc.Load(filePath);

            // Example XPath: //Settings/MyHistoryKey/Entry
            var nodes = doc.SelectNodes($"//Settings/RunHistory/Entry");
            if (nodes == null || nodes.Count == 0)
                return defaultValue ?? new List<string>();

            var list = new List<string>();
            foreach (XmlNode node in nodes)
            {
                if (!string.IsNullOrWhiteSpace(node.InnerText))
                    list.Add(node.InnerText);
            }

            return list;
        }
        catch
        {
            return defaultValue ?? new List<string>();
        }
    }

    public static void SetRunHistory(List<string> newValues)
    {
        try
        {
            var localAppDataPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "Rebound");
            var filePath = Path.Combine(localAppDataPath, "RunHistory", $"history.xml");

            if (!Directory.Exists(localAppDataPath))
                Directory.CreateDirectory(localAppDataPath);

            if (!Directory.Exists(Path.Combine(localAppDataPath, "RunHistory")))
                Directory.CreateDirectory(Path.Combine(localAppDataPath, "RunHistory"));

            var doc = new XmlDocument();

            if (File.Exists(filePath))
            {
                doc.Load(filePath);
            }
            else
            {
                var declaration = doc.CreateXmlDeclaration("1.0", "utf-8", null);
                doc.AppendChild(declaration);

                var root = doc.CreateElement("Settings");
                doc.AppendChild(root);
            }

            var rootElement = doc.DocumentElement ?? doc.AppendChild(doc.CreateElement("Settings")) as XmlElement;

            // Remove existing history node if it exists
            var historyNode = rootElement.SelectSingleNode("RunHistory");
            if (historyNode != null)
                rootElement.RemoveChild(historyNode);

            // Recreate history node
            var newHistoryElement = doc.CreateElement("RunHistory");
            foreach (var value in newValues)
            {
                var entryElement = doc.CreateElement("Entry");
                entryElement.InnerText = value;
                newHistoryElement.AppendChild(entryElement);
            }

            rootElement.AppendChild(newHistoryElement);

            doc.Save(filePath);
        }
        catch
        {

        }
    }

    public RunViewModel ViewModel { get; } = new();

    public RunWindow()
    {
        InitializeComponent();
        var history = GetRunHistory();
        foreach(var item in history)
        {
            if (!string.IsNullOrWhiteSpace(item))
            {
                ViewModel.RunHistory.Add(item);
            }
        }
        // Load last history entry into Path (if any)
        if (ViewModel.RunHistory.Count > 0)
        {
            ViewModel.Path = ViewModel.RunHistory[^1]; // ^1 = last element
        }
        CheckRunButtonEnabledState();
    }

    public void CheckRunButtonEnabledState()
    {
        if (string.IsNullOrEmpty(ViewModel.Path) || string.IsNullOrWhiteSpace(ViewModel.Path))
        {
            ViewModel.IsRunButtonEnabled = false;
        }
        else
            ViewModel.IsRunButtonEnabled = true;
    }

    [RelayCommand]
    public void Cancel()
    {
        App.CloseRunWindow();
    }

    [RelayCommand]
    public async Task BrowseAsync()
    {
        var openPicker = new FileOpenPicker();

        // Initialize the file picker with the current window handle
        unsafe
        {
            // Get the HWND from your Window
            var hwnd = App.RunWindow?.Handle.ToCsWin32HWND();
            InitializeWithWindow.Initialize(openPicker, (nint)hwnd.Value);
        }

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
                ExitRunBox();
                return;
            }
            catch { return; }
        }

        if (ViewModel.Path.Trim().StartsWith("%"))
        {
            await Launcher.LaunchUriAsync(new Uri(Environment.ExpandEnvironmentVariables(ViewModel.Path.Trim())));
            ExitRunBox();
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
                ExitRunBox();
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
            ExitRunBox();
            return;
        }
        catch
        {
            return;
        }

        void ExitRunBox()
        {
            if (ViewModel.RunHistory.Contains(ViewModel.Path.Trim()))
            {
                ViewModel.RunHistory.Remove(ViewModel.Path.Trim());
            }
            ViewModel.RunHistory.Add(ViewModel.Path.Trim());
            SetRunHistory(ViewModel.RunHistory.ToList());
            Cancel();
        }
    }


    [RelayCommand]
    public void DeleteHistory()
    {
        ViewModel.RunHistory.Clear();
        SetRunHistory(ViewModel.RunHistory.ToList());
    }

    private void InputBox_TextChanged(AutoSuggestBox sender, AutoSuggestBoxTextChangedEventArgs args)
    {
        // Only respond to direct user input (not programmatic text changes)
        if (args.Reason == AutoSuggestionBoxTextChangeReason.UserInput)
        {
            if (string.IsNullOrWhiteSpace(sender.Text))
            {
                sender.ItemsSource = ViewModel.RunHistory;
            }
            else
            {
                var splitText = sender.Text.ToLower().Split(" ");
                var suitableItems = ViewModel.RunHistory
                    .Where(historyItem => splitText.All(key => historyItem.ToLower().Contains(key)))
                    .ToList();

                sender.ItemsSource = suitableItems;
            }
        }

        // Ensure button state is always updated at the end
        CheckRunButtonEnabledState();
    }

    private async void InputBox_KeyDown(object sender, KeyRoutedEventArgs e)
    {
        if (e.Key == VirtualKey.Enter)
            await DoRunLogicAsync();
    }

    private async void InputBox_QuerySubmitted(object sender, AutoSuggestBoxQuerySubmittedEventArgs e)
    {
        CheckRunButtonEnabledState();
        await DoRunLogicAsync();
    }

    private async void InputBox_SuggestionChosen(object sender, AutoSuggestBoxSuggestionChosenEventArgs e)
    {
        // Check button states manually since SuggestionChosen does not trigger TextChanged
        if (e.SelectedItem is string selectedItem && !string.IsNullOrEmpty(selectedItem) && !string.IsNullOrWhiteSpace(selectedItem))
        {
            ViewModel.IsRunButtonEnabled = true;
        }
        else
        {
            ViewModel.IsRunButtonEnabled = false;
        }

        if (!InputBox.IsSuggestionListOpen)
            await DoRunLogicAsync();
    }

    private async Task DoRunLogicAsync()
    {
        var downState = CoreVirtualKeyStates.Down;
        var isShiftPressed = (InputKeyboardSource.GetKeyStateForCurrentThread(VirtualKey.Menu) & downState) == downState;
        var isCtrlPressed = (InputKeyboardSource.GetKeyStateForCurrentThread(VirtualKey.Control) & downState) == downState;
        RunButton.Focus(FocusState.Pointer);
        await Task.Delay(50);
        await RunAsync(isShiftPressed && isCtrlPressed ? true : ViewModel.RunAsAdmin);
    }
}