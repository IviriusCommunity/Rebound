// Copyright (C) Ivirius(TM) Community 2020 - 2026. All Rights Reserved.
// Licensed under the MIT License.

using Rebound.Core;
using Rebound.Core.Native.Wrappers;
using Rebound.Core.Settings;
using Rebound.Core.Storage;
using TerraFX.Interop.Windows;
using static TerraFX.Interop.Windows.Windows;

#pragma warning disable CA1031 // Do not catch general exception types

namespace Rebound.Forge.Cogs;

/// <summary>
/// Creates a shortcut to an executable inside the start menu in the Rebound folder.
/// </summary>
public class ShortcutCog : ICog
{
    /// <inheritdoc/>
    public required string CogName { get; set; }

    /// <inheritdoc/>
    public required Guid CogId { get; set; }

    /// <inheritdoc/>
    public required bool RequiresElevation { get; set; }

    /// <inheritdoc/>
    public string CogDescription { get => $"Create shortcut '{ShortcutName}' for executable '{ExePath}'."; }

    /// <summary>
    /// Path of the target executable to be launched.
    /// </summary>
    public required string ExePath { get; set; }

    /// <summary>
    /// The display name of the shortcut.
    /// </summary>
    public required string ShortcutName { get; set; }

    /// <summary>
    /// Location of the icon to be used. Leave empty to use the target executable's icon.
    /// </summary>
    public string? IconLocation { get; set; }

    /// <summary>
    /// Returns the full path to a Start Menu shortcut with the specified name.
    /// </summary>
    public static string GetShortcutPath(string shortcutName) =>
        Path.Combine(Variables.ReboundStartMenuFolder, $"{shortcutName}.lnk");

    /// <inheritdoc/>
    public unsafe Task<CogOperationResult> ApplyAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var shortcutPath = GetShortcutPath(ShortcutName);

            DirectoryEx.Create(Variables.ReboundStartMenuFolder);

            // Create IShellLink instance
            using ComPtr<IShellLinkW> shellLink = default;
            using ManagedPtr<Guid> iid = IID.IID_IShellLinkW;
            using ManagedPtr<Guid> clsid = CLSID.CLSID_ShellLink;

            var hr = CoCreateInstance(
                clsid,
                null,
                (uint)CLSCTX.CLSCTX_INPROC_SERVER,
                iid,
                (void**)shellLink.GetAddressOf());

            if (hr.FAILED || shellLink.Get() is null)
            {
                ReboundLogger.WriteToLog(
                    "ShortcutCog Apply",
                    $"CoCreateInstance failed. HRESULT=0x{hr.Value:X}, pointer null? {shellLink.Get() is null}",
                    LogMessageSeverity.Error);
                return Task.FromResult(new CogOperationResult(false, "COM_EXCEPTION", false));
            }

            // Set executable path
            using ManagedPtr<char> exePath = ExePath;
            shellLink.Get()->SetPath(exePath);

            // Set working directory
            using ManagedPtr<char> workingDir = Path.GetDirectoryName(ExePath)!;
            shellLink.Get()->SetWorkingDirectory(workingDir);

            // Set icon if provided
            if (!string.IsNullOrEmpty(IconLocation))
            {
                using ManagedPtr<char> iconLocation = IconLocation;
                shellLink.Get()->SetIconLocation(iconLocation, 0);
            }

            // Query IPersistFile and save
            using ComPtr<IPersistFile> persistFile = default;
            using ManagedPtr<Guid> iid2 = IID.IID_IPersistFile;

            hr = ((IUnknown*)shellLink.Get())->QueryInterface(iid2, (void**)&persistFile);

            if (hr.FAILED || persistFile.Get() is null)
            {
                ReboundLogger.WriteToLog(
                    "ShortcutCog Apply",
                    $"QueryInterface(IPersistFile) failed. HRESULT=0x{hr.Value:X}",
                    LogMessageSeverity.Error);
                return Task.FromResult(new CogOperationResult(false, "COM_EXCEPTION", false));
            }

            using ManagedPtr<char> shortcutPathPtr = shortcutPath;
            persistFile.Get()->Save(shortcutPathPtr, true);

            return Task.FromResult(new CogOperationResult(true, null, true));
        }
        catch (Exception ex)
        {
            ReboundLogger.WriteToLog(
                "ShortcutCog Apply",
                "An exception occurred while applying the shortcut cog.",
                LogMessageSeverity.Error,
                ex);
            return Task.FromResult(new CogOperationResult(false, "EXCEPTION", false));
        }
    }

    /// <inheritdoc/>
    public Task<CogOperationResult> RemoveAsync(CancellationToken cancellationToken)
    {
        try
        {
            var shortcutPath = GetShortcutPath(ShortcutName);

            if (!SettingsManager.GetValue("InstallShortcuts", "rebound", true))
                return Task.FromResult(new CogOperationResult(false, "COM_EXCEPTION", false));

            if (File.Exists(shortcutPath))
                File.Delete(shortcutPath);

            return Task.FromResult(new CogOperationResult(true, null, true));
        }
        catch (Exception ex)
        {
            ReboundLogger.WriteToLog(
                "ShortcutCog Remove",
                "An exception occurred while removing the shortcut cog.",
                LogMessageSeverity.Error,
                ex);
            return Task.FromResult(new CogOperationResult(false, "EXCEPTION", false));
        }
    }

    /// <inheritdoc/>
    public Task<CogStatus> GetStatusAsync()
    {
        try
        {
            if (!SettingsManager.GetValue("InstallShortcuts", "rebound", true))
                return Task.FromResult(new CogStatus(CogState.NotInstalled, "Shortcuts are disabled."));
                
            return Task.FromResult(new CogStatus(File.Exists(GetShortcutPath(ShortcutName)) ? CogState.Installed : CogState.NotInstalled));
        }
        catch (Exception ex)
        {
            ReboundLogger.WriteToLog(
                "ShortcutCog GetStatus",
                "An exception occurred while checking the shortcut cog.",
                LogMessageSeverity.Error,
                ex);
            return Task.FromResult(new CogStatus(CogState.Unknown, "Failed to get status."));
        }
    }
}