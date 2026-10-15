// Copyright (C) Ivirius(TM) Community 2020 - 2026. All Rights Reserved.
// Licensed under the MIT License.

using Rebound.Core;

namespace Rebound.Forge.Cogs;

/// <summary>
/// Creates a program association for a specific executable to be consumed by Rebound Service Host.
/// </summary>
public partial class ProgramAssociation : ICog
{
    /// <inheritdoc/>
    public required string CogName { get; set; }

    /// <inheritdoc/>
    public required Guid CogId { get; set; }

    /// <inheritdoc/>
    public required bool RequiresElevation { get; set; }

    /// <inheritdoc/>
    public string CogDescription { get => $"Redirect {OriginalFileName} to {Target}."; }

    /// <summary>
    /// The path to the original file or directory.
    /// </summary>
    public required string OriginalFileName { get; set; }

    /// <summary>
    /// The target path to redirect the original program to.
    /// </summary>
    /// <remarks>
    /// This parameter accepts both executables and package family names.
    /// The type is inherited from usage.
    /// </remarks>
    public required string Target { get; set; }

    /// <inheritdoc/>
    public async Task<CogOperationResult> ApplyAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            ReboundLogger.WriteToLog(
                "ProgramAssociationCog Apply",
                "Apply started.");

            await File.WriteAllTextAsync(
                Path.Combine(Variables.ReboundProgramAssociationsFolder, $"{OriginalFileName}.passoc"),
                Target, 
                cancellationToken).ConfigureAwait(false);

            ReboundLogger.WriteToLog(
                "ProgramAssociationCog Apply",
                $"Created program association for {OriginalFileName}.");

            return new CogOperationResult(true, null, true);
        }
        catch (Exception ex)
        {
            ReboundLogger.WriteToLog(
                "ProgramAssociationCog Apply",
                $"Apply failed with exception.",
                LogMessageSeverity.Error,
                ex);
            return new CogOperationResult(false, "EXCEPTION", false);
        }
    }

    /// <inheritdoc/>
    public Task<CogOperationResult> RemoveAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            ReboundLogger.WriteToLog(
                "ProgramAssociationCog Remove",
                "Remove started.");

            if (File.Exists(Path.Combine(Variables.ReboundProgramAssociationsFolder, $"{OriginalFileName}.passoc")))
            {
                File.Delete(Path.Combine(Variables.ReboundProgramAssociationsFolder, $"{OriginalFileName}.passoc"));
                ReboundLogger.WriteToLog(
                    "ProgramAssociationCog Remove",
                    $"Removed program association for {OriginalFileName}.");
                return Task.FromResult(new CogOperationResult(true, null, true));
            }
            else
            {
                ReboundLogger.WriteToLog(
                    "ProgramAssociationCog Remove",
                    $"No program association found for {OriginalFileName}. Nothing to remove.",
                    LogMessageSeverity.Warning);
                return Task.FromResult(new CogOperationResult(false, "NOT_FOUND", true));
            }
        }
        catch (Exception ex)
        {
            ReboundLogger.WriteToLog(
                "ProgramAssociationCog Remove",
                "Remove failed with exception.",
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
            ReboundLogger.WriteToLog(
                "ProgramAssociationCog GetStatus",
                "GetStatus started.");

            bool exists = File.Exists(Target);

            ReboundLogger.WriteToLog(
                "ProgramAssociationCog GetStatus",
                $"IsApplied check: {Target} exists? {exists}");
            return Task.FromResult(new CogStatus(exists ? CogState.Installed : CogState.NotInstalled));
        }
        catch (Exception ex)
        {
            ReboundLogger.WriteToLog(
                "ProgramAssociationCog GetStatus",
                "GetStatus failed with exception.",
                LogMessageSeverity.Error,
                ex);
            return Task.FromResult(new CogStatus(CogState.Unknown, ex.Message));
        }
    }
}