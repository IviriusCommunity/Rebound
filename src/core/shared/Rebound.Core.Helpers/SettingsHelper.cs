// Copyright (C) Ivirius(TM) Community 2020 - 2025. All Rights Reserved.
// Licensed under the MIT License.

using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
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

public sealed class SettingsListener
{
    public event EventHandler<SettingChangedEventArgs>? SettingChanged;

    public SettingsListener()
    {
        System.Threading.Tasks.Task.Run(async () =>
        {
            var basePath = Path.Combine(System.Environment.GetFolderPath(System.Environment.SpecialFolder.UserProfile), ".rebound");
            var storageFolder = await Windows.Storage.StorageFolder.GetFolderFromPathAsync(basePath);

            if (!Directory.Exists(basePath))
                Directory.CreateDirectory(basePath);
            File.SetAttributes(basePath, FileAttributes.Directory);

            var x = storageFolder.CreateFileQueryWithOptions(new());
            x?.ContentsChanged += X_ContentsChanged;
        });
    }

    private void X_ContentsChanged(IStorageQueryResultBase sender, object args)
    {
        Debug.WriteLine("Internal screaming");

        try
        {
            SettingChanged?.Invoke(this, new SettingChangedEventArgs("", ""));
        }
        catch
        {
            // Ignore transient IO or XML errors
        }
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