using System;
using Windows.Storage;

namespace Rebound.Common.Helpers;

public static class SettingsHelper
{
    public static Object GetSetting(string key)
    {
        ApplicationDataContainer LocalSettings = ApplicationData.Current.LocalSettings;
        return LocalSettings.Values[key];
    }

    public static Int32 GetSettingInt(string key)
    {
        ApplicationDataContainer LocalSettings = ApplicationData.Current.LocalSettings;
        return (Int32)LocalSettings.Values[key];
    }

    public static Boolean GetSettingBool(string key)
    {
        ApplicationDataContainer LocalSettings = ApplicationData.Current.LocalSettings;
        return (Boolean)LocalSettings.Values[key];
    }

    public static String GetSettingString(string key)
    {
        ApplicationDataContainer LocalSettings = ApplicationData.Current.LocalSettings;
        return (String)LocalSettings.Values[key];
    }

    public static void SetSetting(string key, object value)
    {
        ApplicationDataContainer LocalSettings = ApplicationData.Current.LocalSettings;
        LocalSettings.Values[key] = value;
    }
}
