using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ReboundTpm.Models;
public class TpmReset
{
    public static async Task ResetTpmAsync(ContentDialog dial)
    {
        dial.Content = "Processing...";
        try
        {
            // Path to the PowerShell script
            string scriptPath = Path.Combine(Path.GetTempPath(), "Reset-TPM.ps1");

            // Write the PowerShell script to a temporary file
            File.WriteAllText(scriptPath, @"
            Write-Host 'Starting TPM reset process...'
            Clear-Tpm -ErrorAction Stop
            Write-Host 'TPM reset successfully completed.'
        ");

            // Set up the process to run PowerShell with elevated privileges
            ProcessStartInfo psi = new ProcessStartInfo
            {
                FileName = "powershell.exe",
                Arguments = $"-NoProfile -ExecutionPolicy Bypass -File \"{scriptPath}\"",
                Verb = "runas", // Run as administrator
                UseShellExecute = true,
                CreateNoWindow = true
            };

            // Start the process and wait for it to exit
            var process = Process.Start(psi);
            await process.WaitForExitAsync();

            // Check the exit code
            if (process.ExitCode == 0)
            {
                // Update InfoBar for success
                dial.Content = "TPM reset successfully completed.";
                dial.SecondaryButtonText = "Close";
            }
            else
            {
                // Update InfoBar for failure
                dial.Content = "TPM reset failed. Please try again.";
                dial.IsPrimaryButtonEnabled = true;
            }
            dial.IsSecondaryButtonEnabled = true;
        }
        catch (Exception ex)
        {
            // Handle exceptions and update InfoBar for failure
            dial.Content = $"Operation cancelled from User Account Control.";
            dial.IsPrimaryButtonEnabled = true;
            dial.IsSecondaryButtonEnabled = true;
        }
    }
}
