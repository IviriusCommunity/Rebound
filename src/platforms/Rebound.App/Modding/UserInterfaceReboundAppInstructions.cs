using CommunityToolkit.Mvvm.ComponentModel;
using Rebound.Forge;

namespace Rebound.Modding;

public class UserInterfaceReboundAppInstructions : ReboundAppInstructions
{
    public virtual string Name { get; set; } = string.Empty;

    public virtual string Description { get; set; } = string.Empty;

    public virtual string Icon { get; set; } = string.Empty;

    public virtual string InstallationSteps { get; set; } = string.Empty;
}