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
            var registryPath = $@"{BaseRegistryPath}\{OriginalExecutableName}";
            var debuggerValue = $"{LauncherPath}";

            using var key = Microsoft.Win32.Registry.LocalMachine.CreateSubKey(registryPath, true);
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
            var registryPath = $@"{BaseRegistryPath}\{OriginalExecutableName}";
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
            var registryPath = $@"{BaseRegistryPath}\{OriginalExecutableName}";

            using var key = Microsoft.Win32.Registry.LocalMachine.OpenSubKey(registryPath);
            if (key == null) 
                return false;

            var debuggerValue = key.GetValue("Debugger") as string;
            var expectedValue = $"{LauncherPath}";

            return debuggerValue == expectedValue;
        }
        catch
        {
            return false;
        }
    }
}