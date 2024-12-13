using System;
using Microsoft.Win32;

namespace Rebound.Run.Helpers;

public static class GroupPolicyHelper
{
    public const string EXPLORER_GROUP_POLICY_PATH = @"Software\Microsoft\Windows\CurrentVersion\Policies\Explorer";

    public static bool? IsGroupPolicyEnabled(string path, string value, int trueValue)
    {
        try
        {
            // Path to the registry key
            var registryKeyPath = path;
            // Name of the value we are looking for
            var valueName = value;

            // Open the registry key
            using var key = Registry.CurrentUser.OpenSubKey(registryKeyPath);
            if (key != null)
            {
                var val = key.GetValue(valueName);

                if (val != null && (int)val == trueValue)
                {
                    // Run box is disabled
                    return true;
                }
                else
                {
                    // Run box is enabled
                    return false;
                }
            }
            else
            {
                // Key not found, assume Run box is enabled
                return null;
            }
        }
        catch (Exception)
        {
            // Handle any exceptions
            return null;
        }
    }
}
