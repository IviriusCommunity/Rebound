namespace Riverside.Toolkit.Controls;

public partial class TitleBarEx
{
    // Registry keys
    public const string REG_DWM = @"Software\Microsoft\Windows\DWM"; // DWM registry path
    public const string REG_COLORPREVALENCE = "ColorPrevalence";     // Color prevalence key

    // Check if accent color is enabled on title bars and window borders
    public static bool IsAccentColorEnabledForTitleBars()
    {
        try
        {
            // Get the value
            using Microsoft.Win32.RegistryKey key = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(REG_DWM);
            return key?.GetValue(REG_COLORPREVALENCE) is int intValue && intValue == 1;
        }
        catch
        {
            return false;
        }
    }
}
