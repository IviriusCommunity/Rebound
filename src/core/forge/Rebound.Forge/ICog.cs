namespace Rebound.Forge;

internal enum ModIntegrity
{
    Installed,
    Corrupt,
    NotInstalled
}

internal enum InstallationTemplate
{
    Basic,
    Recommended,
    Complete,
    Extras
}

internal interface ICog
{
    void Apply();

    void Remove();

    bool IsApplied();
}