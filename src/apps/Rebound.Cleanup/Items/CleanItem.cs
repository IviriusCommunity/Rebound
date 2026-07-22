// Copyright (C) Ivirius(TM) Community 2020 - 2026. All Rights Reserved.
// Licensed under the MIT License.

using CommunityToolkit.Mvvm.ComponentModel;
using Rebound.Core.Settings;
using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using static CommunityToolkit.WinUI.Animations.Expressions.ExpressionValues;

#pragma warning disable CA1031 // Do not catch general exception types

namespace Rebound.Cleanup.Items;

/// <summary>
/// Represents a cleanable item with specific targets for
/// cleaning and visual properties for UI binding.
/// </summary>
internal partial class CleanItem : ObservableObject
{
    [ObservableProperty] public partial string Name { get; set; } = string.Empty;

    [ObservableProperty] public partial string ImagePath { get; set; } = string.Empty;

    [ObservableProperty] public partial string ItemID { get; set; } = string.Empty;

    [ObservableProperty] public partial string Description { get; set; } = string.Empty;

    [ObservableProperty] public partial long Size { get; set; } = 0;

    [ObservableProperty] public partial int FileCount { get; set; } = 0;

    [ObservableProperty] public partial bool IsChecked { get; set; } = false;

    [ObservableProperty] public partial bool IsEnumerated { get; set; } = false;

    public CleanTarget[] CleanTargets { get; set; } = [];

    public string LaunchTargetPath { get; set; } = Environment.GetFolderPath(Environment.SpecialFolder.Windows);

    private bool _defaultIsChecked;

    public CleanItem(bool defaultIsChecked)
        => _defaultIsChecked = defaultIsChecked;

    partial void OnItemIDChanged(string value)
        => IsChecked = SettingsManager.GetValue($"IsChecked{ConvertStringToNumericString(ItemID)}", "cleanmgr", _defaultIsChecked);

    partial void OnIsCheckedChanged(bool oldValue, bool newValue)
        => SettingsManager.SetValue($"IsChecked{ConvertStringToNumericString(ItemID)}", "cleanmgr", newValue);

    public static string ConvertStringToNumericString(string input)
    {
        var numericString = new StringBuilder();
        foreach (var c in input)
        {
            numericString.Append((int)c);
        }
        return numericString.ToString();
    }
}

/// <summary>
/// Specifies the depth of the search when enumerating files in a directory.
/// </summary>
internal enum SearchDepth
{
    /// <summary>
    /// Only searches the containing files of the specified directory.
    /// </summary>
    TopDirectoryOnly,

    /// <summary>
    /// Recursively searches everything inside the directory.
    /// </summary>
    AllDirectories
}

internal class CleanTarget
{
    /// <summary>
    /// The target path for the clean target to search.
    /// </summary>
    public string Path { get; set; } = string.Empty;

    public SearchDepth Depth { get; set; } = SearchDepth.TopDirectoryOnly;

    /// <summary>
    /// A predicate filter gives you 100% control over file matching (Regex, extensions, names, etc.)
    /// </summary>
    public Func<FileInfo, bool> FileFilter { get; set; } = (file) => true;

    /// <summary>
    /// If this is set, the execution engine skips default deletion 
    /// and invokes this delegate instead, passing the target's 
    /// path and context.
    /// </summary>
    public Func<CleanTarget, IProgress<(long size, int count, string itemPath)>, CancellationToken, Task>? CustomDeleteAction { get; set; }

    /// <summary>
    /// If this is set, the execution engine skips default deletion 
    /// and invokes this delegate instead, passing the target's 
    /// path and context.
    /// </summary>
    public Func<CleanTarget, IProgress<(long size, int count, string itemPath)>, CancellationToken, Task<(long size, int count)>>? CustomEnumerateAction { get; set; }

    /// <summary>
    /// If true, empty subdirectories will be deleted
    /// at once with their containing files. If false,
    /// all directories will remain in place and only the
    /// files inside will be deleted.
    /// </summary>
    public bool DeleteEmptySubdirectories { get; set; }

    /// <summary>
    /// Deletes every file specified by <see cref="FileFilter"/> and reports live progress.
    /// </summary>
    public async Task ExecuteCleanAsync(
        IProgress<(long size, int count, string itemPath)> progressReporter,
        CancellationToken cancellationToken)
    {
        // If a custom action was assigned, execute it and exit
        if (CustomDeleteAction != null)
        {
            await CustomDeleteAction(this, progressReporter, cancellationToken).ConfigureAwait(false);
            return;
        }

        // Default action
        await Task.Run(() =>
        {
            if (File.Exists(Path))
            {
                var fileInfo = new FileInfo(Path);
                if (fileInfo.Name.Equals("desktop.ini", StringComparison.OrdinalIgnoreCase))
                    return;

                if (FileFilter(fileInfo))
                {
                    try
                    {
                        long size = fileInfo.Length;
                        fileInfo.Attributes = FileAttributes.Normal;
                        fileInfo.Delete();
                        progressReporter?.Report((size, 1, fileInfo.FullName));
                    }
                    catch { /* locked/in use */ }
                }
                return;
            }

            if (!Directory.Exists(Path)) return;

            var dirInfo = new DirectoryInfo(Path);

            var options = new EnumerationOptions
            {
                IgnoreInaccessible = true,
                RecurseSubdirectories = Depth == SearchDepth.AllDirectories,
                AttributesToSkip = FileAttributes.ReparsePoint
            };

            long batchSize = 0;
            int batchCount = 0;
            string lastReportedFilePath = string.Empty;
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();

            // Use EnumerateFiles with options instead of SearchOption
            foreach (FileInfo file in dirInfo.EnumerateFiles("*", options))
            {
                cancellationToken.ThrowIfCancellationRequested();

                try
                {
                    if (file.Name.Equals("desktop.ini", StringComparison.OrdinalIgnoreCase))
                        continue;

                    if (FileFilter(file))
                    {
                        long fileSize = file.Length;

                        file.Attributes = FileAttributes.Normal;
                        file.Delete();

                        // Accumulate progress metrics only on a successful deletion
                        batchSize += fileSize;
                        batchCount++;
                        lastReportedFilePath = file.FullName;

                        if (stopwatch.ElapsedMilliseconds > 100)
                        {
                            progressReporter?.Report((batchSize, batchCount, lastReportedFilePath));

                            batchSize = 0;
                            batchCount = 0;
                            stopwatch.Restart();
                        }
                    }
                }
                catch { /* skip locked/in use */ }
            }

            // Flush any remaining completed deletions left in the final batch window BEFORE folder cleanup
            if (batchCount > 0)
            {
                progressReporter?.Report((batchSize, batchCount, lastReportedFilePath));
            }

            // ---- Post-Deletion Directory Cleanup ----
            if (Depth == SearchDepth.AllDirectories && DeleteEmptySubdirectories)
            {
                try
                {
                    // Enumerate all subdirectories, sorting by path length descending 
                    // to guarantee we process the deepest child folders first.
                    var subDirs = dirInfo.GetDirectories("*", SearchOption.AllDirectories)
                                         .OrderByDescending(d => d.FullName.Length);

                    foreach (var subDir in subDirs)
                    {
                        cancellationToken.ThrowIfCancellationRequested();

                        try
                        {
                            // FIXED: Use EnumerateFileSystemInfos() which exists on DirectoryInfo
                            if (!subDir.EnumerateFileSystemInfos().Any())
                            {
                                // Strip system/hidden/readonly attributes on the folder if necessary
                                if ((subDir.Attributes & FileAttributes.ReadOnly) != 0 ||
                                    (subDir.Attributes & FileAttributes.Hidden) != 0)
                                {
                                    subDir.Attributes = FileAttributes.Normal;
                                }

                                subDir.Delete(recursive: false);
                            }
                        }
                        catch (IOException) { /* Directory is locked or became non-empty */ }
                        catch (UnauthorizedAccessException) { /* Insufficient permissions */ }
                    }
                }
                catch (Exception) { /* Guard against top-level directory access issues */ }
            }
        }, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Enumerates the contents of the target item.
    /// </summary>
    /// <returns>
    /// A tuple containing the size of the files in bytes and their number.
    /// </returns>
    public async Task<(long size, int count)> EnumerateAsync(
        IProgress<(long size, int count, string itemPath)> progressReporter,
        CancellationToken cancellationToken)
    {
        if (CustomEnumerateAction != null)
            return await CustomEnumerateAction(this, progressReporter, cancellationToken).ConfigureAwait(false);

        return await Task.Run(() =>
        {
            if (File.Exists(Path))
            {
                var fileInfo = new FileInfo(Path);
                if (fileInfo.Name.Equals("desktop.ini", StringComparison.OrdinalIgnoreCase))
                    return (0, 0);

                if (FileFilter(fileInfo))
                {
                    progressReporter?.Report((fileInfo.Length, 1, fileInfo.FullName));
                    return (fileInfo.Length, 1);
                }
                return (0, 0);
            }

            if (!Directory.Exists(Path))
                return (0, 0);

            long totalSize = 0;
            int totalFileCount = 0;

            var options = new EnumerationOptions
            {
                IgnoreInaccessible = true,
                RecurseSubdirectories = Depth == SearchDepth.AllDirectories,
                AttributesToSkip = FileAttributes.ReparsePoint
            };

            long batchSize = 0;
            int batchCount = 0;
            string lastReportedFilePath = string.Empty;
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();

            try
            {
                var dirInfo = new DirectoryInfo(Path);

                foreach (var fsInfo in dirInfo.EnumerateFileSystemInfos("*", options))
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    if (fsInfo is FileInfo fileInfo)
                    {
                        try
                        {
                            if (fileInfo.Name.Equals("desktop.ini", StringComparison.OrdinalIgnoreCase))
                                continue;

                            if (FileFilter(fileInfo))
                            {
                                totalSize += fileInfo.Length;
                                totalFileCount++;

                                batchSize += fileInfo.Length;
                                batchCount++;
                                lastReportedFilePath = fileInfo.FullName;

                                if (stopwatch.ElapsedMilliseconds > 100)
                                {
                                    progressReporter?.Report((batchSize, batchCount, lastReportedFilePath));

                                    batchSize = 0;
                                    batchCount = 0;
                                    stopwatch.Restart();
                                }
                            }
                        }
                        catch (Exception) { continue; }
                    }
                }

                // Flush any remaining items using the exact last file path processed
                if (batchCount > 0)
                {
                    progressReporter?.Report((batchSize, batchCount, lastReportedFilePath));
                }
            }
            catch
            {
                // Fail gracefully
            }

            return (totalSize, totalFileCount);
        }, cancellationToken).ConfigureAwait(false);
    }
}