﻿// Copyright (C) Ivirius(TM) Community 2020 - 2025. All Rights Reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using IWshRuntimeLibrary;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media.Imaging;
using Rebound.Shell.ExperiencePack;
using Windows.Storage;
using Windows.Storage.FileProperties;

namespace Rebound.Shell.Desktop;

public partial class DesktopItem : ObservableObject
{
    [ObservableProperty]
    public partial string? FileName { get; set; }

    [ObservableProperty]
    public partial string? FilePath { get; set; }

    [ObservableProperty]
    public partial BitmapImage? Thumbnail { get; set; } = null;

    [ObservableProperty]
    public partial bool IsThumbnailLoading { get; set; } = true;

    [ObservableProperty]
    public partial bool IsShortcut { get; set; } = false;

    [ObservableProperty]
    public partial bool IsSystemFile { get; set; } = false;

    [ObservableProperty]
    public partial bool IsVideoFile { get; set; } = false;

    [ObservableProperty]
    public partial bool IsHidden { get; set; } = false;

    [ObservableProperty]
    public partial bool IsExe { get; set; } = false;

    [ObservableProperty]
    public partial bool? IsSelected { get; set; } = false;

    [ObservableProperty]
    public partial double X { get; set; } = -1;

    [ObservableProperty]
    public partial double Y { get; set; } = -1;

    [ObservableProperty]
    public partial Thickness Margin { get; set; } = new(0);

    partial void OnXChanged(double oldValue, double newValue)
    {
        DesktopSettingsHelper.SetValue($"X{(FilePath ?? "").ConvertStringToNumericString()}", newValue);
        Margin = new Thickness(newValue, Y, 0, 0);
    }

    partial void OnYChanged(double oldValue, double newValue)
    {
        DesktopSettingsHelper.SetValue($"Y{(FilePath ?? "").ConvertStringToNumericString()}", newValue);
        Margin = new Thickness(X, newValue, 0, 0);
    }

    public DesktopItem(string filePath)
    {
        FilePath = filePath;
        X = DesktopSettingsHelper.GetDoubleValue($"X{filePath.ConvertStringToNumericString()}");
        Y = DesktopSettingsHelper.GetDoubleValue($"Y{filePath.ConvertStringToNumericString()}");
        Margin = new Thickness(X, Y, 0, 0);
        FileName = Path.GetFileName(filePath);
        IsThumbnailLoading = false; // Set initially
        Load(filePath);
    }

    public async void Load(string filePath)
    {
        // Run file checks in parallel
        var checkTasks = new List<Task>
    {
        Task.Run(() => IsShortcut = CheckIfShortcut(filePath)),
        Task.Run(() => IsSystemFile = CheckIfSystemFile(filePath)),
        Task.Run(() => IsHidden = IsFileHidden(filePath)),
        Task.Run(() => IsVideoFile = CheckIfVideoFile(filePath)),
        Task.Run(() => IsExe = IsProgramFile(filePath))
    };

        await Task.WhenAll(checkTasks).ConfigureAwait(true); // Wait for all checks to complete
    }

    public static bool IsFileHidden(string path)
    {
        if (!System.IO.File.Exists(path) && !Directory.Exists(path))
        {
            return false;
        }

        var attributes = System.IO.File.GetAttributes(path);
        return (attributes.HasFlag(System.IO.FileAttributes.Hidden) || attributes.HasFlag(System.IO.FileAttributes.System));
    }

    public static bool IsProgramFile(string filePath)
    {
        // Define a list of common executable file extensions
        var programExtensions = new[] { ".exe", ".com", ".bat", ".msi", ".cmd", ".vbs", ".ps1" };

        // Get the file extension from the file path (case-insensitive comparison)
        var fileExtension = Path.GetExtension(filePath)?.ToLower(System.Globalization.CultureInfo.CurrentCulture);

        // Check if the file extension matches any of the executable extensions
        return Array.Exists(programExtensions, ext => ext.Equals(fileExtension, StringComparison.OrdinalIgnoreCase));
    }

    public static async Task<BitmapImage?> GetFileIconAsync(string path)
    {
        const int maxRetries = 3;
        var attempt = 0;

        while (attempt < maxRetries)
        {
            try
            {
                // Handle desktop.ini directly
                if (Path.GetFileName(path).Equals("desktop.ini", StringComparison.OrdinalIgnoreCase))
                {
                    var uri = new Uri($"ms-appx:///Assets/shell32_151.png");
                    return new BitmapImage(uri);
                }

                // Fetch file/folder info asynchronously
                var item = await GetStorageItemAsync(path).ConfigureAwait(true);
                if (item == null)
                {
                    return null;
                }

                var thumbnail = await GetThumbnailAsync(item).ConfigureAwait(true);
                if (thumbnail == null || thumbnail.Size == 0)
                {
                    return null;
                }

                // Return the BitmapImage of the thumbnail
                return await ConvertThumbnailToBitmapImageAsync(thumbnail).ConfigureAwait(true);
            }
            catch
            {
                attempt++;
                if (attempt >= maxRetries)
                {
                    return null;
                }

                // Exponential backoff for retries
                await Task.Delay(TimeSpan.FromMilliseconds(100 * Math.Pow(2, attempt))).ConfigureAwait(true);
            }
        }

        return new();
    }

    // Fetch the storage item asynchronously (file or folder)
    private static async Task<object?> GetStorageItemAsync(string path)
    {
        if (System.IO.File.Exists(path))
        {
            return await StorageFile.GetFileFromPathAsync(path);
        }
        else if (System.IO.Directory.Exists(path))
        {
            return await StorageFolder.GetFolderFromPathAsync(path);
        }
        return null;
    }

    // Get the thumbnail for a file or folder
    private static async Task<StorageItemThumbnail?> GetThumbnailAsync(object item)
    {
        if (item is StorageFile file)
        {
            return await file.GetThumbnailAsync(ThumbnailMode.SingleItem);
        }
        else if (item is StorageFolder folder)
        {
            return await folder.GetThumbnailAsync(ThumbnailMode.SingleItem);
        }
        return null;
    }

    public static bool CheckIfVideoFile(string filePath)
    {
        // Define a list of common video file extensions
        var videoExtensions = new[] { ".mp4", ".avi", ".mov", ".mkv", ".flv", ".wmv", ".webm", ".mpeg", ".mpg", ".3gp" };

        // Get the file extension from the file path (case-insensitive comparison)
        var fileExtension = Path.GetExtension(filePath)?.ToLower(System.Globalization.CultureInfo.CurrentCulture);

        // Check if the file extension matches any of the video extensions
        return Array.Exists(videoExtensions, ext => ext.Equals(fileExtension, StringComparison.OrdinalIgnoreCase));
    }

    // Convert thumbnail stream to BitmapImage
    private static async Task<BitmapImage> ConvertThumbnailToBitmapImageAsync(StorageItemThumbnail thumbnail)
    {
        var bitmapImage = new BitmapImage();
        var stream = thumbnail.CloneStream();
        await bitmapImage.SetSourceAsync(stream);
        return bitmapImage;
    }

    [RequiresUnreferencedCode("WshRuntimeLibrary is not supported in .NET 6+")]
    public async Task LoadThumbnailAsync()
    {
        if (IsThumbnailLoading)
        {
            return;  // Prevent multiple simultaneous loads
        }

        try
        {
            IsThumbnailLoading = true;
            FilePath ??= "";

            var targetPath = FilePath;

            // Resolve shortcut target asynchronously
            if (CheckIfShortcut(FilePath))
            {
                try
                {
                    var shortcut = await Task.Run(() => {
                        var wshShell = new WshShell();
                        return wshShell.CreateShortcut(FilePath);
                    }).ConfigureAwait(true);
                    targetPath = shortcut?.TargetPath ?? "";
                    targetPath = "";
                }
                catch
                {
                    return;
                }
            }

            // Load the thumbnail asynchronously
            var thumbnail = await GetFileIconAsync(targetPath).ConfigureAwait(true);
            if (thumbnail != null)
            {
                // Efficiently scale while preserving aspect ratio
                SetThumbnailDimensions(thumbnail);

                Thumbnail = thumbnail;
            }
        }
        finally
        {
            IsThumbnailLoading = false;
        }
    }

    // Set the thumbnail dimensions efficiently while preserving aspect ratio
    private static void SetThumbnailDimensions(BitmapImage thumbnail)
    {
        var maxDimension = 100;
        if (thumbnail.PixelHeight > thumbnail.PixelWidth)
        {
            thumbnail.DecodePixelHeight = maxDimension;
            thumbnail.DecodePixelWidth = 0; // Let width auto-scale
        }
        else
        {
            thumbnail.DecodePixelWidth = maxDimension;
            thumbnail.DecodePixelHeight = 0; // Let height auto-scale
        }
    }

    // Checks if the file is a shortcut (.lnk)
    private static bool CheckIfShortcut(string filePath) => Path.GetExtension(filePath).Equals(".lnk", StringComparison.OrdinalIgnoreCase);

    // Checks if the file is a system-related file (e.g., desktop.ini)
    private static bool CheckIfSystemFile(string filePath) => Path.GetFileName(filePath).Equals("desktop.ini", StringComparison.OrdinalIgnoreCase);
}