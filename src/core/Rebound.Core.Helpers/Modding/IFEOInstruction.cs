using Microsoft.Win32;

namespace Rebound.Helpers.Modding;

public class IFEOInstruction : IReboundAppInstruction
{
    private const string BaseRegistryPath = @"SOFTWARE\Microsoft\Windows NT\CurrentVersion\Image File Execution Options";

    public required string Name { get; set; }

    public required string Path { get; set; }

    public IFEOInstruction()
    {

    }

    public void Apply()
    {
        string registryPath = $@"{BaseRegistryPath}\{Name}";
        string debuggerValue = $"{System.Environment.GetFolderPath(System.Environment.SpecialFolder.LocalApplicationData)}\\{Path}";

        using RegistryKey? key = Microsoft.Win32.Registry.LocalMachine.CreateSubKey(registryPath, true);
        key?.SetValue("Debugger", debuggerValue, RegistryValueKind.String);
    }

    public void Remove()
    {
        string registryPath = $@"{BaseRegistryPath}\{Name}";
        Microsoft.Win32.Registry.LocalMachine.DeleteSubKeyTree(registryPath, false);
    }

    public bool IsApplied()
    {
        string registryPath = $@"{BaseRegistryPath}\{Name}";

        using RegistryKey? key = Microsoft.Win32.Registry.LocalMachine.OpenSubKey(registryPath);
        if (key == null) return false;

        string? debuggerValue = key.GetValue("Debugger") as string;
        string expectedValue = $"{System.Environment.GetFolderPath(System.Environment.SpecialFolder.LocalApplicationData)}\\{Path}";

        return debuggerValue == expectedValue;
    }
}