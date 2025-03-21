using Microsoft.Win32;

namespace Rebound.Helpers.Modding;

public class IFEOInstruction : IReboundAppInstruction
{
    private const string BaseRegistryPath = @"SOFTWARE\Microsoft\Windows NT\CurrentVersion\Image File Execution Options";

    public required string OriginalExecutableName { get; set; }

    public required string LauncherName { get; set; }

    public IFEOInstruction()
    {

    }

    public void Apply()
    {
        string registryPath = $@"{BaseRegistryPath}\{OriginalExecutableName}";
        string debuggerValue = $"%PROGRAMFILES%\\Rebound\\{LauncherName}";

        using RegistryKey? key = Microsoft.Win32.Registry.LocalMachine.CreateSubKey(registryPath, true);
        key?.SetValue("Debugger", debuggerValue, RegistryValueKind.String);
    }

    public void Remove()
    {
        string registryPath = $@"{BaseRegistryPath}\{OriginalExecutableName}";
        Microsoft.Win32.Registry.LocalMachine.DeleteSubKeyTree(registryPath, false);
    }

    public bool IsApplied()
    {
        string registryPath = $@"{BaseRegistryPath}\{OriginalExecutableName}";

        using RegistryKey? key = Microsoft.Win32.Registry.LocalMachine.OpenSubKey(registryPath);
        if (key == null) return false;

        string? debuggerValue = key.GetValue("Debugger") as string;
        string expectedValue = $"%PROGRAMFILES%\\Rebound\\{LauncherName}";

        return debuggerValue == expectedValue;
    }
}