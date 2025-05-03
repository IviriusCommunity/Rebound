using System;
using System.Globalization;
using System.IO;
using System.Xml;

namespace Rebound.Cleanup.Helpers;

internal class SettingsHelper
{
    public static T? GetValue<T>(string key, T? defaultValue = default)
    {
        try
        {
            // Define the path for the XML file
            var localAppDataPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Rebound");
            var filePath = Path.Combine(localAppDataPath, "cleanmgr.xml");

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

    public static void SetValue<T>(string key, T newValue)
    {
        try
        {
            var localAppDataPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Rebound");
            var filePath = Path.Combine(localAppDataPath, "cleanmgr.xml");

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
