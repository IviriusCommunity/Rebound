// Copyright (C) Ivirius(TM) Community 2020 - 2025. All Rights Reserved.
// Licensed under the MIT License.

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