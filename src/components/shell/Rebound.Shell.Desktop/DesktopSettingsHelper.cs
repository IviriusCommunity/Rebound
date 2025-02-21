namespace Rebound.Shell.Desktop;

public static partial class DesktopSettingsHelper
{
    public static T? GetValue<T>(string key)
    {
        try
        {
            var userSettings = Microsoft.Windows.Storage.ApplicationData.GetDefault();
            if (userSettings?.LocalSettings?.Values.ContainsKey(key) == true)
            {
                if (userSettings.LocalSettings.Values[key] is T value)
                {
                    return value;
                }
            }
            return default;
        }
        catch
        {
            return default;
        }
    }

    public static double GetDoubleValue(string key)
    {
        try
        {
            var userSettings = Microsoft.Windows.Storage.ApplicationData.GetDefault();
            if (userSettings?.LocalSettings?.Values.ContainsKey(key) == true)
            {
                if (userSettings.LocalSettings.Values[key] is double value)
                {
                    return value;
                }
            }
            return -1;
        }
        catch
        {
            return -1;
        }
    }

    public static void SetValue<T>(string key, T newValue)
    {
        try
        {
            var userSettings = Microsoft.Windows.Storage.ApplicationData.GetDefault();
            userSettings.LocalSettings.Values[key] = newValue;
        }
        catch
        {
            return;
        }
    }
}