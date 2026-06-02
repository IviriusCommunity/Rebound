// Copyright (C) Ivirius(TM) Community 2020 - 2026. All Rights Reserved.
// Licensed under the MIT License.

using System.Diagnostics;
using Microsoft.Windows.AppLifecycle;
using Windows.ApplicationModel.Activation;

namespace Rebound.Core.UI;

/// <summary>
/// Controls whether the app allows multiple simultaneous instances.
/// </summary>
public enum InstanceMode
{
    /// <summary>
    /// Only one instance is allowed. Subsequent launches are redirected to the
    /// existing instance via <see cref="AppInstance.RedirectActivationToAsync"/>.
    /// </summary>
#pragma warning disable CA1720 // Shut up
    Single,
#pragma warning restore CA1720

    /// <summary>
    /// Every launch creates its own independent instance. No redirection occurs.
    /// </summary>
    Multiple
}

/// <summary>
/// Options for <see cref="SingleInstanceAppService.Relaunch"/>.
/// </summary>
public sealed class InstanceRelaunchOptions
{
    /// <summary>
    /// When <see langword="true"/>, the new instance is spawned with elevation (runas).
    /// </summary>
    public bool Elevated { get; init; }

    /// <summary>
    /// When <see langword="true"/>, the current process is killed after the new
    /// instance has been spawned.
    /// </summary>
    public bool ShutdownCurrent { get; init; }

    /// <summary>
    /// When <see langword="true"/>, the new instance always registers as a fresh
    /// instance (GUID key) regardless of the app's <see cref="InstanceMode"/>.
    /// After registration the new instance respects the app's normal mode.
    /// </summary>
    public bool ForceNewInstance { get; init; }

    /// <summary>
    /// Arguments forwarded to the new instance.
    /// When <see langword="null"/>, the current activation arguments are forwarded
    /// as-is (launch arguments string only).
    /// </summary>
    public string? Arguments { get; init; }
}

/// <summary>
/// Carries the full platform activation payload delivered to
/// <see cref="SingleInstanceAppService.Launched"/>.
/// </summary>
public sealed class SingleInstanceLaunchEventArgs(
    AppActivationArguments activationArguments,
    bool isFirstLaunch) : EventArgs
{
    /// <summary>
    /// The raw platform activation arguments.
    /// Cast <see cref="AppActivationArguments.Data"/> according to
    /// <see cref="AppActivationArguments.Kind"/>:
    /// <list type="bullet">
    ///   <item><see cref="ExtendedActivationKind.Launch"/>    → <see cref="ILaunchActivatedEventArgs"/></item>
    ///   <item><see cref="ExtendedActivationKind.Protocol"/>  → <see cref="IProtocolActivatedEventArgs"/></item>
    ///   <item><see cref="ExtendedActivationKind.File"/>      → <see cref="IFileActivatedEventArgs"/></item>
    /// </list>
    /// </summary>
    public AppActivationArguments ActivationArguments { get; } = activationArguments;

    /// <summary>
    /// <see langword="true"/> when this is the process that won the instance key
    /// (i.e. not a redirected activation from a second launch).
    /// </summary>
    public bool IsFirstLaunch { get; } = isFirstLaunch;

    /// <summary>
    /// Convenience accessor for the raw argument string when the activation kind
    /// is <see cref="ExtendedActivationKind.Launch"/>.
    /// Returns <see cref="string.Empty"/> for all other kinds.
    /// </summary>
    public string LaunchArguments =>
        ActivationArguments.Kind == ExtendedActivationKind.Launch
            ? string.Join(" ", System.Environment.GetCommandLineArgs().Skip(1))
            : string.Empty;
}

/// <summary>
/// Manages single- or multi-instance lifetime for a packaged WinUI 3 application
/// built on <see cref="AppInstance"/> from Microsoft.Windows.AppLifecycle.
/// <para>
/// Drop-in replacement for the previous mutex + named-pipe implementation.
/// The <see cref="Launched"/> event contract is preserved; only the event-args
/// type has changed to carry the full <see cref="AppActivationArguments"/>.
/// </para>
/// </summary>
/// <param name="appId">
/// Stable, unique identifier for this application
/// (e.g. <c>"Rebound.ControlPanel"</c>).
/// Used as the <see cref="AppInstance"/> registration key when
/// <see cref="InstanceMode.Single"/> is in effect.
/// </param>
/// <param name="mode">
/// Whether to enforce a single instance or allow multiple.
/// Defaults to <see cref="InstanceMode.Single"/>.
/// </param>
public sealed partial class SingleInstanceAppService(string appId, InstanceMode mode = InstanceMode.Single) : IDisposable
{
    private AppInstance? _instance;
    private bool _disposed;

    /// <summary>
    /// Fired whenever this instance should handle an activation — both on first
    /// launch and when a redirected activation arrives from a second launch
    /// (single-instance mode only).
    /// </summary>
    public event EventHandler<SingleInstanceLaunchEventArgs>? Launched;

    /// <summary>
    /// Call once from <c>Application.OnLaunched</c>.
    /// Registers this process with <see cref="AppInstance"/>, redirects if a
    /// primary instance already exists (single-instance mode), and fires
    /// <see cref="Launched"/>.
    /// </summary>
    public void Launch()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        var activationArgs = AppInstance.GetCurrent().GetActivatedEventArgs();

        // In Multiple mode every launch is independent — skip registration.
        if (mode == InstanceMode.Multiple)
        {
            FireLaunched(activationArgs, isFirstLaunch: true);
            return;
        }

        // Single mode: compete for the fixed key.
        _instance = AppInstance.FindOrRegisterForKey(appId);

        if (_instance.IsCurrent)
        {
            // We won — subscribe to future redirected activations.
            _instance.Activated += OnRedirectedActivation;
            FireLaunched(activationArgs, isFirstLaunch: true);
        }
        else
        {
            // Another instance holds the key — redirect and exit.
            RedirectAndExit(_instance, activationArgs);
        }
    }

    /// <summary>
    /// Spawns a new instance of the application with the supplied options.
    /// </summary>
    /// <param name="options">Controls elevation, shutdown, forced new instance, and arguments.</param>
    public void Relaunch(InstanceRelaunchOptions options)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        var exe = Process.GetCurrentProcess().MainModule?.FileName
            ?? throw new InvalidOperationException("Cannot determine current executable path.");

        // When forcing a new instance unregister our key first so the new
        // process can win it (single-instance mode) or register freely (multiple).
        if (options?.ForceNewInstance == true)
            _instance?.UnregisterKey();

        var args = options?.Arguments ?? GetCurrentLaunchArguments();

        var psi = new ProcessStartInfo
        {
            FileName = exe,
            Arguments = args,
            UseShellExecute = options?.Elevated == true, // ShellExecute required for runas
            Verb = options?.Elevated == true ? "runas" : string.Empty
        };

        // For non-elevated relaunches we can pass arguments more reliably
        // without ShellExecute, so keep UseShellExecute false in that case.
        if (options?.Elevated != true)
            psi.UseShellExecute = false;

        Process.Start(psi);

        if (options?.ShutdownCurrent == true)
            Process.GetCurrentProcess().Kill();
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        _disposed = true;

        if (_instance is not null)
        {
            _instance.Activated -= OnRedirectedActivation;

            // Only unregister if we currently own the key.
            if (_instance.IsCurrent)
                _instance.UnregisterKey();
        }
    }

    private void OnRedirectedActivation(object? sender, AppActivationArguments args)
        => FireLaunched(args, isFirstLaunch: false);

    private void FireLaunched(AppActivationArguments args, bool isFirstLaunch)
        => Launched?.Invoke(this, new SingleInstanceLaunchEventArgs(args, isFirstLaunch));

    /// <summary>
    /// Forwards <paramref name="args"/> to <paramref name="target"/> and exits
    /// the current process. Marked async-void intentionally: this is a
    /// fire-and-exit path and we must not block <c>OnLaunched</c>.
    /// </summary>
    private static async void RedirectAndExit(AppInstance target, AppActivationArguments args)
    {
        try
        {
            await target.RedirectActivationToAsync(args);
        }
        finally
        {
            Process.GetCurrentProcess().Kill();
        }
    }

    /// <summary>
    /// Extracts the raw argument string from the current launch activation.
    /// Returns <see cref="string.Empty"/> for non-launch activation kinds —
    /// callers should supply explicit <see cref="InstanceRelaunchOptions.Arguments"/>
    /// when relaunching from a protocol or file activation context.
    /// </summary>
    private static string GetCurrentLaunchArguments()
    {
        var args = AppInstance.GetCurrent().GetActivatedEventArgs();
        return args.Kind == ExtendedActivationKind.Launch
            ? string.Join(" ", System.Environment.GetCommandLineArgs().Skip(1))
            : string.Empty;
    }
}