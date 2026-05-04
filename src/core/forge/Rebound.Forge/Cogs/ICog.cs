// Copyright (C) Ivirius(TM) Community 2020 - 2026. All Rights Reserved.
// Licensed under the MIT License.

namespace Rebound.Forge.Cogs;

/// <summary>
/// Represents the installation or execution state of a cog.
/// </summary>
public enum CogState
{
    /// <summary>
    /// The cog is not installed or has not been applied.
    /// </summary>
    NotInstalled,

    /// <summary>
    /// The cog is fully installed and active.
    /// </summary>
    Installed,

    /// <summary>
    /// The cog is only partially installed or partially applied.
    /// This may indicate a failed or interrupted operation.
    /// </summary>
    PartiallyInstalled,

    /// <summary>
    /// The cog is in a state that can be ignored by the system
    /// without affecting overall stability or operation flow.
    /// </summary>
    Ignorable,

    /// <summary>
    /// The cog state could not be determined.
    /// This typically indicates an error or unsupported configuration.
    /// </summary>
    Unknown
}

/// <summary>
/// Represents the result of querying a cog's current state.
/// </summary>
/// <param name="State">The resolved state of the cog.</param>
/// <param name="Message">
/// Optional diagnostic or informational message describing the state result.
/// </param>
public record CogStatus(CogState State, string? Message = null);

/// <summary>
/// Represents the result of a cog operation such as apply or remove.
/// </summary>
/// <param name="Success">Indicates whether the operation completed successfully.</param>
/// <param name="Error">
/// Optional error message describing why the operation failed.
/// </param>
/// <param name="SafeToContinue">
/// Indicates whether the system can safely continue executing subsequent cogs
/// after this operation completes.
/// </param>
/// <param name="Ignorable">
/// Indicates that failures from this operation may be safely ignored
/// by higher-level orchestration logic.
/// </param>
public record CogOperationResult(
    bool Success,
    string? Error,
    bool SafeToContinue,
    bool Ignorable = false);

/// <summary>
/// Defines the contract for a Rebound cog (mod unit).
/// A cog represents a discrete unit of behavior that can be applied,
/// removed, and queried for state in a controlled and deterministic manner.
/// </summary>
/// <remarks>
/// Implementations must be fully deterministic and must not rely on reflection,
/// dynamic type discovery, or runtime code generation. All behavior must be
/// explicitly defined at compile time due to Native AOT constraints.
/// </remarks>
public interface ICog
{
    /// <summary>
    /// Applies the cog to the current system context.
    /// </summary>
    /// <param name="cancellationToken">
    /// Token used to cancel the operation. Implementations must respect cancellation promptly.
    /// </param>
    /// <returns>
    /// A result describing whether the operation succeeded and whether execution
    /// can safely continue.
    /// </returns>
    Task<CogOperationResult> ApplyAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Removes the cog from the current system context, reverting any changes
    /// previously applied by <see cref="ApplyAsync"/>.
    /// </summary>
    /// <param name="cancellationToken">
    /// Token used to cancel the operation. Implementations must respect cancellation promptly.
    /// </param>
    /// <returns>
    /// A result describing whether the operation succeeded and whether execution
    /// can safely continue.
    /// </returns>
    Task<CogOperationResult> RemoveAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves the current installation or application state of the cog.
    /// </summary>
    /// <returns>
    /// A <see cref="CogStatus"/> representing the cog's current state and optional diagnostic message.
    /// </returns>
    Task<CogStatus> GetStatusAsync();

    /// <summary>
    /// Gets the display name of the cog. Must be stable and unique within a given mod package.
    /// </summary>
    string CogName { get; }

    /// <summary>
    /// Gets a human-readable description of the cog's behavior and purpose.
    /// This is intended for UI display and automated task listing.
    /// </summary>
    string CogDescription { get; }

    /// <summary>
    /// Gets the globally unique identifier for this cog.
    /// This value must remain constant across versions of the same cog.
    /// </summary>
    Guid CogId { get; }

    /// <summary>
    /// Indicates whether this cog requires elevated privileges to execute.
    /// If true, the host must validate elevation before invoking Apply or Remove operations.
    /// </summary>
    bool RequiresElevation { get; }
}

public class ConfirmationPromptEventArgs : EventArgs
{
    /// <summary>
    /// A TaskCompletionSource that the UI can use to signal the user's decision.
    /// The UI should set the result to true if the user confirms, or false if they decline.
    /// </summary>
    public TaskCompletionSource<bool> UserResponse { get; } = new TaskCompletionSource<bool>();
}

public interface IConfirmationPromptCog : ICog
{
    /// <summary>
    /// Gets or sets whether the operation requires explicit user consent or not.
    /// </summary>
    bool RequiresUserConfirmation { get; set; }
    /// <summary>
    /// Raised when the cog needs user confirmation to proceed with its operation. The event args
    /// provide a <see cref="TaskCompletionSource{Boolean}"/> that the UI can use to signal the user's decision.
    /// </summary>
    event EventHandler<ConfirmationPromptEventArgs>? OnConfirmationRequested;
}