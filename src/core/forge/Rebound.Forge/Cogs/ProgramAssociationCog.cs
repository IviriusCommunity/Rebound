// Copyright (C) Ivirius(TM) Community 2020 - 2026. All Rights Reserved.
// Licensed under the MIT License.

using Rebound.Core;

#pragma warning disable CA1031 // Do not catch general exception types

namespace Rebound.Forge.Cogs;

public enum ProgramAssociationType
{
    Executable,
    URI,
    AUMID
}

public record ProgramAssociationTarget(ProgramAssociationType Type, string Destination);

/// <summary>
/// Creates a folder to the target path recursively.
/// </summary>
public partial class ProgramAssociationCog : ICog
{
    /// <inheritdoc/>
    public required string CogName { get; set; }

    /// <inheritdoc/>
    public required Guid CogId { get; set; }

    /// <inheritdoc/>
    public string CogDescription { get => $"Redirect {OriginalExecutable} to {Target.Destination}."; }

    /// <inheritdoc/>
    public bool RequiresElevation { get; } = true;

    /// <summary>
    /// The original executable's display name, with the file extension.
    /// </summary>
    public required string OriginalExecutable { get; set; }

    public required ProgramAssociationTarget Target { get; set; }

    /// <inheritdoc/>
    public async Task<CogOperationResult> ApplyAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            ReboundLogger.WriteToLog(
                "ProgramAssociationCog Apply", 
                $"Creating program association for {OriginalExecutable}...");

            await File.WriteAllTextAsync(Path.Combine(Variables.ReboundProgramAssociationsFolder, $"{OriginalExecutable}.prassoc"), Target.Type switch
            {
                ProgramAssociationType.Executable => $"PATH={Target.Destination}",
                ProgramAssociationType.URI => $"URI={Target.Destination}",
                ProgramAssociationType.AUMID => $"AUMID={Target.Destination}",
                _ => throw new NotSupportedException()
            }, cancellationToken).ConfigureAwait(true);

            ReboundLogger.WriteToLog(
                "ProgramAssociationCog Apply",
                $"Created program association for {OriginalExecutable}.");
            return new(true, null, true);
        }
        catch (Exception ex)
        {
            ReboundLogger.WriteToLog(
                "ProgramAssociationCog Apply",
                $"Failed to create program association for {OriginalExecutable}.",
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
                "ProgramAssociationCog Remove", 
                $"Deleting program association for {OriginalExecutable}...");

            File.Delete(Path.Combine(Variables.ReboundProgramAssociationsFolder, $"{OriginalExecutable}.prassoc"));

            ReboundLogger.WriteToLog(
                "ProgramAssociationCog Remove", 
                $"Program association deleted for {OriginalExecutable}.");
            return new(true, null, true);
        }
        catch (Exception ex)
        {
            ReboundLogger.WriteToLog(
                "ProgramAssociationCog Remove",
                $"Failed to delete program association for {OriginalExecutable}.",
                LogMessageSeverity.Error,
                ex);

            return new CogOperationResult(false, "EXCEPTION", false);
        }
    }

    /// <inheritdoc/>
    /// <inheritdoc/>
    public async Task<CogStatus> GetStatusAsync()
    {
        try
        {
            ReboundLogger.WriteToLog(
                "ProgramAssociationCog GetStatus",
                $"Checking status of program association for {OriginalExecutable}...");

            string filePath = Path.Combine(
                Variables.ReboundProgramAssociationsFolder,
                $"{OriginalExecutable}.prassoc");

            if (!File.Exists(filePath))
            {
                ReboundLogger.WriteToLog(
                    "ProgramAssociationCog GetStatus",
                    $"Program association for {OriginalExecutable} does not exist.");

                return new CogStatus(CogState.NotInstalled);
            }

            string expectedContents = Target.Type switch
            {
                ProgramAssociationType.Executable => $"PATH={Target.Destination}",
                ProgramAssociationType.URI => $"URI={Target.Destination}",
                ProgramAssociationType.AUMID => $"AUMID={Target.Destination}",
                _ => throw new NotSupportedException(
                    $"Unsupported association type: {Target.Type}")
            };

            string actualContents = await File.ReadAllTextAsync(filePath)
                .ConfigureAwait(false);

            bool isValid = string.Equals(
                actualContents,
                expectedContents,
                StringComparison.Ordinal);

            ReboundLogger.WriteToLog(
                "ProgramAssociationCog GetStatus",
                $"Program association for {OriginalExecutable} {(isValid ? "is valid" : "does not match the expected contents")}.");

            return new CogStatus(
                isValid ? CogState.Installed : CogState.NotInstalled);
        }
        catch (Exception ex)
        {
            ReboundLogger.WriteToLog(
                "ProgramAssociationCog GetStatus",
                $"Failed to check status of program association for {OriginalExecutable}.",
                LogMessageSeverity.Error,
                ex);

            return new CogStatus(CogState.Unknown, ex.Message);
        }
    }
}