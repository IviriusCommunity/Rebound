// Copyright (C) Ivirius(TM) Community 2020 - 2025. All Rights Reserved.
// Licensed under the MIT License.

using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Xml;
using TerraFX.Interop.Windows;
using Windows.Storage.Search;

namespace Rebound.Core.Helpers;

public static class SettingsHelper
{
    public static T? GetValue<T>(string key, string appName, T? defaultValue = default)
    {
        try
        {
            // Define the path for the XML file
            var localAppDataPath = Path.Combine(System.Environment.GetFolderPath(System.Environment.SpecialFolder.UserProfile), ".rebound");
            var filePath = Path.Combine(localAppDataPath, $"{appName}.xml");

            // Check if the file exists
            if (!File.Exists(filePath))
            {
                return defaultValue;
            }

            // Load the XML document
            var doc = new XmlDocument();
            doc.Load(filePath);

            // Find the setting in the XML by key
            var settingNode = doc.SelectSingleNode($"//Settings/{key}");

            // If the setting exists, return its value as the specified type
            if (settingNode != null)
            {
                var value = Convert.ChangeType(settingNode.InnerText, typeof(T), CultureInfo.InvariantCulture);
                return (T)value;
            }

            return defaultValue;
        }
        catch (Exception ex) 
        {
            return defaultValue;
        }
    }

    public static void SetValue<T>(string key, string appName, T newValue)
    {
        try
        {
            var localAppDataPath = Path.Combine(System.Environment.GetFolderPath(System.Environment.SpecialFolder.UserProfile), ".rebound");
            var filePath = Path.Combine(localAppDataPath, $"{appName}.xml");

            if (!Directory.Exists(localAppDataPath))
            {
                Directory.CreateDirectory(localAppDataPath);
            }
            File.SetAttributes(localAppDataPath, FileAttributes.Directory);

            var doc = new XmlDocument();

            // If the file exists, load it. If not, create a root.
            if (File.Exists(filePath))
            {
                doc.Load(filePath);
            }
            else
            {
                var declaration = doc.CreateXmlDeclaration("1.0", "utf-8", null);
                doc.AppendChild(declaration);

                var root = doc.CreateElement("Settings");
                doc.AppendChild(root);
            }

            // At this point, doc.DocumentElement should always exist
            var rootElement = doc.DocumentElement;
            if (rootElement == null)
            {
                rootElement = doc.CreateElement("Settings");
                doc.AppendChild(rootElement);
            }

            var settingNode = rootElement.SelectSingleNode(key);
            if (settingNode != null)
            {
                settingNode.InnerText = newValue?.ToString() ?? "";
            }
            else
            {
                var newElement = doc.CreateElement(key);
                newElement.InnerText = newValue?.ToString() ?? "";
                rootElement.AppendChild(newElement);
            }

            // Save the document to file
            doc.Save(filePath);
        }
        catch
        {

        }
    }
}

public sealed class SettingsListener : IDisposable
{
    public event EventHandler<SettingChangedEventArgs>? SettingChanged;

    private readonly string _basePath;
    private readonly Timer _timer;
    private readonly int _pollIntervalMs = 500; // check twice per second
    private FileSystemSnapshot _lastSnapshot;

    public SettingsListener()
    {
        _basePath = Path.Combine(
            System.Environment.GetFolderPath(System.Environment.SpecialFolder.UserProfile),
            ".rebound"
        );

        if (!Directory.Exists(_basePath))
            Directory.CreateDirectory(_basePath);

        _lastSnapshot = FileSystemSnapshot.Capture(_basePath);

        _timer = new Timer(OnTimerTick, null, _pollIntervalMs, _pollIntervalMs);
    }

    private void OnTimerTick(object? state)
    {
        try
        {
            var current = FileSystemSnapshot.Capture(_basePath);

            if (!_lastSnapshot.Equals(current))
            {
                _lastSnapshot = current;
                SettingChanged?.Invoke(this, new SettingChangedEventArgs("", ""));
            }
        }
        catch
        {
            // Ignore transient IO issues
        }
    }

    public void Dispose()
    {
        _timer.Dispose();
    }

    // Helper to capture file state
    private class FileSystemSnapshot
    {
        public readonly (string path, DateTime lastWrite)[] Files;

        private FileSystemSnapshot((string, DateTime)[] files)
        {
            Files = files;
        }

        public static FileSystemSnapshot Capture(string folder)
        {
            var files = Directory.GetFiles(folder, "*", SearchOption.AllDirectories)
                                 .Select(f => (f, File.GetLastWriteTimeUtc(f)))
                                 .ToArray();
            return new FileSystemSnapshot(files);
        }

        public override bool Equals(object? obj)
        {
            if (obj is not FileSystemSnapshot other) return false;
            if (Files.Length != other.Files.Length) return false;

            for (int i = 0; i < Files.Length; i++)
            {
                if (Files[i].path != other.Files[i].path || Files[i].lastWrite != other.Files[i].lastWrite)
                    return false;
            }

            return true;
        }

        public override int GetHashCode() => 0; // not used
    }
}

public class SettingChangedEventArgs : EventArgs
{
    public string Name { get; }
    public string Value { get; }

    public SettingChangedEventArgs(string name, string value)
    {
        Name = name;
        Value = value;
    }
}