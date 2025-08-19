// Copyright (C) Ivirius(TM) Community 2020 - 2025. All Rights Reserved.
// Licensed under the MIT License.

using CommunityToolkit.Mvvm.Input;
using Microsoft.UI.Input;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Rebound.Helpers;
using Rebound.Helpers.Windowing;
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
using WinUIEx;

namespace Rebound.Shell.Run;

public sealed partial class RunWindow : WindowEx
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
            await Task.Delay(100);
            InputBox.IsSuggestionListOpen = false;
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
            Close();
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
        // Since selecting an item will also change the text,
        // only listen to changes caused by user entering text.
        if (args.Reason == AutoSuggestionBoxTextChangeReason.UserInput)
        {
            if (string.IsNullOrEmpty(sender.Text) || string.IsNullOrWhiteSpace(sender.Text))
            {
                sender.ItemsSource = ViewModel.RunHistory;
                return;
            }
            var suitableItems = new List<string>();
            var splitText = sender.Text.ToLower().Split(" ");
            foreach (var cat in ViewModel.RunHistory)
            {
                var found = splitText.All((key) =>
                {
                    return cat.ToLower().Contains(key);
                });
                if (found)
                {
                    suitableItems.Add(cat);
                }
            }
            sender.ItemsSource = suitableItems;
        }
    }

    private async void InputBox_KeyDown(object sender, KeyRoutedEventArgs e)
    {
        if (e.Key == VirtualKey.Enter)
            await DoRunLogicAsync();
    }

    private async void InputBox_QuerySubmitted(object sender, AutoSuggestBoxQuerySubmittedEventArgs e)
    {
        await DoRunLogicAsync();
    }

    private async void InputBox_SuggestionChosen(object sender, AutoSuggestBoxSuggestionChosenEventArgs e)
    {
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