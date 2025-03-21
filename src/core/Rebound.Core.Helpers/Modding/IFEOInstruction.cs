using Microsoft.Win32;

namespace Rebound.Helpers.Modding;

public class IFEOInstruction : IReboundAppInstruction
{
    private const string BaseRegistryPath = @"SOFTWARE\Microsoft\Windows NT\CurrentVersion\Image File Execution Options";

    public required string OriginalExecutableName { get; set; }

    public required string LauncherPath { get; set; }

    public IFEOInstruction()
    {

    }

    public void Apply()
    {
        try
        {
            string registryPath = $@"{BaseRegistryPath}\{OriginalExecutableName}";
            string debuggerValue = $"{LauncherPath}";

            using RegistryKey? key = Microsoft.Win32.Registry.LocalMachine.CreateSubKey(registryPath, true);
            key?.SetValue("Debugger", debuggerValue, RegistryValueKind.String);
        }
        catch
        {

        }
    }

    public void Remove()
    {
        try
        {
            string registryPath = $@"{BaseRegistryPath}\{OriginalExecutableName}";
            Microsoft.Win32.Registry.LocalMachine.DeleteSubKeyTree(registryPath, false);
        }
        catch
        {

        }
    }

    public bool IsApplied()
    {
        try
        {
            string registryPath = $@"{BaseRegistryPath}\{OriginalExecutableName}";

            using RegistryKey? key = Microsoft.Win32.Registry.LocalMachine.OpenSubKey(registryPath);
            if (key == null) return false;

            string? debuggerValue = key.GetValue("Debugger") as string;
            string expectedValue = $"{LauncherPath}";

            return debuggerValue == expectedValue;
        }
        catch
        {
            return false;
        }
    }
}