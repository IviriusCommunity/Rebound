using System;
using System.Diagnostics;
using System.Threading.Tasks;

namespace Rebound.Forge.Engines;

internal static class IFEOEngine
{
    // Method to pause IFEO entry by copying and deleting the registry key
    public static async Task PauseIFEOEntryAsync(string executableName)
    {
        try
        {
            var basePath = @"SOFTWARE\Microsoft\Windows NT\CurrentVersion\Image File Execution Options";
            var originalKey = $"{basePath}\\{executableName}";
            var newKey = $"{basePath}\\INVALID{executableName}";

            // Check if the original IFEO entry exists
            using (var original = Microsoft.Win32.Registry.LocalMachine.OpenSubKey(originalKey, writable: true))
            {
                if (original != null)
                {
                    // Create the new key and copy values
                    using (var destination = Microsoft.Win32.Registry.LocalMachine.CreateSubKey(newKey))
                    {
                        foreach (var valueName in original.GetValueNames())
                        {
                            var value = original.GetValue(valueName);
                            var kind = original.GetValueKind(valueName);
                            destination.SetValue(valueName, value ?? "", kind);
                        }
                    }

                    // Delete the original key
                    Microsoft.Win32.Registry.LocalMachine.DeleteSubKeyTree(originalKey);
                }
            }

            await Task.CompletedTask.ConfigureAwait(false);  // Placeholder for async method if needed
        }
        catch (Exception ex)
        {
            // Handle exceptions/logging if needed
            Debug.WriteLine($"Error pausing IFEO entry: {ex.Message}");
        }
    }

    // Method to resume IFEO entry by copying it back and deleting the "INVALID" key
    public static async Task ResumeIFEOEntryAsync(string executableName)
    {
        try
        {
            var basePath = @"SOFTWARE\Microsoft\Windows NT\CurrentVersion\Image File Execution Options";
            var originalKey = $"{basePath}\\{executableName}";
            var invalidKey = $"{basePath}\\INVALID{executableName}";

            // Check if the invalid key exists
            using (var invalid = Microsoft.Win32.Registry.LocalMachine.OpenSubKey(invalidKey, writable: true))
            {
                if (invalid != null)
                {
                    // Create the original key and copy values back
                    using (var destination = Microsoft.Win32.Registry.LocalMachine.CreateSubKey(originalKey))
                    {
                        foreach (var valueName in invalid.GetValueNames())
                        {
                            var value = invalid.GetValue(valueName);
                            var kind = invalid.GetValueKind(valueName);
                            destination.SetValue(valueName, value ?? "", kind);
                        }
                    }

                    // Delete the invalid key
                    Microsoft.Win32.Registry.LocalMachine.DeleteSubKeyTree(invalidKey);
                }
            }

            await Task.CompletedTask.ConfigureAwait(false);  // Placeholder for async method if needed
        }
        catch (Exception ex)
        {
            // Handle exceptions/logging if needed
            Debug.WriteLine($"Error resuming IFEO entry: {ex.Message}");
        }
    }
}