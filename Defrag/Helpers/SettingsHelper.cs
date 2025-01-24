using Microsoft.Windows.Storage;

#nullable enable

namespace Rebound.Defrag.Helpers;

public class SettingsHelper
{
    public static T? GetValue<T>(string key)
    {
        try
        {
            var userSettings = ApplicationData.GetDefault();
            return (T)userSettings.LocalSettings.Values[key] is not null ? (T)userSettings.LocalSettings.Values[key] : default;
        }
        catch
        {
            return default;
        }
    }

    public static void SetValue<T>(string key, T newValue)
    {
        try
        {
            var userSettings = ApplicationData.GetDefault();
            userSettings.LocalSettings.Values[key] = newValue;
        }
        catch
        {
            return;
        }
    }
}