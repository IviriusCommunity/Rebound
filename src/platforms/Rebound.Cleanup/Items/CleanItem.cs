using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Text;
using CommunityToolkit.Mvvm.ComponentModel;
using Windows.Win32;

namespace Rebound.Cleanup.Items;

internal enum ItemType
{
    Normal,
    ThumbnailCache,
    RecycleBin,
    FirefoxCookies,
    FirefoxHistory,
    ChromiumCookies,
    ChromiumHistory
}

internal partial class CleanItem : ObservableObject
{
    [ObservableProperty]
    public partial string Name { get; set; }

    [ObservableProperty]
    public partial string ImagePath { get; set; }

    [ObservableProperty]
    public partial string ItemPath { get; set; }

    [ObservableProperty]
    public partial string ItemID { get; set; }

    [ObservableProperty]
    public partial string Description { get; set; }

    [ObservableProperty]
    public partial string DisplaySize { get; set; } = "0 B";

    [ObservableProperty]
    public partial long Size { get; set; } = 0;

    [ObservableProperty]
    public partial bool IsChecked { get; set; } = false;

    public ObservableCollection<string> FilePaths { get; set; } = [];

    partial void OnIsCheckedChanged(bool oldValue, bool newValue)
    {
        Helpers.SettingsHelper.SetValue($"IsChecked{ConvertStringToNumericString(ItemID)}", newValue);
    }

    partial void OnSizeChanged(long oldValue, long newValue)
    {
        DisplaySize = FormatSize(Size);
    }

    private readonly ItemType _itemType;

    public CleanItem(string name, string imagePath, string description, string itemPath, string id, bool defaultIsChecked, ItemType itemType = ItemType.Normal)
    {
        Name = name;
        ImagePath = imagePath;
        Description = description;
        _itemType = itemType;
        ItemPath = itemPath;
        ItemID = id;
        IsChecked = Helpers.SettingsHelper.GetValue($"IsChecked{ConvertStringToNumericString(ItemID)}", defaultIsChecked);
        Refresh();
    }

    public long CalculateTotalFileSize()
    {
        long totalSize = 0;

        foreach (var path in FilePaths)
        {
            try
            {
                if (File.Exists(path))
                {
                    var fileInfo = new FileInfo(path);
                    totalSize += fileInfo.Length;
                }
            }
            catch (UnauthorizedAccessException)
            {

            }
            catch (IOException)
            {

            }
        }

        return totalSize;
    }

    public void Delete()
    {
        if (_itemType == ItemType.RecycleBin)
        {
            PInvoke.SHEmptyRecycleBin(new Windows.Win32.Foundation.HWND(0), ItemPath[..3], 0x00000007);
        }
        else
        {
            foreach (var file in FilePaths)
            {
                try
                {
                    if (File.Exists(file))
                    {
                        File.Delete(file);
                    }
                }
                catch (UnauthorizedAccessException)
                {

                }
                catch (PathTooLongException)
                {

                }
                catch (IOException)
                {

                }
            }
        }

        Refresh();
    }

    private void Refresh()
    {
        FilePaths = GetFilesFromFolders(_itemType);
        Size = CalculateTotalFileSize();
    }

    public static string ConvertStringToNumericString(string input)
    {
        var numericString = new StringBuilder();
        foreach (var c in input)
        {
            numericString.Append((int)c);
        }
        return numericString.ToString();
    }

    public ObservableCollection<string> GetFilesFromFolders(ItemType itemType)
    {
        var allFiles = new ObservableCollection<string>();

        try
        {
            var files = Directory.EnumerateFiles(ItemPath, "*", SearchOption.AllDirectories);
            foreach (var file in files)
            {
                var fileInfo = new FileInfo(file);

                switch (itemType)
                {
                    case ItemType.ThumbnailCache:
                        if (fileInfo.Extension.Contains("db", StringComparison.OrdinalIgnoreCase) &&
                            fileInfo.Name.Contains("thumbcache", StringComparison.OrdinalIgnoreCase))
                        {
                            allFiles.Add(file);
                        }
                        break;

                    case ItemType.FirefoxCookies:
                        if (fileInfo.Name.Equals("cookies.sqlite", StringComparison.OrdinalIgnoreCase))
                        {
                            allFiles.Add(file);
                        }
                        break;

                    case ItemType.FirefoxHistory:
                        if (fileInfo.Name.Equals("places.sqlite", StringComparison.OrdinalIgnoreCase))
                        {
                            allFiles.Add(file);
                        }
                        break;
                    case ItemType.ChromiumCookies:
                        if (fileInfo.Name.Equals("Cookies", StringComparison.OrdinalIgnoreCase))
                        {
                            allFiles.Add(file);
                        }
                        break;

                    case ItemType.ChromiumHistory:
                        if (fileInfo.Name.Equals("History", StringComparison.OrdinalIgnoreCase))
                        {
                            allFiles.Add(file);
                        }
                        break;
                    default:
                        allFiles.Add(file);
                        break;
                }
            }
        }
        catch (UnauthorizedAccessException)
        {

        }
        catch (PathTooLongException)
        {

        }
        catch (IOException)
        {

        }

        return allFiles;
    }

    public static string FormatSize(long sizeInBytes)
    {
        return sizeInBytes < 1024
            ? $"{sizeInBytes} B"
            : sizeInBytes < 1024 * 1024
                ? $"{sizeInBytes / 1024.0:F2} KB"
                : sizeInBytes < 1024 * 1024 * 1024
                            ? $"{sizeInBytes / (1024.0 * 1024):F2} MB"
                            : sizeInBytes < 1024L * 1024 * 1024 * 1024
                                        ? $"{sizeInBytes / (1024.0 * 1024 * 1024):F2} GB"
                                        : sizeInBytes < 1024L * 1024 * 1024 * 1024 * 1024
                                                ? $"{sizeInBytes / (1024.0 * 1024 * 1024 * 1024):F2} TB"
                                                : $"{sizeInBytes / (1024.0 * 1024 * 1024 * 1024 * 1024):F2} PB";
    }
}