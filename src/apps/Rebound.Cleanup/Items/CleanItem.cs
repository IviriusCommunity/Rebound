// Copyright (C) Ivirius(TM) Community 2020 - 2026. All Rights Reserved.
// Licensed under the MIT License.

using CommunityToolkit.Mvvm.ComponentModel;
using Rebound.Core.Settings;
using System;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

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

    public CleanTarget[] CleanTargets { get; set; } = [];

    public string LaunchTargetPath { get; set; } = Environment.GetFolderPath(Environment.SpecialFolder.Windows);

    public CleanItem(bool defaultIsChecked)
        => IsChecked = SettingsManager.GetValue($"IsChecked{ConvertStringToNumericString(ItemID)}", "cleanmgr", defaultIsChecked);

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
    public Func<CleanTarget, CancellationToken, Task>? CustomDeleteAction { get; set; }

    /// <summary>
    /// If this is set, the execution engine skips default deletion 
    /// and invokes this delegate instead, passing the target's 
    /// path and context.
    /// </summary>
    public Func<CleanTarget, CancellationToken, Task<(long size, int count)>>? CustomEnumerateAction { get; set; }

    /// <summary>
    /// Deletes every file specified by <see cref="FileFilter"/>.
    /// </summary>
    public async Task ExecuteCleanAsync(CancellationToken cancellationToken)
    {
        // If a custom action was assigned, execute it and exit
        if (CustomDeleteAction != null)
        {
            await CustomDeleteAction(this, cancellationToken).ConfigureAwait(false);
            return;
        }

        // Default action
        await Task.Run(() =>
        {
            if (File.Exists(Path))
            {
                try { File.Delete(Path); } catch { /* locked/in use */ }
                return;
            }

            if (!Directory.Exists(Path)) return;

            var dirInfo = new DirectoryInfo(Path);
            var searchOption = Depth == SearchDepth.AllDirectories ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly;

            foreach (FileInfo file in dirInfo.EnumerateFiles("*", searchOption))
            {
                cancellationToken.ThrowIfCancellationRequested();

                try
                {
                    if (FileFilter(file))
                    {
                        file.Attributes = FileAttributes.Normal;
                        file.Delete();
                    }
                }
                catch { /* skip locked/in use */ }
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
        IProgress<(long size, int count)> progressReporter,
        CancellationToken cancellationToken)
    {
        if (CustomEnumerateAction != null)
            return await CustomEnumerateAction(this, cancellationToken).ConfigureAwait(false);

        return await Task.Run(() =>
        {
            // If it's a single file target rather than a directory
            if (File.Exists(Path))
            {
                var fileInfo = new FileInfo(Path);
                if (FileFilter(fileInfo))
                {
                    progressReporter?.Report((fileInfo.Length, 1));
                    return (fileInfo.Length, 1);
                }
                return (0, 0);
            }

            if (!Directory.Exists(Path))
                return (0, 0);

            long totalSize = 0;
            int totalFileCount = 0;
            var searchOption = Depth == SearchDepth.AllDirectories
                ? SearchOption.AllDirectories
                : SearchOption.TopDirectoryOnly;

            try
            {
                var dirInfo = new DirectoryInfo(Path);

                // Enumerate files lazily to stream results back in real time
                foreach (FileInfo file in dirInfo.EnumerateFiles("*", searchOption))
                {
                    // Check for cancellation requests on heavy disk loads
                    cancellationToken.ThrowIfCancellationRequested();

                    try
                    {
                        if (FileFilter(file))
                        {
                            long fileLength = file.Length;
                            totalSize += fileLength;
                            totalFileCount += 1;

                            // Report progress up to the UI thread continuously
                            progressReporter?.Report((totalSize, totalFileCount));
                        }
                    }
                    catch (Exception)
                    {
                        // Safely skip files wrapped in system locks during scanning
                    }
                }
            }
            catch (Exception)
            {
                // Root directory unreadable or locked
            }

            return (totalSize, totalFileCount);
        }, cancellationToken).ConfigureAwait(false);
    }
}