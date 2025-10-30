// Copyright (C) Ivirius(TM) Community 2020 - 2025. All Rights Reserved.
// Licensed under the MIT License.

using System.Threading.Tasks;

namespace Rebound.Forge;

public enum ModIntegrity
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

public interface ICog
{
    Task ApplyAsync();

    Task RemoveAsync();

    Task<bool> IsAppliedAsync();
}