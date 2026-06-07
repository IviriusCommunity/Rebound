// Copyright (C) Ivirius(TM) Community 2020 - 2026. All Rights Reserved.
// Licensed under the MIT License.

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LiveChartsCore;
using LiveChartsCore.Kernel;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Painting;
using Microsoft.UI.Dispatching;
using Rebound.ControlPanel.Models;
using Rebound.ControlPanel.Services;
using SkiaSharp;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Rebound.ControlPanel.ViewModels;

/// <summary>
/// ViewModel for <c>ReliabilityMonitorPage</c>.
///
/// Owns the full 28-day raw dataset and slices it into the current page window
/// for the chart and detail lists. The page codebehind binds everything via
/// <c>x:Bind ViewModel.*</c>.
///
/// Chart convention: LiveCharts Y axis is inverted (0 = top = worst).
/// ChartValues contains <c>10.0 - stabilityIndex</c> so the gradient
/// (red at top, green at bottom) matches the stability direction.
/// </summary>
internal sealed partial class ReliabilityMonitorViewModel : ObservableObject
{
    /// <summary>
    /// True while the initial data load is in progress.
    /// </summary>
    [ObservableProperty] public partial bool IsLoading { get; set; } = true;

    /// <summary>
    /// True when "Weeks" segment is selected.
    /// </summary>
    [ObservableProperty] public partial bool ViewByWeeks { get; set; }

    /// <summary>
    /// Whether the ← scroll-back button is enabled.
    /// </summary>
    [ObservableProperty] public partial bool CanScrollBack { get; set; }

    /// <summary>
    /// Whether the → scroll-forward button is enabled.
    /// </summary>
    [ObservableProperty] public partial bool CanScrollForward { get; set; }

    /// <summary>
    /// How many page-widths back from "today" the current view is.
    /// 0 = most recent page (today visible), 1 = one page back, etc.
    /// </summary>
    private int _pageOffset;

    /// <summary>
    /// Full 28-day dataset, ascending (index 0 = oldest).
    /// </summary>
    public List<ReliabilityDay> AllDays { get; private set; } = [];

    /// <summary>
    /// The dispatcher of the UI thread, used to marshal icon updates.
    /// </summary>
    private readonly DispatcherQueue _dispatcher;

    /// <summary>
    /// The line series instance. Built once in the constructor so we can attach
    /// <c>ChartPointPointerDown</c> - the event must be wired on the series object,
    /// not declaratively in XAML, because the handler needs a strongly typed point.
    /// Exposed as <c>ISeries[]</c> so the chart's <c>Series</c> property can bind to it.
    /// </summary>
    public ISeries[] Series { get; }

    /// <summary>
    /// A single semi-transparent section that highlights the selected column.
    /// Xi/Xj are updated by <see cref="SelectChartPointCommand"/>.
    /// </summary>
    public List<IChartElement> Sections { get; }

    /// <summary>
    /// Values fed to the LineSeries. Already flipped: value = <c>10.0 - stabilityIndex</c>.
    /// LineSeries binds to this collection directly; updates are reflected live.
    /// </summary>
    public ObservableCollection<double> ChartValues { get; } = [];

    /// <summary>
    /// X-axis date labels for the current page window.
    /// Must be a <c>string[]</c> - <c>Axis.Labels</c> does not accept IEnumerable.
    /// Replaced (not mutated) on every page change so x:Bind OneWay picks it up.
    /// </summary>
    public ObservableCollection<string> XAxisLabels { get; set; } = [];

    /// <summary>
    /// Y-axis labels - fixed, never changes.
    /// </summary>
    public string[] YAxisLabels { get; } = ["10", "9", "8", "7", "6", "5", "4", "3", "2", "1"];

    public ObservableCollection<ReliabilityEvent> Errors { get; } = [];

    public ObservableCollection<ReliabilityEvent> Warnings { get; } = [];

    public ObservableCollection<ReliabilityEvent> Informational { get; } = [];

    [ObservableProperty] public partial bool IsErrorsEmpty { get; set; }

    [ObservableProperty] public partial bool IsWarningsEmpty { get; set; }

    [ObservableProperty] public partial bool IsInformationalEmpty { get; set; }

    /// <summary>
    /// Number of columns shown on one chart page in Days mode.
    /// </summary>
    private const int DayPageSize = 20;

    /// <summary>
    /// Number of columns shown on one chart page in Weeks mode.
    /// </summary>
    private const int WeekPageSize = 8;

    private int PageSize => ViewByWeeks ? WeekPageSize : DayPageSize;

    public ReliabilityMonitorViewModel(DispatcherQueue dispatcher)
    {
        Errors.CollectionChanged += CollectionChanged;
        Warnings.CollectionChanged += CollectionChanged;
        Informational.CollectionChanged += CollectionChanged;

        _dispatcher = dispatcher;

        // Build the gradient paints - same colours as the XAML prototype.
        var fillGradient = new LinearGradientPaint([ 
                new SKColor(0xFF, 0x0C, 0x10, 0x60), 
                new SKColor(0xFF, 0x73, 0x04, 0x42),
                new SKColor(0xFF, 0xE9, 0x2C, 0x35), 
                new SKColor(0x2F, 0xFF, 0x24, 0x1B) ],
            new SKPoint(0f, 0f), new SKPoint(0f, 1f));

        var strokeGradient = new LinearGradientPaint([ 
                new SKColor(0xFF, 0x0C, 0x10), 
                new SKColor(0xFF, 0x73, 0x04),
                new SKColor(0xFF, 0xE9, 0x2C), 
                new SKColor(0x2F, 0xFF, 0x24) ],
            new SKPoint(0f, 0f), new SKPoint(0f, 1f))
        { StrokeThickness = 3 };

        var lineSeries = new LineSeries<double>
        {
            Values = ChartValues,
            Fill = fillGradient,
            Stroke = strokeGradient,
            GeometryFill = null,
            GeometryStroke = null,
        };

        // Attach point-click handler - point.Context.Index is the 0-based index
        // within ChartValues, which maps 1:1 to the current page slice.
        lineSeries.ChartPointPointerDown += (_, point) =>
        {
            if (point is null) return;
            SelectChartPoint(point.Index);
        };

        Series = [lineSeries];

        // Selection highlight section - hidden until a point is clicked.
        var highlightSection = new RectangularSection
        {
            Fill = new SolidColorPaint(new SKColor(255, 255, 255, 30)),
            Xi = -1,
            Xj = -1,
        };

        Sections = [highlightSection];
    }

    private void CollectionChanged(object? sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
    {
        IsErrorsEmpty = Errors.Count <= 0;
        IsWarningsEmpty = Warnings.Count <= 0;
        IsInformationalEmpty = Informational.Count <= 0;
    }

    [RelayCommand]
    public static void ViewAllEvents()
    {
        System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
        {
            FileName = "eventvwr.exe",
            UseShellExecute = true,
        });
    }

    private async Task LoadIconsAsync()
    {
        foreach (var ev in AllDays.SelectMany(d => d.Events))
        {
            _dispatcher.TryEnqueue(async () =>
            {
                ev.Icon = await ReliabilityService.LoadIconAsync(ev.ExecutablePath).ConfigureAwait(true);
            });
        }
    }

    /// <summary>
    /// Called from the Page's <c>Loaded</c> event. Loads data and refreshes the view.
    /// </summary>
    [RelayCommand]
    public async Task LoadDataAsync()
    {
        IsLoading = true;

        try
        {
            AllDays = await ReliabilityService.LoadAsync(daysBack: 28).ConfigureAwait(true);
            _pageOffset = 0;
            RefreshView();
            _ = LoadIconsAsync(); // fire-and-forget; icons populate lazily
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand(CanExecute = nameof(CanScrollBack))]
    public void ScrollBack()
    {
        _pageOffset++;
        RefreshView();
    }

    [RelayCommand(CanExecute = nameof(CanScrollForward))]
    public void ScrollForward()
    {
        if (_pageOffset > 0) _pageOffset--;
        RefreshView();
    }

    partial void OnViewByWeeksChanged(bool value)
    {
        _pageOffset = 0;
        RefreshView();
    }

    /// <summary>
    /// Recomputes <see cref="ChartValues"/>, <see cref="XAxisLabels"/>, and the
    /// three event buckets for the currently selected page/mode.
    /// </summary>
    private void RefreshView()
    {
        if (ViewByWeeks)
            RefreshWeekView();
        else
            RefreshDayView();

        // Update pagination button state
        CanScrollBack = (_pageOffset + 1) * PageSize < (ViewByWeeks ? WeekCount() : AllDays.Count);
        CanScrollForward = _pageOffset > 0;

        ScrollBackCommand.NotifyCanExecuteChanged();
        ScrollForwardCommand.NotifyCanExecuteChanged();
    }

    private void RefreshDayView()
    {
        // Slice the days for the current page, newest-first paging
        int totalDays = AllDays.Count;
        int endIndex = totalDays - _pageOffset * DayPageSize; // exclusive
        int startIndex = Math.Max(0, endIndex - DayPageSize); // inclusive

        var slice = AllDays[startIndex..endIndex];

        // Chart values
        ChartValues.Clear();
        var labels = new string[slice.Count];

        for (var i = 0; i < slice.Count; i++)
        {
            ChartValues.Add(10.0 - slice[i].StabilityIndex);
            labels[i] = slice[i].Date.ToString("d", CultureInfo.CurrentCulture);
        }

        XAxisLabels.Clear();
        foreach (var label in labels)
            XAxisLabels.Add(label);

        if (slice.Count > 0)
            SelectPageIndex(slice.Count - 1, slice[^1].Events);
        else
            ClearSelectionHighlight();

        // Details - show events for the most recent day in the slice that has any
        var selectedDay = slice.LastOrDefault(d => d.Events.Count > 0) ?? slice.LastOrDefault();
        PopulateEventLists(selectedDay?.Events ?? []);
    }

    private void RefreshWeekView()
    {
        var weeks = BuildWeeks();
        int totalWeeks = weeks.Count;
        int endIndex = totalWeeks - _pageOffset * WeekPageSize;
        int startIndex = Math.Max(0, endIndex - WeekPageSize);

        var slice = weeks[startIndex..endIndex];

        ChartValues.Clear();
        var labels = new string[slice.Count];

        for (var i = 0; i < slice.Count; i++)
        {
            ChartValues.Add(10.0 - slice[i].StabilityIndex);
            labels[i] = slice[i].WeekStart.ToString("d", CultureInfo.CurrentCulture);
        }

        XAxisLabels.Clear();
        foreach (var label in labels)
            XAxisLabels.Add(label);

        if (slice.Count > 0)
            SelectPageIndex(slice.Count - 1, slice[^1].Events);
        else
            ClearSelectionHighlight();

        var selectedWeek = slice.LastOrDefault(w => w.Events.Count > 0) ?? slice.LastOrDefault();
        PopulateEventLists(selectedWeek?.Events ?? []);
    }

    private void SelectPageIndex(int pageIndex, IReadOnlyList<ReliabilityEvent> events)
    {
        var section = Sections[0];

        if (section is RectangularSection rectangularSection)
        {
            rectangularSection.Xi = pageIndex - 0.5;
            rectangularSection.Xj = pageIndex + 0.5;
        }

        PopulateEventLists(events);
    }

    private void PopulateEventLists(IEnumerable<ReliabilityEvent> events)
    {
        Errors.Clear();
        Warnings.Clear();
        Informational.Clear();

        foreach (var ev in events)
        {
            switch (ev.Kind)
            {
                case ReliabilityEventKind.Error: Errors.Add(ev); break;
                case ReliabilityEventKind.Warning: Warnings.Add(ev); break;
                case ReliabilityEventKind.Informational: Informational.Add(ev); break;
            }
        }
    }

    private List<ReliabilityWeek> BuildWeeks()
    {
        if (AllDays.Count == 0) return [];

        var weeks = new List<ReliabilityWeek>();
        var firstDay = AllDays[0].Date;

        // Align to Monday of the first week
        int dow = (int)firstDay.DayOfWeek;
        var weekStart = firstDay.AddDays(-(dow == 0 ? 6 : dow - 1));

        while (weekStart <= AllDays[^1].Date)
        {
            var weekEnd = weekStart.AddDays(6);

            var daysInWeek = AllDays
                .Where(d => d.Date >= weekStart && d.Date <= weekEnd)
                .ToList();

            if (daysInWeek.Count > 0)
            {
                var allEvents = daysInWeek.SelectMany(d => d.Events).ToList();
                double avgIndex = daysInWeek.Average(d => d.StabilityIndex);

                weeks.Add(new ReliabilityWeek
                {
                    WeekStart = weekStart,
                    WeekEnd = weekEnd,
                    StabilityIndex = Math.Round(avgIndex, 1),
                    Events = allEvents,
                });
            }

            weekStart = weekStart.AddDays(7);
        }

        return weeks;
    }

    private int WeekCount() => BuildWeeks().Count;

    /// <summary>
    /// Called when the user clicks a chart point.
    /// Updates the detail lists and moves the selection highlight section.
    /// </summary>
    private void SelectChartPoint(int pageIndex)
    {
        // Move the highlight section to sit around the clicked column
        var section = Sections[0];
        if (section is RectangularSection rectangularSection)
        {
            rectangularSection.Xi = pageIndex - 0.5;
            rectangularSection.Xj = pageIndex + 0.5;

            if (ViewByWeeks)
            {
                var weeks = BuildWeeks();
                int totalWeeks = weeks.Count;
                int endIndex = totalWeeks - _pageOffset * WeekPageSize;
                int startIndex = Math.Max(0, endIndex - WeekPageSize);
                var slice = weeks[startIndex..endIndex];

                if (pageIndex >= 0 && pageIndex < slice.Count)
                    PopulateEventLists(slice[pageIndex].Events);
            }
            else
            {
                int totalDays = AllDays.Count;
                int endIndex = totalDays - _pageOffset * DayPageSize;
                int startIndex = Math.Max(0, endIndex - DayPageSize);
                var slice = AllDays[startIndex..endIndex];

                if (pageIndex >= 0 && pageIndex < slice.Count)
                    PopulateEventLists(slice[pageIndex].Events);
            }
        }
    }

    private void ClearSelectionHighlight()
    {
        if (Sections[0] is RectangularSection rectangularSection)
        {
            rectangularSection.Xi = -1;
            rectangularSection.Xj = -1;
        }
    }
}