namespace Rebound.Cleanup.Helpers;

internal class SettingsHelper
{
    public static T? GetValue<T>(string key, T? defaultValue = default)
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
            var userSettings = Microsoft.Windows.Storage.ApplicationData.GetDefault();
            userSettings.LocalSettings.Values[key] = newValue;
        }
        catch
        {
            return;
        }
    }
}