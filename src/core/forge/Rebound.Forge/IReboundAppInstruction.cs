namespace Rebound.Forge;

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
    void Apply();

    void Remove();

    bool IsApplied();
}