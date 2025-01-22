using IWshRuntimeLibrary;

#nullable enable

namespace Rebound.Helpers.Modding;

public enum IconType
{
    Default,
    Modern
}

public partial class ReboundAppShortcut
{
    public string? Path { get; set; }

    public string? OriginalIconSource { get; set; }

    public string? ModernIconSource { get; set; }

    public string? TargetPath { get; set; }

    public bool ReplaceExisting { get; set; } = true;

    public bool RunAsAdmin { get; set; } = false;

    public bool IsShortcutIntact()
    {
        return false;
    }

    public void ChangeIcon(IconType iconType)
    {
        var wsh = new WshShell();
        IWshShortcut shortcut = wsh.CreateShortcut("PATH HERE");
        shortcut.IconLocation = iconType switch
        {
            IconType.Default => OriginalIconSource,
            IconType.Modern => ModernIconSource,
            _ => throw new System.NotImplementedException()
        };
    }
}