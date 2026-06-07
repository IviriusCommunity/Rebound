// Copyright (C) Ivirius(TM) Community 2020 - 2026. All Rights Reserved.
// Licensed under the MIT License.

using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using Rebound.ControlPanel.Models;
using Rebound.Core.Native.Windows;
using Rebound.Core.Native.Wrappers;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using System.Xml.Linq;
using TerraFX.Interop.Windows;
using static TerraFX.Interop.Windows.Windows;

namespace Rebound.ControlPanel.Services;

/// <summary>
/// Reads Windows reliability data from the Event Log (primary) and WMI
/// Win32_ReliabilityStabilityMetrics (fallback cross-check).
/// All EVT operations are P/Invoked directly into wevtapi.dll.
/// </summary>
internal static partial class ReliabilityService
{
    /// <summary>
    /// Loads reliability data for the past <paramref name="daysBack"/> calendar
    /// days (inclusive of today). Returns one <see cref="ReliabilityDay"/> per
    /// day, sorted ascending (oldest first), so index 0 = oldest and the last
    /// element = today.
    /// </summary>
    /// <param name="daysBack">
    /// How many days back to query for.
    /// </param>
    public static async Task<List<ReliabilityDay>> LoadAsync(int daysBack = 28)
    {
        // EVT queries are blocking; hop off the main thread.
        return await Task.Run(() => LoadInternal(daysBack)).ConfigureAwait(true);
    }

    private static readonly EventSource[] Sources =
    [
        // App crashes (Event ID 1000)
        new("Application",
            "*[System[(EventID=1000)]]",
            ReliabilityEventKind.Error,
            xml => BuildAppFaultSummary(xml, "Stopped working")),

        // App hangs (Event ID 1002)
        new("Application",
            "*[System[(EventID=1002)]]",
            ReliabilityEventKind.Error,
            xml => BuildAppFaultSummary(xml, "Stopped responding")),

        // BugCheck / BSOD (Event ID 1001, source BugCheck in System log)
        new("System",
            "*[System[Provider[@Name='BugCheck'] and (EventID=1001)]]",
            ReliabilityEventKind.Error,
            _ => ("System", "Blue screen of death")),

        // Unexpected shutdown / kernel power (Event ID 41)
        new("System",
            "*[System[Provider[@Name='Microsoft-Windows-Kernel-Power'] and (EventID=41)]]",
            ReliabilityEventKind.Error,
            _ => ("System", "Unexpected shutdown")),

        // Failed Windows Update (WUClient ID 20 = download failed, 24 = install failed)
        new("System",
            "*[System[Provider[@Name='Microsoft-Windows-WindowsUpdateClient'] and (EventID=20 or EventID=24)]]",
            ReliabilityEventKind.Warning,
            xml => BuildUpdateSummary(xml, failed: true)),

        // Diagnostics-Performance boot degradation (IDs 100/101)
        new("Microsoft-Windows-Diagnostics-Performance/Operational",
            "*[System[(EventID=100 or EventID=101)]]",
            ReliabilityEventKind.Warning,
            _ => ("Windows", "Boot performance degradation")),

        // Diagnostics-Performance shutdown degradation (IDs 200/201)
        new("Microsoft-Windows-Diagnostics-Performance/Operational",
            "*[System[(EventID=200 or EventID=201)]]",
            ReliabilityEventKind.Warning,
            _ => ("Windows", "Shutdown performance degradation")),

        // Successful Windows Update (IDs 19 = install success, 43 = download success)
        new("System",
            "*[System[Provider[@Name='Microsoft-Windows-WindowsUpdateClient'] and (EventID=19 or EventID=43)]]",
            ReliabilityEventKind.Informational,
            xml => BuildUpdateSummary(xml, failed: false)),

        // Successful application recovery via WER (Application ID 1001)
        new("Application",
            "*[System[Provider[@Name='Windows Error Reporting'] and (EventID=1001)]]",
            ReliabilityEventKind.Informational,
            BuildProblemReportSummary),

        // User logon/logoff (Winlogon IDs 7001/7002)
        new("Microsoft-Windows-Winlogon/Operational",
            "*[System[(EventID=7001 or EventID=7002)]]",
            ReliabilityEventKind.Informational,
            BuildLogonSummary),
    ];

    // Per-event penalty applied to the day's raw score before clamping.
    // The score starts at 10.0 and is driven down by penalties, then decayed.
    private static double PenaltyFor(ReliabilityEventKind kind, string summary) => kind switch
    {
        ReliabilityEventKind.Error when summary.Contains("Blue screen", StringComparison.OrdinalIgnoreCase) => 8.0,
        ReliabilityEventKind.Error when summary.Contains("Unexpected shutdown", StringComparison.OrdinalIgnoreCase) => 4.0,
        ReliabilityEventKind.Error when summary.Contains("Stopped responding", StringComparison.OrdinalIgnoreCase) => 1.5,
        ReliabilityEventKind.Error => 2.5,
        ReliabilityEventKind.Warning => 0.5,
        _ => 0.0,
    };

    private static List<ReliabilityDay> LoadInternal(int daysBack)
    {
        // Calculate the days back
        var today = DateOnly.FromDateTime(DateTime.Now);
        var windowStart = today.AddDays(-(daysBack - 1));

        // Build a day-keyed dictionary
        var dayMap = new Dictionary<DateOnly, List<ReliabilityEvent>>();
        for (var d = windowStart; d <= today; d = d.AddDays(1))
            dayMap[d] = [];

        // Query each source channel
        foreach (var source in Sources)
            QueryChannel(source, windowStart, today, dayMap);

        // Compute a fallback stability index.

        // Windows exposes its own Reliability Monitor score through
        // Win32_ReliabilityStabilityMetrics.SystemStabilityIndex, and we prefer that
        // when available. The exact Windows formula is not publicly documented, so this
        // fallback intentionally models the user-facing behaviour rather than pretending
        // to be an exact clone.
        
        // The model is rolling, not per-day-isolated:
        // - the index starts healthy at 10.0;
        // - each day's reliability events apply immediate damage;
        // - quiet days recover toward 10.0;
        // - recovery is proportional to the missing health, so it is faster after a bad
        //   day and slower as the score gets close to perfect again;
        // - daily damage is softened with a cap so one noisy day does not flatten the
        //   chart all the way to 1 unless it is genuinely severe.
        
        // This keeps the graph useful: spikes show reliability damage, and the following
        // days visibly heal if the system stays quiet.
        var days = new List<ReliabilityDay>(daysBack);
        double stabilityIndex = 10.0;

        const double RecoveryRate = 0.22;
        const double DailyDamageSoftCap = 7.0;

        for (var d = windowStart; d <= today; d = d.AddDays(1))
        {
            var events = dayMap[d];

            var rawPenalty = events.Sum(e => PenaltyFor(e.Kind, e.Summary));

            // Apply a soft cap to daily damage. Small penalties remain almost unchanged,
            // while very noisy days still hurt badly without making every spike identical.
            
            // Formula:
            //   capped = cap * (1 - e^(-raw / cap))
            
            // Examples with cap=7:
            //   raw 1  -> 0.9
            //   raw 3  -> 2.4
            //   raw 7  -> 4.4
            //   raw 14 -> 6.1
            var dailyDamage = DailyDamageSoftCap * (1.0 - Math.Exp(-rawPenalty / DailyDamageSoftCap));

            // Recover before applying today's damage. This makes a quiet day visibly heal,
            // while a bad day can still interrupt recovery and pull the score down.
            stabilityIndex += (10.0 - stabilityIndex) * RecoveryRate;
            stabilityIndex -= dailyDamage;

            stabilityIndex = Math.Clamp(stabilityIndex, 1.0, 10.0);

            days.Add(new ReliabilityDay
            {
                Date = d,
                StabilityIndex = Math.Round(stabilityIndex, 1),
                Events = [.. events
                    .GroupBy(e => new
                    {
                        e.Kind,
                        e.SourceName,
                        e.Summary,
                        Minute = new DateTimeOffset(
                            e.Timestamp.Year,
                            e.Timestamp.Month,
                            e.Timestamp.Day,
                            e.Timestamp.Hour,
                            e.Timestamp.Minute,
                            0,
                            e.Timestamp.Offset)
                    })
                    .Select(g => g.First())
                    .OrderByDescending(e => e.Timestamp)],
            });
        }

        // Attempt WMI cross-check for stability values; overwrite computed values
        // if Win32_ReliabilityStabilityMetrics has data (it's pre-computed by Windows).
        TryApplyWmiStabilityMetrics(days);

        return days;
    }

    private static unsafe void QueryChannel(
        EventSource source,
        DateOnly from,
        DateOnly to,
        Dictionary<DateOnly, List<ReliabilityEvent>> dayMap)
    {
        // Build time-bounded xpath query
        var startUtc = from.ToDateTime(TimeOnly.MinValue, DateTimeKind.Local).ToUniversalTime();
        var endUtc = to.ToDateTime(TimeOnly.MaxValue, DateTimeKind.Local).ToUniversalTime();

        // EVT time format: YYYY-MM-DDTHH:MM:SS.000Z
        var startStr = startUtc.ToString("yyyy-MM-ddTHH:mm:ss.000Z", null);
        var endStr = endUtc.ToString("yyyy-MM-ddTHH:mm:ss.000Z", null);

        // Build the query and run it
        var query = $"{source.XPathFilter} and *[System[TimeCreated[@SystemTime>='{startStr}' and @SystemTime<='{endStr}']]]";

        var hQuery = EvtQuery(0, source.Channel, query, EvtQueryChannelPath | EvtQueryReverseDirection);
        if (hQuery == 0) 
            return;

        try
        {
            using var buffer = new ManagedArrayPtr<nint>(64);
            uint returned = 0;

            // Interesting how this API is designed...
            while (EvtNext(hQuery, (uint)buffer.Length, buffer, uint.MaxValue, 0, &returned))
            {
                for (var i = 0; i < returned; i++)
                {
                    var hEvent = buffer[i];
                    if (hEvent == 0) 
                        continue;

                    try
                    {
                        ProcessEvent(hEvent, source, dayMap);
                    }
                    finally
                    {
                        EvtClose(hEvent);
                    }
                }
            }
        }
        finally
        {
            EvtClose(hQuery);
        }
    }

    private static void ProcessEvent(
        nint hEvent,
        EventSource source,
        Dictionary<DateOnly, List<ReliabilityEvent>> dayMap)
    {
        var xml = RenderEventXml(hEvent);
        if (xml is null) 
            return;

        // Parse timestamp from the XML — <TimeCreated SystemTime="..."/>
        try
        {
            var doc = XDocument.Parse(xml);
            XNamespace ns = "http://schemas.microsoft.com/win/2004/08/events/event";

            var systemEl = doc.Root?.Element(ns + "System");
            var timeStr = systemEl?.Element(ns + "TimeCreated")?.Attribute("SystemTime")?.Value;

            if (!DateTimeOffset.TryParse(timeStr, out var timestamp))
                return;

            var recordIdStr = systemEl?.Element(ns + "EventRecordID")?.Value;
            if (!ulong.TryParse(recordIdStr, out var recordId))
                recordId = 0;

            // Build source name + summary from the xml
            var (sourceName, summary) = source.SummaryBuilder(xml);

            var day = DateOnly.FromDateTime(timestamp.LocalDateTime);
            if (!dayMap.TryGetValue(day, out var events))
                return;

            events.Add(new ReliabilityEvent
            {
                SourceName = sourceName,
                Summary = summary,
                Kind = source.Kind,
                Timestamp = timestamp,
                RawXml = FormatXml(xml),
                EventRecordId = recordId,
                Channel = source.Channel,
                ExecutablePath = ExtractExecutablePath(xml),
            });
        }
        catch
        {
            // Malformed XML or unexpected schema; skip the event.
        }
    }

    private static (string source, string summary) BuildAppFaultSummary(string xml, string statusText)
    {
        try
        {
            var doc = XDocument.Parse(xml);
            XNamespace ns = "http://schemas.microsoft.com/win/2004/08/events/event";
            var data = doc.Root
                ?.Element(ns + "EventData")
                ?.Elements(ns + "Data")
                .ToDictionary(e => e.Attribute("Name")?.Value ?? "", e => e.Value);

            if (data is null) return ("Application", statusText);

            // Event 1000/1002 Data fields: ApplicationName, ApplicationPath
            var appName = data.GetValueOrDefault("ApplicationName")
                ?? data.GetValueOrDefault("AppName")
                ?? "Unknown application";

            return (appName, statusText);
        }
        catch
        {
            return ("Application", statusText);
        }
    }

    public static async Task<ImageSource?> LoadIconAsync(string? executablePath)
    {
        if (string.IsNullOrWhiteSpace(executablePath))
            return new BitmapImage(new Uri("ms-appx:///Assets/Glyphs/Program.ico"));

        // Try executable icon
        var pixels = await Task.Run(() => ExtractShellIconPixels(executablePath)).ConfigureAwait(true);
        if (pixels is not null)
            return CreateBitmapFromPixels(pixels);

        // If none, try package assets
        var logoPath = await Task.Run(() => TryFindPackagedAppLogoPath(executablePath)).ConfigureAwait(true);
        if (logoPath is not null)
            return new BitmapImage(new Uri(logoPath));

        // Default icon
        return new BitmapImage(new Uri("ms-appx:///Assets/Glyphs/Program.ico"));
    }

    private static (string source, string summary) BuildProblemReportSummary(string xml)
    {
        try
        {
            var doc = XDocument.Parse(xml);
            XNamespace ns = "http://schemas.microsoft.com/win/2004/08/events/event";

            var data = doc.Root
                ?.Element(ns + "EventData")
                ?.Elements(ns + "Data")
                .ToList();

            if (data is null)
                return ("Application", "Problem report generated");

            var named = data
                .Where(e => !string.IsNullOrWhiteSpace(e.Attribute("Name")?.Value))
                .ToDictionary(e => e.Attribute("Name")!.Value, e => e.Value);

            var appName =
                GetNamed(named, "ApplicationName")
                ?? GetNamed(named, "AppName")
                ?? GetNamed(named, "FriendlyEventName")
                ?? GetNamed(named, "P1")
                ?? GetPositional(data, 0)
                ?? "Application";

            appName = CleanDisplayName(appName);

            return (appName, "Problem report generated");
        }
        catch
        {
            return ("Application", "Problem report generated");
        }
    }

    private static string? GetNamed(Dictionary<string, string> data, string name)
    => data.TryGetValue(name, out var value) ? value : null;

    private static string? GetPositional(List<XElement> data, int index)
        => index >= 0 && index < data.Count ? data[index].Value : null;

    private static string CleanDisplayName(string value)
    {
        value = Path.GetFileNameWithoutExtension(value.Trim());

        if (value.Contains('\\', StringComparison.InvariantCultureIgnoreCase) || value.Contains('/', StringComparison.InvariantCultureIgnoreCase))
            value = Path.GetFileNameWithoutExtension(value);

        return value;
    }

    private static IconPixels? ExtractShellIconPixels(string path)
        => TryExtractWithExtractIconEx(path);

    private static string? TryFindPackagedAppLogoPath(string executablePath)
    {
        try
        {
            if (!executablePath.Contains(@"\WindowsApps\", StringComparison.OrdinalIgnoreCase))
                return null;

            var manifestPath = FindPackageManifest(executablePath);
            if (manifestPath is null)
                return null;

            var packageRoot = Path.GetDirectoryName(manifestPath);
            if (packageRoot is null)
                return null;

            var relativeLogoPath = ReadLogoPathFromManifest(manifestPath);
            if (string.IsNullOrWhiteSpace(relativeLogoPath))
                return null;

            var baseLogoPath = Path.Combine(packageRoot, relativeLogoPath);
            return FindBestLogoVariant(baseLogoPath);
        }
        catch
        {
            return null;
        }
    }

    private static string? FindPackageManifest(string executablePath)
    {
        var directory = Path.GetDirectoryName(executablePath);

        while (!string.IsNullOrWhiteSpace(directory))
        {
            var manifestPath = Path.Combine(directory, "AppxManifest.xml");

            if (File.Exists(manifestPath))
                return manifestPath;

            directory = Path.GetDirectoryName(directory);
        }

        return null;
    }

    private static string? ReadLogoPathFromManifest(string manifestPath)
    {
        var doc = XDocument.Load(manifestPath);

        var visualElements = doc
            .Descendants()
            .FirstOrDefault(e => e.Name.LocalName == "VisualElements");

        if (visualElements is null)
            return null;

        string[] preferredAttributes = [
            "Square44x44Logo",
            "Square150x150Logo",
            "Logo",
            "SmallLogo",
        ];

        foreach (var attributeName in preferredAttributes)
        {
            var value = visualElements
                .Attributes()
                .FirstOrDefault(a => a.Name.LocalName == attributeName)
                ?.Value;

            if (!string.IsNullOrWhiteSpace(value))
                return value.Replace('/', Path.DirectorySeparatorChar);
        }

        return null;
    }

    private static string? FindBestLogoVariant(string baseLogoPath)
    {
        if (File.Exists(baseLogoPath))
            return baseLogoPath;

        var directory = Path.GetDirectoryName(baseLogoPath);
        var extension = Path.GetExtension(baseLogoPath);
        var nameWithoutExtension = Path.GetFileNameWithoutExtension(baseLogoPath);

        if (directory is null || string.IsNullOrWhiteSpace(extension))
            return null;

        if (!Directory.Exists(directory))
            return null;

        return Directory
            .EnumerateFiles(directory, $"{nameWithoutExtension}*{extension}")
            .OrderByDescending(ScoreLogoVariant)
            .FirstOrDefault();
    }

    private static int ScoreLogoVariant(string path)
    {
        var fileName = Path.GetFileName(path);

        if (fileName.Contains("targetsize-48", StringComparison.OrdinalIgnoreCase)) return 100;
        if (fileName.Contains("targetsize-44", StringComparison.OrdinalIgnoreCase)) return 95;
        if (fileName.Contains("targetsize-32", StringComparison.OrdinalIgnoreCase)) return 90;
        if (fileName.Contains("scale-200", StringComparison.OrdinalIgnoreCase)) return 80;
        if (fileName.Contains("scale-150", StringComparison.OrdinalIgnoreCase)) return 70;
        if (fileName.Contains("scale-100", StringComparison.OrdinalIgnoreCase)) return 60;

        return 10;
    }

    private static unsafe IconPixels? TryExtractWithExtractIconEx(string path)
    {
        HICON smallIcon = default;
        using ManagedPtr<char> pathPtr = path;

        var count = ExtractIconExW(
            pathPtr, 
            0, 
            null, 
            &smallIcon, 
            1);

        if (count == 0 || smallIcon == 0)
            return null;

        try
        {
            return HIconToPixels(smallIcon);
        }
        finally
        {
            DestroyIcon(smallIcon);
        }
    }

    private static unsafe IconPixels? HIconToPixels(nint hIcon)
    {
        ICONINFO iconInfo;

        if (!GetIconInfo((HICON)hIcon, &iconInfo))
            return null;

        try
        {
            var bmp = new BITMAP();

            if (GetObjectW(new((void*)iconInfo.hbmColor), sizeof(BITMAP), &bmp) == 0)
                return null;

            var width = bmp.bmWidth;
            var height = bmp.bmHeight;
            var stride = checked(width * 4);

            using var pixels = new ManagedArrayPtr<byte>(checked(stride * height));

            var bmi = new BITMAPINFO();
            bmi.bmiHeader.biSize = (uint)sizeof(BITMAPINFOHEADER);
            bmi.bmiHeader.biWidth = width;
            bmi.bmiHeader.biHeight = -height;
            bmi.bmiHeader.biPlanes = 1;
            bmi.bmiHeader.biBitCount = 32;
            bmi.bmiHeader.biCompression = BI_RGB;

            var hdc = GetDC(HWND.NULL);

            try
            {
                if (GetDIBits(
                    hdc,
                    new((void*)iconInfo.hbmColor),
                    0,
                    (uint)height,
                    pixels,
                    &bmi,
                    DIB_RGB_COLORS) == 0)
                {
                    return null;
                }
            }
            finally
            {
                _ = ReleaseDC(HWND.NULL, hdc);
            }

            var span = pixels.AsSpan();

            var hasAlpha = false;
            for (var i = 3; i < span.Length; i += 4)
            {
                if (span[i] != 0)
                {
                    hasAlpha = true;
                    break;
                }
            }

            if (!hasAlpha)
            {
                for (var i = 3; i < span.Length; i += 4)
                    span[i] = 255;
            }

            return new IconPixels(span.ToArray(), width, height);
        }
        finally
        {
            if (iconInfo.hbmColor != 0)
                DeleteObject(new((void*)iconInfo.hbmColor));

            if (iconInfo.hbmMask != 0)
                DeleteObject(new((void*)iconInfo.hbmMask));
        }
    }

    private static WriteableBitmap CreateBitmapFromPixels(IconPixels pixels)
    {
        var bitmap = new WriteableBitmap(pixels.Width, pixels.Height);

        using (var stream = bitmap.PixelBuffer.AsStream())
            stream.Write(pixels.Bgra, 0, pixels.Bgra.Length);

        bitmap.Invalidate();
        return bitmap;
    }

    private static (string source, string summary) BuildUpdateSummary(string xml, bool failed)
    {
        try
        {
            var doc = XDocument.Parse(xml);
            XNamespace ns = "http://schemas.microsoft.com/win/2004/08/events/event";
            var data = doc.Root
                ?.Element(ns + "EventData")
                ?.Elements(ns + "Data")
                .ToDictionary(e => e.Attribute("Name")?.Value ?? "", e => e.Value);

            // The update title is typically in "updateTitle" or param1
            var title = data?.GetValueOrDefault("updateTitle")
                ?? data?.GetValueOrDefault("param1")
                ?? "Windows Update";

            var summary = failed ? "Failed Windows Update" : "Successful Windows Update";
            return (title, summary);
        }
        catch
        {
            return ("Windows Update", failed ? "Failed Windows Update" : "Successful Windows Update");
        }
    }

    private static (string source, string summary) BuildLogonSummary(string xml)
    {
        try
        {
            var doc = XDocument.Parse(xml);
            XNamespace ns = "http://schemas.microsoft.com/win/2004/08/events/event";
            var eventId = doc.Root
                ?.Element(ns + "System")
                ?.Element(ns + "EventID")
                ?.Value;

            var summary = eventId == "7001" ? "User logged on" : "User logged off";
            return ("Windows Logon", summary);
        }
        catch
        {
            return ("Windows Logon", "Logon event");
        }
    }

    private static string? ExtractExecutablePath(string xml)
    {
        try
        {
            var doc = XDocument.Parse(xml);
            XNamespace ns = "http://schemas.microsoft.com/win/2004/08/events/event";

            var dataElements = doc.Root
                ?.Element(ns + "EventData")
                ?.Elements(ns + "Data")
                .ToList();

            if (dataElements is null || dataElements.Count == 0)
                return null;

            var namedData = dataElements
                .Where(e => !string.IsNullOrWhiteSpace(e.Attribute("Name")?.Value))
                .ToDictionary(e => e.Attribute("Name")!.Value, e => e.Value);

            var candidates = new[]
            {
            GetNamed(namedData, "ApplicationPath"),
            GetNamed(namedData, "FaultingApplicationPath"),
            GetNamed(namedData, "FaultingModulePath"),
            GetNamed(namedData, "ExceptionModule"),
            GetNamed(namedData, "AppPath"),
            GetNamed(namedData, "Path"),

            // Common positional guesses for Application Error / WER events.
            GetPositional(dataElements, 1),
            GetPositional(dataElements, 6),
            GetPositional(dataElements, 10),
        };

            foreach (var candidate in candidates)
            {
                var path = NormalizeExecutableCandidate(candidate);
                if (path is not null)
                    return path;
            }

            return null;
        }
        catch
        {
            return null;
        }
    }

    private static string? NormalizeExecutableCandidate(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;

        value = value.Trim();

        if (!value.EndsWith(".exe", StringComparison.OrdinalIgnoreCase))
            return null;

        if (Path.IsPathRooted(value) && File.Exists(value))
            return value;

        return null;
    }

    private static string FormatXml(string xml)
    {
        try
        {
            return XDocument.Parse(xml).ToString(SaveOptions.None);
        }
        catch
        {
            return xml;
        }
    }

    private static void TryApplyWmiStabilityMetrics(List<ReliabilityDay> days)
    {
        try
        {
            // Win32_ReliabilityStabilityMetrics has: TimeGenerated (datetime), SystemStabilityIndex (real)
            var wmiData = new Dictionary<DateOnly, double>();

            new WmiConnection().ExecuteWmiQuery(
                "SELECT TimeGenerated, SystemStabilityIndex FROM Win32_ReliabilityStabilityMetrics",
                obj =>
                {
                    unsafe
                    {
                        var timeStr = WmiConnection.GetString((TerraFX.Interop.Windows.IWbemClassObject*)obj, "TimeGenerated");
                        var index = WmiConnection.GetDouble((TerraFX.Interop.Windows.IWbemClassObject*)obj, "SystemStabilityIndex");

                        // WMI datetime format: yyyyMMddHHmmss.ffffff+offset
                        if (timeStr is not null && TryParseWmiDate(timeStr, out var date))
                            wmiData[date] = Math.Clamp(index, 1.0, 10.0);
                    }
                });

            // Overlay WMI values where available
            foreach (var day in days)
            {
                if (wmiData.TryGetValue(day.Date, out var wmiIndex))
                    day.StabilityIndex = Math.Round(wmiIndex, 1);
            }
        }
        catch
        {
            // WMI unavailable (e.g. service stopped) — computed values remain.
        }
    }

    private static bool TryParseWmiDate(string wmiDate, out DateOnly result)
    {
        // Format: "20260605120000.000000+060"
        result = default;
        if (wmiDate.Length < 8) return false;

        if (int.TryParse(wmiDate[..4], out var year) &&
            int.TryParse(wmiDate[4..6], out var month) &&
            int.TryParse(wmiDate[6..8], out var day))
        {
            result = new DateOnly(year, month, day);
            return true;
        }

        return false;
    }

    private const uint EvtQueryChannelPath = 0x1;
    private const uint EvtQueryReverseDirection = 0x200;
    private const uint EvtRenderEventXml = 1;

    [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
    [LibraryImport("wevtapi.dll", SetLastError = true, StringMarshalling = StringMarshalling.Utf16)]
    internal static partial nint EvtQuery(
        nint session,
        string path,
        string query,
        uint flags);

    [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
    [LibraryImport("wevtapi.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal unsafe static partial bool EvtNext(
        nint resultSet,
        uint eventArraySize,
        nint* eventArray,
        uint timeout,
        uint flags,
        uint* returned);

    [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
    [LibraryImport("wevtapi.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal unsafe static partial bool EvtRender(
        nint context,
        nint fragment,
        uint flags,
        uint bufferSize,
        char* buffer,
        uint* bufferUsed,
        uint* propertyCount);

    [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
    [LibraryImport("wevtapi.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static partial bool EvtClose(nint @object);

    private static unsafe string? RenderEventXml(nint hEvent)
    {
        const int ErrorInsufficientBuffer = 122;
        const uint InitialSizeBytes = 4096;

        using (var buffer = new ManagedArrayPtr<char>((int)(InitialSizeBytes / sizeof(char))))
        {
            uint used;

            if (EvtRender(0, hEvent, EvtRenderEventXml, InitialSizeBytes, buffer, &used, null))
                return Utf16BufferToString(buffer, used);

            if (Marshal.GetLastPInvokeError() != ErrorInsufficientBuffer)
                return null;

            using var retryBuffer = new ManagedArrayPtr<char>(
                checked((int)((used + sizeof(char) - 1) / sizeof(char))));

            return EvtRender(0, hEvent, EvtRenderEventXml, used, retryBuffer, &used, null)
                ? Utf16BufferToString(retryBuffer, used)
                : null;
        }
    }

    private static unsafe string Utf16BufferToString(ManagedArrayPtr<char> buffer, uint usedBytes)
    {
        var charCount = checked((int)(usedBytes / sizeof(char)));

        if (charCount > 0 && buffer[charCount - 1] == '\0')
            charCount--;

        return new string((char*)buffer, 0, charCount);
    }

    private const uint BI_RGB = 0;
    private const uint DIB_RGB_COLORS = 0;

    private sealed record IconPixels(byte[] Bgra, int Width, int Height);

    private sealed record EventSource(
        string Channel,
        string XPathFilter,
        ReliabilityEventKind Kind,
        Func<string, (string SourceName, string Summary)> SummaryBuilder);
}