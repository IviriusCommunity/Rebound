// Copyright (C) Ivirius(TM) Community 2020 - 2026. All Rights Reserved.
// Licensed under the MIT License.

using Windows.System;

namespace Rebound.Forge.Launchers;

public class UriLauncher : ILauncher
{
    public required Uri Uri { get; set; }

    /// <inheritdoc/>
    public async Task LaunchAsync()
    {
        await Launcher.LaunchUriAsync(Uri);
    }
}