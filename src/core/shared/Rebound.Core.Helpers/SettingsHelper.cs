// Copyright (C) Ivirius(TM) Community 2020 - 2025. All Rights Reserved.
// Licensed under the MIT License.

using System;
using System.Globalization;
using System.IO;
using System.Threading;
using System.Xml;

namespace Rebound.Core.Helpers;

public static class SettingsHelper
{
    public static T? GetValue<T>(string key, string appName, T? defaultValue = default)
    {
        try
        {
            // Define the path for the XML file
            var localAppDataPath = Path.Combine(System.Environment.GetFolderPath(System.Environment.SpecialFolder.LocalApplicationData), "Rebound");
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
        catch
        {
            return defaultValue;
        }
    }

    public static void SetValue<T>(string key, string appName, T newValue)
    {
        try
        {
            var localAppDataPath = Path.Combine(System.Environment.GetFolderPath(System.Environment.SpecialFolder.LocalApplicationData), "Rebound");
            var filePath = Path.Combine(localAppDataPath, $"{appName}.xml");

            if (!Directory.Exists(localAppDataPath))
            {
                Directory.CreateDirectory(localAppDataPath);
            }

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
    private readonly string _appName;
    private readonly string _settingsPath;
    private readonly FileSystemWatcher _watcher;
    private readonly SynchronizationContext? _uiContext = SynchronizationContext.Current;
    private DateTime _lastEventTime = DateTime.MinValue;

    public event EventHandler<SettingChangedEventArgs>? SettingChanged;

    public SettingsListener(string appName)
    {
        _appName = appName;
        var basePath = Path.Combine(System.Environment.GetFolderPath(System.Environment.SpecialFolder.LocalApplicationData), "Rebound");
        _settingsPath = Path.Combine(basePath, $"{appName}.xml");

        if (!Directory.Exists(basePath))
            Directory.CreateDirectory(basePath);

        _watcher = new FileSystemWatcher(basePath, $"{appName}.xml")
        {
            NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.Size | NotifyFilters.FileName,
            EnableRaisingEvents = true
        };

        _watcher.Changed += OnFileChanged;
        _watcher.Created += OnFileChanged;
        _watcher.Renamed += OnFileChanged;
        _watcher.Deleted += OnFileChanged;
    }

    private void OnFileChanged(object? sender, FileSystemEventArgs e)
    {
        // Debounce frequent duplicate events
        var now = DateTime.Now;
        if ((now - _lastEventTime).TotalMilliseconds < 200)
            return;

        _lastEventTime = now;

        try
        {
            Thread.Sleep(100); // let the file finish writing

            if (!File.Exists(_settingsPath))
                return;

            var doc = new XmlDocument();
            doc.Load(_settingsPath);

            var root = doc.DocumentElement;
            if (root == null)
                return;

            foreach (XmlNode node in root.ChildNodes)
            {
                if (node is XmlElement element)
                {
                    var key = element.Name;
                    var value = element.InnerText;

                    void Raise() => SettingChanged?.Invoke(this, new SettingChangedEventArgs(key, value));
                    if (_uiContext != null)
                        _uiContext.Post(_ => Raise(), null);
                    else
                        Raise();
                }
            }
        }
        catch
        {
            // Ignore transient IO or XML errors
        }
    }

    public void Dispose()
    {
        _watcher.Dispose();
    }
}

public sealed class SettingChangedEventArgs : EventArgs
{
    public string Key { get; }
    public string Value { get; }

    public SettingChangedEventArgs(string key, string value)
    {
        Key = key;
        Value = value;
    }
}