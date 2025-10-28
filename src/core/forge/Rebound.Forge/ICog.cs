// Copyright (C) Ivirius(TM) Community 2020 - 2025. All Rights Reserved.
// Licensed under the MIT License.

using System.Threading.Tasks;

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
    Task ApplyAsync();

    Task RemoveAsync();

    Task<bool> IsAppliedAsync();
}