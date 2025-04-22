namespace Rebound.Helpers.Modding;

public enum ReboundAppIntegrity
{
    Installed,
    Corrupt,
    NotInstalled
}

public enum InstallationTemplate
{
    Basic,
    Recommended,
    Complete,
    Extras
}

public interface IReboundAppInstruction
{
    public void Apply();

    public void Remove();

    public bool IsApplied();
}