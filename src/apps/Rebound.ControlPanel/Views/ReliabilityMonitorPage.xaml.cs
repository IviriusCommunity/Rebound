// Copyright (C) Ivirius(TM) Community 2020 - 2026. All Rights Reserved.
// Licensed under the MIT License.

using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.WinUI.Controls;
using LiveChartsCore.SkiaSharpView;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Text;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.Windows.Storage.Pickers;
using Rebound.ControlPanel.Models;
using Rebound.ControlPanel.ViewModels;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace Rebound.ControlPanel.Views;

internal sealed partial class ReliabilityMonitorPage : Page
{
    private sealed record EventDetails(
        string Provider,
        string EventId,
        string Level,
        string Channel,
        string RecordId,
        string TimeCreated,
        IReadOnlyList<(string Name, string Value)> Data);

    internal ReliabilityMonitorViewModel ViewModel { get; }

    public ReliabilityMonitorPage()
    {
        ViewModel = new ReliabilityMonitorViewModel(DispatcherQueue.GetForCurrentThread());
        InitializeComponent();
        Loaded += OnLoaded;
    }

    private void OnViewModeChanged(object sender, SelectionChangedEventArgs e)
    {
        if (sender is CommunityToolkit.WinUI.Controls.Segmented seg)
            ViewModel.ViewByWeeks = seg.SelectedIndex == 1;
    }

    private async void OnLoaded(object sender, RoutedEventArgs e)
    {
        // Wire the X axis so label updates (page changes) are reflected.
        // We do this here rather than in XAML because Axis.Labels is string[],
        // which x:Bind can handle OneWay but requires the property to fire
        // PropertyChanged - our [ObservableProperty] XAxisLabels does that.
        // The chart itself is named ReliabilityChart in the XAML.
        UpdateXAxisLabels();
        ViewModel.PropertyChanged += (_, args) =>
        {
            if (args.PropertyName == nameof(ViewModel.XAxisLabels))
                UpdateXAxisLabels();
        };

        await ViewModel.LoadDataCommand.ExecuteAsync(null).ConfigureAwait(false);
    }

    private void UpdateXAxisLabels()
    {
        // Reach into the chart's XAxes and update the Labels array.
        // XAxes is set in XAML with an XamlAxis; we update Labels directly.
        if (ReliabilityChart.XAxes is { } xAxes)
        {
            foreach (var axis in xAxes)
            {
                if (axis is Axis a)
                    a.Labels = ViewModel.XAxisLabels;
            }
        }
    }

    [RelayCommand]
    public async Task ViewEventDetailsAsync(ReliabilityEvent ev)
    {
        var details = ParseEventDetails(ev.RawXml);

        var stackPanel = new StackPanel()
        {
            Spacing = 16
        };

        var segmented = new Segmented
        {
        HorizontalAlignment = HorizontalAlignment.Stretch
        };

        var segmentedGeneralTab = new SegmentedItem()
        {
            Content = "General",
            IsSelected = true
        };

        var segmentedXmlTab = new SegmentedItem()
        {
            Content = "XML"
        };

        segmented.Items.Add(segmentedGeneralTab);
        segmented.Items.Add(segmentedXmlTab);

        var generalGrid = new ContentControl() { Content = BuildGeneralEventView(details) };
        var xmlGrid = new ScrollViewer()
        {
            Content = new TextBlock
            {
                Text = ev.RawXml,
                IsTextSelectionEnabled = true,
                TextWrapping = TextWrapping.Wrap,
                FontFamily = new FontFamily("Cascadia Mono"),
                FontSize = 12
            },
            Padding = new(0, 0, 8, 0),
            Height = 400,
            Visibility = Visibility.Collapsed
        };

        segmented.SelectionChanged += (s, e) =>
        {
            switch (segmented.SelectedIndex)
            {
                case 0:
                    xmlGrid.Visibility = Visibility.Collapsed;
                    generalGrid.Visibility = Visibility.Visible;
                    break;
                case 1:
                    xmlGrid.Visibility = Visibility.Visible;
                    generalGrid.Visibility = Visibility.Collapsed;
                    break;
            }
        };

        stackPanel.Children.Add(segmented);
        stackPanel.Children.Add(generalGrid);
        stackPanel.Children.Add(xmlGrid);

        var dialog = new ContentDialog
        {
            Title = ev.SourceName,
            Content = stackPanel,
            CloseButtonText = "Close",
            XamlRoot = XamlRoot,
        };

        await dialog.ShowAsync();
    }

    private static EventDetails ParseEventDetails(string xml)
    {
        var doc = XDocument.Parse(xml);
        XNamespace ns = "http://schemas.microsoft.com/win/2004/08/events/event";

        var system = doc.Root?.Element(ns + "System");
        var eventData = doc.Root?.Element(ns + "EventData");

        var provider = system?.Element(ns + "Provider")?.Attribute("Name")?.Value ?? "";
        var eventId = system?.Element(ns + "EventID")?.Value ?? "";
        var level = system?.Element(ns + "Level")?.Value ?? "";
        var channel = system?.Element(ns + "Channel")?.Value ?? "";
        var recordId = system?.Element(ns + "EventRecordID")?.Value ?? "";
        var timeCreated = system?.Element(ns + "TimeCreated")?.Attribute("SystemTime")?.Value ?? "";

        var data = eventData?
            .Elements(ns + "Data")
            .Select((e, i) => (
                Name: e.Attribute("Name")?.Value ?? $"Data {i + 1}",
                Value: e.Value))
            .ToList()
            ?? [];

        return new EventDetails(provider, eventId, level, channel, recordId, timeCreated, data);
    }

    private static FrameworkElement BuildGeneralEventView(EventDetails details)
    {
        var panel = new StackPanel { Spacing = 8 };

        AddField(panel, "Provider", details.Provider);
        AddField(panel, "Event ID", details.EventId);
        AddField(panel, "Level", details.Level);
        AddField(panel, "Channel", details.Channel);
        AddField(panel, "Record ID", details.RecordId);
        AddField(panel, "Time Created", details.TimeCreated);

        if (details.Data.Count > 0)
        {
            panel.Children.Add(new TextBlock
            {
                Text = "Event Data",
                FontWeight = FontWeights.SemiBold,
                Margin = new Thickness(0, 12, 0, 0),
            });

            foreach (var (name, value) in details.Data)
                AddField(panel, name, value);
        }

        return new ScrollViewer
        {
            Height = 400,
            Padding = new(0, 0, 8, 0),
            Content = panel,
        };
    }

    private static void AddField(StackPanel panel, string name, string value)
    {
        var grid = new Grid { ColumnSpacing = 12 };
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(150) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

        grid.Children.Add(new TextBlock
        {
            Text = name,
            Foreground = (Brush)Application.Current.Resources["TextFillColorSecondaryBrush"],
        });

        var valueText = new TextBlock
        {
            Text = string.IsNullOrWhiteSpace(value) ? "-" : value,
            TextWrapping = TextWrapping.Wrap,
            IsTextSelectionEnabled = true
        };

        Grid.SetColumn(valueText, 1);
        grid.Children.Add(valueText);

        panel.Children.Add(grid);
    }

    /// <summary>
    /// Exports reliability history to a CSV next to the user's Desktop.
    /// Matches the "Save reliability history" link in the real Reliability Monitor.
    /// </summary>
    [RelayCommand]
    public async Task ExportAsync()
    {
        try
        {
            var picker = new FileSavePicker(XamlRoot.ContentIslandEnvironment.AppWindowId)
            {
                CommitButtonText = "Export",
                SuggestedStartLocation = PickerLocationId.Desktop,
                DefaultFileExtension = ".csv",
                SuggestedFileName = $"ReliabilityHistory_{DateTime.Now:yyyy-MM-dd}"
            }; 
            picker.FileTypeChoices.Add("CSV Files", new List<string>() { ".csv" });

            // Show the picker dialog window
            var result = await picker.PickSaveFileAsync();

            if (result != null)
            {
                string savePath = result.Path;
                using var writer = new StreamWriter(savePath, append: false, System.Text.Encoding.UTF8);
                await writer.WriteLineAsync("Date,StabilityIndex,Kind,Source,Summary").ConfigureAwait(false);

                foreach (var day in ViewModel.AllDays)
                {
                    foreach (var ev in day.Events)
                    {
                        await writer.WriteLineAsync(
                            $"{day.Date:dd.MM.yyyy},{day.StabilityIndex},{ev.Kind},{CsvEscape(ev.SourceName)},{CsvEscape(ev.Summary)}").ConfigureAwait(false);
                    }
                }
            }
        }
        catch { /* Swallow export errors - UI can show a toast if desired */ }
    }

    private static string CsvEscape(string value)
        => value.Contains(',', StringComparison.InvariantCultureIgnoreCase) || value.Contains('"', StringComparison.InvariantCultureIgnoreCase)
            ? $"\"{value.Replace("\"", "\"\"", StringComparison.InvariantCultureIgnoreCase)}\""
            : value;

    private void OnViewEventDetailsClicked(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement { Tag: ReliabilityEvent ev })
            ViewEventDetailsCommand.Execute(ev);
    }
}