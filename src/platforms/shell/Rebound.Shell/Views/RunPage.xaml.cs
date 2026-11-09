// Copyright (C) Ivirius(TM) Community 2020 - 2026. All Rights Reserved.
// Licensed under the MIT License.

using CommunityToolkit.Mvvm.Input;
using Microsoft.UI.Input;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.Windows.Storage.Pickers;
using Rebound.Core;
using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml;
using Windows.System;
using Windows.UI.Core;
using Windows.Win32.Foundation;

#pragma warning disable CA1031 // Do not catch general exception types

namespace Rebound.Shell.Run;

internal sealed partial class RunWindow : Page
{
    public RunViewModel ViewModel { get; } = new();

    public RunWindow()
    {
        InitializeComponent();
        Loaded += (s, e) =>
        {
            var history = GetRunHistory();
            foreach (var item in history)
                if (!string.IsNullOrWhiteSpace(item))
                    ViewModel.RunHistory.Add(item);

            // Load last history entry into Path (if any)
            if (ViewModel.RunHistory.Count > 0)
                ViewModel.Path = ViewModel.RunHistory[^1];
            InputBox.IsSuggestionListOpen = false;
        };
    }

    [RelayCommand]
    public static void Cancel()
        => App.CloseRunWindow();

    [RelayCommand]
    public async Task BrowseAsync()
    {
        var picker = new FileOpenPicker(XamlRoot.ContentIslandEnvironment.AppWindowId)
        {
            CommitButtonText = "Select",
            SuggestedStartLocation = PickerLocationId.ComputerFolder,
            ViewMode = PickerViewMode.List
        };

        picker.FileTypeFilter.Add(".exe");
        picker.FileTypeFilter.Add(".bat");
        picker.FileTypeFilter.Add(".cmd");
        picker.FileTypeFilter.Add(".com");
        picker.FileTypeFilter.Add(".msc");
        picker.FileTypeFilter.Add("*");

        var file = await picker.PickSingleFileAsync();
        if (file != null)
            ViewModel.Path = $"\"{file.Path}\""; // Wrap in quotes in case path has spaces
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
            int spaceIndex = input.IndexOf(' ', StringComparison.InvariantCultureIgnoreCase);
            if (spaceIndex == -1)
            {
                exePath = input;
                args = string.Empty;
            }
            else
            {
                exePath = input[..spaceIndex];
                args = input[(spaceIndex + 1)..];
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

        if (ViewModel.Path.Trim().StartsWith('%'))
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
            SetRunHistory(ViewModel.RunHistory);
            Cancel();
        }
    }


    [RelayCommand]
    public void DeleteHistory()
    {
        ViewModel.RunHistory.Clear();
        SetRunHistory(ViewModel.RunHistory);
    }

    [LibraryImport("shell32.dll", EntryPoint = "#61")]
    [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
    public static partial void RunFileDlg(
        void* hwnd,
        void* hIcon,
        char* lpstrDirectory,
        char* lpstrTitle,
        char* lpstrDescription,
        uint uFlags);

    [RelayCommand]
    public unsafe void LaunchLegacy()
    {
        fixed (char* title = "Legacy Run")
        {
            RunFileDlg(
                HWND.Null,
                default,
                null,
                title,
                null,
                0);
        }
    }

    private void InputBox_TextChanged(AutoSuggestBox sender, AutoSuggestBoxTextChangedEventArgs args)
    {
        if (args.Reason == AutoSuggestionBoxTextChangeReason.UserInput)
        {
            var query = sender.Text.Trim();

            sender.ItemsSource = string.IsNullOrEmpty(query)
                ? ViewModel.RunHistory
                : ViewModel.RunHistory
                    .Where(item => item.Contains(
                        query,
                        StringComparison.CurrentCultureIgnoreCase))
                    .ToList();
        }
    }

    private static void InputBox_SuggestionChosen(AutoSuggestBox sender, AutoSuggestBoxSuggestionChosenEventArgs args)
    {
        if (args.SelectedItem is string item)
            sender.Text = item;
    }

    private async void InputBox_QuerySubmitted(AutoSuggestBox sender, AutoSuggestBoxQuerySubmittedEventArgs args)
        => await DoRunLogicAsync().ConfigureAwait(false);

    private async void InputBox_SuggestionChosen(object sender, AutoSuggestBoxSuggestionChosenEventArgs e)
    {
        if (!InputBox.IsSuggestionListOpen)
            await DoRunLogicAsync().ConfigureAwait(false);
    }

    private async Task DoRunLogicAsync()
    {
        var down = CoreVirtualKeyStates.Down;
        var altPressed = (InputKeyboardSource.GetKeyStateForCurrentThread(VirtualKey.Menu) & down) == down;
        var ctrlPressed = (InputKeyboardSource.GetKeyStateForCurrentThread(VirtualKey.Control) & down) == down;

        RunButton.Focus(FocusState.Programmatic);

        await RunAsync(
            ViewModel.RunAsAdmin 
            || (altPressed && ctrlPressed))
                .ConfigureAwait(false);
    }

    public static Collection<string> GetRunHistory(Collection<string>? defaultValue = null)
    {
        try
        {
            var filePath = Path.Combine(Variables.ReboundDataFolder, "RunHistory", $"history.xml");

            if (!Directory.Exists(Variables.ReboundDataFolder))
                Directory.CreateDirectory(Variables.ReboundDataFolder);

            if (!Directory.Exists(Path.Combine(Variables.ReboundDataFolder, "RunHistory")))
                Directory.CreateDirectory(Path.Combine(Variables.ReboundDataFolder, "RunHistory"));

            if (!File.Exists(filePath))
                return defaultValue ?? [];

            var doc = new XmlDocument();
            doc.Load(filePath);

            // Example XPath: //Settings/MyHistoryKey/Entry
            var nodes = doc.SelectNodes($"//Settings/RunHistory/Entry");
            if (nodes == null || nodes.Count == 0)
                return defaultValue ?? [];

            var list = new Collection<string>();
            foreach (XmlNode node in nodes)
            {
                if (!string.IsNullOrWhiteSpace(node.InnerText))
                    list.Add(node.InnerText);
            }

            return list;
        }
        catch
        {
            return defaultValue ?? [];
        }
    }

    public static void SetRunHistory(Collection<string> newValues)
    {
        try
        {
            var filePath = Path.Combine(Variables.ReboundDataFolder, "RunHistory", $"history.xml");

            if (!Directory.Exists(Variables.ReboundDataFolder))
                Directory.CreateDirectory(Variables.ReboundDataFolder);

            if (!Directory.Exists(Path.Combine(Variables.ReboundDataFolder, "RunHistory")))
                Directory.CreateDirectory(Path.Combine(Variables.ReboundDataFolder, "RunHistory"));

            var doc = new XmlDocument();

            if (File.Exists(filePath))
                doc.Load(filePath);
            else
            {
                var declaration = doc.CreateXmlDeclaration("1.0", "utf-8", null);
                doc.AppendChild(declaration);

                var root = doc.CreateElement("Settings");
                doc.AppendChild(root);
            }

            var rootElement = doc.DocumentElement ?? doc.AppendChild(doc.CreateElement("Settings")) as XmlElement;

            // Remove existing history node if it exists
            var historyNode = rootElement?.SelectSingleNode("RunHistory");
            if (historyNode != null)
                rootElement?.RemoveChild(historyNode);

            // Recreate history node
            var newHistoryElement = doc.CreateElement("RunHistory");
            foreach (var value in newValues ?? [])
            {
                var entryElement = doc.CreateElement("Entry");
                entryElement.InnerText = value;
                newHistoryElement.AppendChild(entryElement);
            }

            rootElement?.AppendChild(newHistoryElement);

            doc.Save(filePath);
        }
        catch { }
    }
}