// Copyright (C) Ivirius(TM) Community 2020 - 2026. All Rights Reserved.
// Licensed under the MIT License.

using Rebound.Core;
using Rebound.Core.UI;
using System.Runtime.InteropServices;
using TerraFX.Interop.Windows;
using Windows.Management.Deployment;

namespace Rebound.Forge.Cogs;

/// <summary>
/// Launches an executable when applied. Ignorable.
/// </summary>
/// <remarks><see cref="IsAppliedAsync"/> will always return <see langword="true"/></remarks>
public class ProcessLaunchCog : ICog
{
    /// <summary>
    /// Gets or sets the full file system path to the executable file.
    /// </summary>
    public required string ExecutablePath { get; set; }

    /// <inheritdoc/>
    public bool Ignorable { get; } = true;

    /// <inheritdoc/>
    public string TaskDescription { get => $"Launch the executable {ExecutablePath}"; }

    /// <inheritdoc/>
    public async Task ApplyAsync()
    {
        try
        {
            await Task.Delay(500);

            if (string.IsNullOrWhiteSpace(ExecutablePath))
            {
                ReboundLogger.Log("[ProcessLaunchCog] ExecutablePath is null or empty");
                return;
            }

            ReboundLogger.Log($"[ProcessLaunchCog] Attempting launch: {ExecutablePath}");

            if (!File.Exists(ExecutablePath))
            {
                ReboundLogger.Log($"[ProcessLaunchCog] File does not exist");
                return;
            }

            // Try standard shell launch first
            if (TryShellExecute(ExecutablePath, elevate: false))
                return;

            // If that failed, retry elevated
            ReboundLogger.Log("[ProcessLaunchCog] Retry with elevation");

            if (TryShellExecute(ExecutablePath, elevate: true))
                return;

            ReboundLogger.Log("[ProcessLaunchCog] All launch attempts failed");
        }
        catch (Exception ex)
        {
            ReboundLogger.Log("[ProcessLaunchCog] Exception during launch", ex);
        }
    }

    const uint SEE_MASK_NOCLOSEPROCESS = 0x00000040;
    const uint SEE_MASK_FLAG_NO_UI = 0x00000400;

    private static unsafe bool TryShellExecute(string path, bool elevate)
    {
        var sei = new SHELLEXECUTEINFOW
        {
            cbSize = (uint)sizeof(SHELLEXECUTEINFOW),
            lpFile = path.ToPointer(),
            lpVerb = elevate ? "runas".ToPointer() : null,
            nShow = SW.SW_SHOWNORMAL,
            fMask = SEE_MASK_NOCLOSEPROCESS | SEE_MASK_FLAG_NO_UI
        };

        try
        {
            if (!TerraFX.Interop.Windows.Windows.ShellExecuteExW(&sei))
            {
                int error = Marshal.GetLastWin32Error();
                LogShellError(error, elevate);
                return false;
            }

            if (sei.hProcess != HANDLE.NULL)
                TerraFX.Interop.Windows.Windows.CloseHandle(sei.hProcess);

            ReboundLogger.Log($"[ProcessLaunchCog] Launch successful ({(elevate ? "elevated" : "normal")})");
            return true;
        }
        finally
        {
            Marshal.FreeHGlobal((IntPtr)sei.lpFile);
            if (sei.lpVerb != null)
                Marshal.FreeHGlobal((IntPtr)sei.lpVerb);
        }
    }

    // =============================
    // Diagnostics
    // =============================

    private static void LogShellError(int error, bool elevate)
    {
        string mode = elevate ? "elevated" : "normal";

        string reason = error switch
        {
            2 => "File not found",
            5 => "Access denied",
            740 => "Elevation required",
            1155 => "Executable format invalid or blocked",
            1223 => "User cancelled UAC",
            _ => $"Win32 error {error}"
        };

        ReboundLogger.Log($"[ProcessLaunchCog] ShellExecute ({mode}) failed: {reason}");
    }

    /// <inheritdoc/>
    public async Task RemoveAsync()
    {

    }

    /// <inheritdoc/>
    public async Task<bool> IsAppliedAsync()
    {
        return true;
    }
}