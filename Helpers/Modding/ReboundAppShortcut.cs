#nullable enable

namespace Rebound.Helpers.Modding;

public partial class ReboundAppShortcut
{
    public string? Path { get; set; }

    public string? OriginalIconSource { get; set; }

    public string? ModernIconSource { get; set; }

    public bool ReplaceExisting { get; set; } = true;

    public bool IsShortcutIconModernized()
    {
        return false;
    }
}