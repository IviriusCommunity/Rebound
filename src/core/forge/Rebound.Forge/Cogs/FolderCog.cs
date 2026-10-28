// Copyright (C) Ivirius(TM) Community 2020 - 2026. All Rights Reserved.
// Licensed under the MIT License.

using Rebound.Core;
using Rebound.Core.Storage;
using System.Security.AccessControl;
using System.Security.Principal;

#pragma warning disable CA1031 // Do not catch general exception types

namespace Rebound.Forge.Cogs;

/// <summary>
/// Creates a folder to the target path recursively.
/// </summary>
public partial class FolderCog : ICog
{
    /// <inheritdoc/>
    public required string CogName { get; set; }

    /// <inheritdoc/>
    public required Guid CogId { get; set; }

    /// <inheritdoc/>
    public string CogDescription { get => $"Create folder at {Path}."; }

    /// <inheritdoc/>
    public required bool RequiresElevation { get; set; }

    /// <summary>
    /// The full path to the directory.
    /// </summary>
    public required string Path { get; set; }

    /// <summary>
    /// Determines if the folder should be deleted or not when <see cref="RemoveAsync"/> is called.
    /// </summary>
    public required bool PersistAfterRemoving { get; set; }

    /// <summary>
    /// Determines if the folder should be locked behind admin privileges. If <see langword="true"/>,
    /// also set <see cref="RequiresElevation"/> to <see langword="true"/>.
    /// </summary>
    public required bool LockBehindAdmin { get; set; }

    /// <inheritdoc/>
    public async Task<CogOperationResult> ApplyAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            ReboundLogger.WriteToLog(
                "FolderCog Apply", 
                $"Creating folder at {Path}...");

            DirectoryEx.Create(Path);
            if (LockBehindAdmin)
                EnsureProtectedDirectory(Path);

            ReboundLogger.WriteToLog(
                "FolderCog Apply", 
                $"Folder created at {Path}.");
            return new(true, null, true);
        }
        catch (Exception ex)
        {
            ReboundLogger.WriteToLog(
                "FolderCog Apply",
                $"Failed to create folder at {Path}.",
                LogMessageSeverity.Error,
                ex);

            return new CogOperationResult(false, "EXCEPTION", false);
        }
    }

    /// <inheritdoc/>
    public async Task<CogOperationResult> RemoveAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            ReboundLogger.WriteToLog(
                "FolderCog Remove", 
                $"Deleting folder at {Path}...");

            // Persistence is enabled, skip deletion
            if (PersistAfterRemoving)
            {
                ReboundLogger.WriteToLog(
                    "FolderCog Remove",
                    $"Skipping deletion of folder at {Path} due to PersistAfterRemoving being set to true.");
                return new(false, null, true, true);
            }

            Directory.Delete(Path);

            ReboundLogger.WriteToLog(
                "FolderCog Remove", 
                $"Folder deleted at {Path}.");
            return new(true, null, true);
        }
        catch (Exception ex)
        {
            ReboundLogger.WriteToLog(
                "FolderCog",
                $"Failed to delete folder at {Path}.",
                LogMessageSeverity.Error,
                ex);

            return new CogOperationResult(false, "EXCEPTION", false);
        }
    }

    /// <inheritdoc/>
    public Task<CogStatus> GetStatusAsync()
    {
        try
        {
            ReboundLogger.WriteToLog(
                "FolderCog GetStatus", 
                $"Checking status of folder at {Path}...");

            bool exists = Directory.Exists(Path);

            ReboundLogger.WriteToLog(
                "FolderCog GetStatus", 
                $"Folder at {Path} {(exists ? "exists" : "does not exist")}.");
            return Task.FromResult(new CogStatus(exists ? CogState.Installed : CogState.NotInstalled));
        }
        catch (Exception ex)
        {
            ReboundLogger.WriteToLog(
                "FolderCog GetStatus",
                $"Failed to check status of folder at {Path}.",
                LogMessageSeverity.Error,
                ex);

            return Task.FromResult(new CogStatus(CogState.Unknown, ex.Message));
        }
    }

    /// <summary>
    /// Creates a directory at the specified path and sets access permissions to allow only the local Administrators
    /// group and the Local System account full control.
    /// </summary>
    /// <remarks>Existing permissions are replaced and inheritance is disabled.</remarks>
    /// <param name="path">The path of the directory to create and protect.</param>
    private static void EnsureProtectedDirectory(string path)
    {
        Directory.CreateDirectory(path);

        var user = new SecurityIdentifier(
            WellKnownSidType.AuthenticatedUserSid, null);

        var administrators = new SecurityIdentifier(
            WellKnownSidType.BuiltinAdministratorsSid, null);

        var system = new SecurityIdentifier(
            WellKnownSidType.LocalSystemSid, null);

        var security = new DirectorySecurity();
        security.SetAccessRuleProtection(
            isProtected: true,
            preserveInheritance: false);

        security.SetOwner(administrators);

        const InheritanceFlags inheritance =
            InheritanceFlags.ContainerInherit | InheritanceFlags.ObjectInherit;

        security.AddAccessRule(new FileSystemAccessRule(
            user,
            FileSystemRights.ReadAndExecute,
            inheritance,
            PropagationFlags.None,
            AccessControlType.Allow));

        security.AddAccessRule(new FileSystemAccessRule(
            administrators,
            FileSystemRights.FullControl,
            inheritance,
            PropagationFlags.None,
            AccessControlType.Allow));

        security.AddAccessRule(new FileSystemAccessRule(
            system,
            FileSystemRights.FullControl,
            inheritance,
            PropagationFlags.None,
            AccessControlType.Allow));

        new DirectoryInfo(path).SetAccessControl(security);
    }
}