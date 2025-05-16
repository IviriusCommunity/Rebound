// Copyright (C) Ivirius(TM) Community 2020 - 2025. All Rights Reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media.Imaging;
using Rebound.Helpers;
using Rebound.Shell.ExperiencePack;
using Windows.Storage;
using Windows.Storage.FileProperties;
using Windows.Win32;
using Windows.Win32.Storage.FileSystem;
using Windows.Win32.System.Com;

namespace Rebound.Shell.Desktop;

public partial class DesktopItem : ObservableObject
{
    [ObservableProperty]
    public partial string? FileName { get; set; }

    [ObservableProperty]
    public partial double Opacity { get; set; } = 1;

    [ObservableProperty]
    public partial bool IsDragging { get; set; } = false;

    [ObservableProperty]
    public partial string? FilePath { get; set; }

    [ObservableProperty]
    public partial BitmapImage? Thumbnail { get; set; } = null;

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
        SettingsHelper.SetValue($"X{(FilePath ?? "").ConvertStringToNumericString()}", "rshell.desktop", newValue);
        Margin = new Thickness(newValue, Y, 0, 0);
    }

    partial void OnYChanged(double oldValue, double newValue)
    {
        SettingsHelper.SetValue($"Y{(FilePath ?? "").ConvertStringToNumericString()}", "rshell.desktop", newValue);
        Margin = new Thickness(X, newValue, 0, 0);
    }

    public DesktopItem(string filePath)
    {
        FilePath = filePath;
        X = SettingsHelper.GetValue($"X{filePath.ConvertStringToNumericString()}", "rshell.desktop", -1);
        Y = SettingsHelper.GetValue($"Y{filePath.ConvertStringToNumericString()}", "rshell.desktop", -1);
        Margin = new Thickness(X, Y, 0, 0);
        FileName = Path.GetFileName(filePath);
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

    public async Task LoadThumbnailAsync()
    {
        try
        {
            FilePath ??= "";

            // Resolve shortcut target asynchronously
            if (CheckIfShortcut(FilePath))
            {
                //unsafe
                //{
                //    const int MAX_PATH = 260;
                //    var clsidShellLink = new Guid("00021401-0000-0000-C000-000000000046");
                //    var iidShellLink = new Guid("000214F9-0000-0000-C000-000000000046");

                //    PInvoke.CoCreateInstance(in clsidShellLink, null, CLSCTX.CLSCTX_INPROC_SERVER, in iidShellLink, out var shellLinkObj);
                //    var shellLink = (Windows.Win32.UI.Shell.IShellLinkW)shellLinkObj;
                //    ((IPersistFile)shellLink).Load(FilePath, 0);

                //    var iconPathBuffer = (char*)Marshal.AllocHGlobal(MAX_PATH * sizeof(char));
                //    string iconPath;
                //    int iconIndex;

                //    try
                //    {
                //        shellLink.GetIconLocation(new Windows.Win32.Foundation.PWSTR(iconPathBuffer), MAX_PATH, out iconIndex);
                //        iconPath = new string(iconPathBuffer).TrimEnd('\0');
                //        iconPath = Environment.ExpandEnvironmentVariables(iconPath);

                //        if (string.IsNullOrWhiteSpace(iconPath) || !File.Exists(iconPath))
                //        {
                //            WIN32_FIND_DATAW findData;
                //            shellLink.GetPath(new Windows.Win32.Foundation.PWSTR(iconPathBuffer), MAX_PATH, &findData, 0);
                //            iconPath = new string(iconPathBuffer).TrimEnd('\0');
                //            iconIndex = 0;
                //        }

                //        if (!File.Exists(iconPath))
                //            return;

                //        // Extract icon
                //        PInvoke.ExtractIconEx(iconPath, iconIndex, out var largeIcon, out _, 1);
                //        if (!largeIcon.IsInvalid)
                //        {
                //            // Clone icon properly
                //            using var sysIcon = Icon.FromHandle(largeIcon.DangerousGetHandle());
                //            using var iconCopy = (Icon)sysIcon.Clone(); // clone to avoid destroying original handle
                //            var bitmap = iconCopy.ToBitmap();
                //            using var ms = new MemoryStream();
                //            bitmap.Save(ms, System.Drawing.Imaging.ImageFormat.Png);
                //            ms.Position = 0;

                //            DispatcherQueue.GetForCurrentThread().TryEnqueue(() =>
                //            {
                //                var image = new BitmapImage();
                //                image.SetSource(ms.AsRandomAccessStream());
                //                Thumbnail = image;
                //            });

                //            PInvoke.DestroyIcon((Windows.Win32.UI.WindowsAndMessaging.HICON)largeIcon.DangerousGetHandle());
                //        }
                //    }
                //    finally
                //    {
                //        Marshal.FreeHGlobal((IntPtr)iconPathBuffer);
                //    }
                //}
            }
            else
            {
                // Load the thumbnail asynchronously
                var thumbnail = await GetFileIconAsync(FilePath);
                if (thumbnail != null)
                {
                    // Efficiently scale while preserving aspect ratio
                    SetThumbnailDimensions(thumbnail);
                    Thumbnail = thumbnail;
                    return;
                }
            }
        }
        catch
        {

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