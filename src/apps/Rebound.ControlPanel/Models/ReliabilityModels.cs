// Copyright (C) Ivirius(TM) Community 2020 - 2026. All Rights Reserved.
// Licensed under the MIT License.

using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.UI.Xaml.Media;
using System;
using System.Collections.Generic;

namespace Rebound.ControlPanel.Models;

/// <summary>
/// Severity bucket - maps to the three severity categories 
/// in the original Reliability Monitor in Windows
/// </summary>
internal enum ReliabilityEventKind
{
    /// <summary>
    /// Application crashes, hangs, BSODs, unexpected shutdowns.
    /// </summary>
    Error,

    /// <summary>
    /// Failed Windows Updates, driver failures, performance degradations.
    /// </summary>
    Warning,

    /// <summary>
    /// Successful updates, installs, recoveries, logon/logoff.
    /// </summary>
    Informational,
}

/// <summary>
/// A single reliability event shown as one row inside an Expander.
/// </summary>
internal sealed partial class ReliabilityEvent : ObservableObject
{
    /// <summary>
    /// Display name of the source application or component.
    /// e.g. "Rebound.DiskCleanup", "Microsoft GameInput", "[insert Microsoft Store package family name here]"
    /// </summary>
    public required string SourceName { get; init; }

    /// <summary>
    /// Short human-readable status of the event.
    /// e.g. "Stopped working", "Failed Windows Update", "Successful application recovery"
    /// </summary>
    public required string Summary { get; init; }

    public required ReliabilityEventKind Kind { get; init; }

    /// <summary>
    /// The timestamp of the current event.
    /// </summary>
    public required DateTimeOffset Timestamp { get; init; }

    /// <summary>
    /// Full path to the faulting executable, used for icon extraction.
    /// Null if not applicable (e.g. Windows Update events).
    /// </summary>
    public string? ExecutablePath { get; init; }

    /// <summary>
    /// Lazily loaded icon; null until <see cref="Services.ReliabilityService.LoadIconAsync(string?)"/> is called.
    /// </summary>
    [ObservableProperty] public partial ImageSource? Icon { get; set; }

    /// <summary>
    /// The raw Event Log XML to be parsed to a XML renderer.
    /// </summary>
    public required string RawXml { get; init; }

    /// <summary>
    /// Event Record ID from the Windows Event Log.
    /// Used to retrieve more details about the event later on.
    /// </summary>
    public ulong EventRecordId { get; init; }

    /// <summary>
    /// The channel the event came from, e.g. "Application", "System".
    /// </summary>
    public required string Channel { get; init; }

    /// <summary>
    /// The date used by UI elements to render as a string directly instead
    /// of relying on specific converters.
    /// </summary>
    public string DisplayDate => Timestamp.LocalDateTime.ToString("g", null);
}

/// <summary>
/// Aggregated data for a single calendar day.
/// </summary>
internal sealed class ReliabilityDay
{
    public required DateOnly Date { get; init; }

    /// <summary>
    /// Stability index on the 1–10 scale (10 = healthy, 1 = very unstable).
    /// Feed <c>10.0 - StabilityIndex</c> to LiveCharts so the gradient renders correctly
    /// (chart Y=0 at top maps to "perfect", Y=9 maps to "worst").
    /// </summary>
    public double StabilityIndex { get; set; }

    /// <summary>
    /// The full list of events for the day.
    /// </summary>
    public IReadOnlyList<ReliabilityEvent> Events { get; init; } = [];
}

/// <summary>
/// Aggregated data for a calendar week (Mon–Sun).
/// The ViewModel produces these from a list of <see cref="ReliabilityDay"/> when
/// "View by Weeks" is selected.
/// </summary>
internal sealed class ReliabilityWeek
{
    public required DateOnly WeekStart { get; init; }
    public required DateOnly WeekEnd { get; init; }

    /// <summary>
    /// Average stability index across the days in this week that have data.
    /// </summary>
    public double StabilityIndex { get; set; }

    /// <summary>
    /// The full list of events for the week.
    /// </summary>
    public IReadOnlyList<ReliabilityEvent> Events { get; init; } = [];
}