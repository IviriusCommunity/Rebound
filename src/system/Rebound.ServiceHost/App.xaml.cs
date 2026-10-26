// Copyright (C) Ivirius(TM) Community 2020 - 2026. All Rights Reserved.
// Licensed under the MIT License.

using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Rebound.Core;
using Rebound.Core.DLLInjection;
using Rebound.Core.DLLInjection.Structure;
using Rebound.Core.IPC;
using Rebound.Core.UI.Windowing;
using System;
using System.Collections.Concurrent;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using WinUIEx;

namespace Rebound.ServiceHost;

public partial class App : Application
{
    private static PipeHost? PipeServer;
    private static FileSystemWatcher? DllWatcher;

    private static readonly string CloneDirectory =
        Path.Combine(Path.GetTempPath(), "Rebound", "InjectedDLLs");

    private static readonly ConcurrentDictionary<string, SemaphoreSlim>
        FileLocks = new(StringComparer.OrdinalIgnoreCase);

    // Track active injectors per DLL path to manage them if needed
    private static readonly ConcurrentDictionary<string, DLLInjector>
        ActiveInjectors = new(StringComparer.OrdinalIgnoreCase);

    public App()
    {
        InitializeComponent();

        DLLInjectionAPI.TryEnableSeDebugPrivilege(out _);

        Directory.CreateDirectory(Variables.ReboundInjectedDLLsFolder);
        Directory.CreateDirectory(CloneDirectory);

        PipeServer = new PipeHost(
            "REBOUND_DLL_INJECTION",
            AccessLevel.Everyone);
        PipeServer.MessageReceived += PipeServer_MessageReceived;

        StartDllMonitoring();

        _ = StartPipeServerAsync();

        while (true)
        {
            Thread.Sleep(1000);
        }
    }

    private async void PipeServer_MessageReceived(PipeConnection arg1, string arg2)
    {
        if (arg2.StartsWith("DIALOG:", StringComparison.InvariantCultureIgnoreCase))
        {
            string trimmed = arg2.Substring(7);
            var title = trimmed.Split("\\lnbrk\\")[0];
            var content = trimmed.Split("\\lnbrk\\")[1];

            var dialog = await ReboundDialog.ShowAsync(title, content, null, "Close", ContentDialogButton.None).ConfigureAwait(false);
        }
    }

    private static async Task StartPipeServerAsync()
    {
        try
        {
            await PipeServer!.StartAsync().ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            ReboundLogger.WriteToLog("DLL injection",
                $"Pipe server failed: {ex}",
            LogMessageSeverity.Error);
        }
    }

    private static void StartDllMonitoring()
    {
        ReboundLogger.WriteToLog("DLL injection",
            $"aaaaa",
            LogMessageSeverity.Error);

        DllWatcher = new FileSystemWatcher(Variables.ReboundInjectedDLLsFolder)
        {
            Filter = "*.dll",
            IncludeSubdirectories = false,
            NotifyFilter =
                NotifyFilters.FileName |
                NotifyFilters.LastWrite |
                NotifyFilters.Size,
            EnableRaisingEvents = false
        };

        DllWatcher.Created += OnDllCreatedOrChanged;
        DllWatcher.Changed += OnDllCreatedOrChanged;
        DllWatcher.Renamed += OnDllRenamed;
        DllWatcher.Deleted += OnDllDeleted;
        DllWatcher.Error += OnWatcherError;

        // Reconcile existing files before enabling event notifications.
        foreach (string file in Directory.EnumerateFiles(
                     Variables.ReboundInjectedDLLsFolder, "*.dll"))
        {
            _ = CloneAndInjectDllAsync(file); // Updated to handle cloning + injection
        }

        DllWatcher.EnableRaisingEvents = true;
    }

    private static void OnDllCreatedOrChanged(
        object sender, FileSystemEventArgs e)
    {
        _ = CloneAndInjectDllAsync(e.FullPath);
    }

    private static void OnDllRenamed(
        object sender, RenamedEventArgs e)
    {
        RemoveCloneAndNotify(e.OldFullPath);
        _ = CloneAndInjectDllAsync(e.FullPath);
    }

    private static void OnDllDeleted(
        object sender, FileSystemEventArgs e)
    {
        RemoveCloneAndNotify(e.FullPath);
    }

    private static async Task CloneAndInjectDllAsync(string originalPath)
    {
        string fullPath = Path.GetFullPath(originalPath);

        SemaphoreSlim fileLock = FileLocks.GetOrAdd(
            fullPath, _ => new SemaphoreSlim(1, 1));

        await fileLock.WaitAsync().ConfigureAwait(false);

        try
        {
            if (!File.Exists(fullPath))
                return;

            string fileName = Path.GetFileName(fullPath);
            string clonePath = Path.Combine(CloneDirectory, fileName);

            // Retry briefly while another process finishes writing the file.
            for (int attempt = 0; attempt < 10; attempt++)
            {
                try
                {
                    await Task.Delay(200).ConfigureAwait(false);

                    if (!File.Exists(fullPath))
                        return;

                    using (var source = new FileStream(
                               fullPath, FileMode.Open, FileAccess.Read,
                               FileShare.ReadWrite | FileShare.Delete))
                    using (var destination = new FileStream(
                               clonePath, FileMode.Create, FileAccess.Write,
                               FileShare.None))
                    {
                        await source.CopyToAsync(destination)
                            .ConfigureAwait(false);
                    }

                    // --- INTEGRATED DLL INJECTOR FUNCTION CALL ---
                    // Once safely cloned, start the injection process using the clone path.
                    StartInjectorForDll(clonePath, fullPath + "meta");
                    return;
                }
                catch (IOException) when (attempt < 9)
                {
                    await Task.Delay(300).ConfigureAwait(false);
                }
            }

            ReboundLogger.WriteToLog("DLL injection",
                $"Could not clone DLL: {fullPath}",
            LogMessageSeverity.Error);
        }
        catch (Exception ex)
        {
            ReboundLogger.WriteToLog("DLL injection",
                $"DLL clone failed: {ex}",
            LogMessageSeverity.Error);
        }
        finally
        {
            fileLock.Release();
        }
    }

    /// <summary>
    /// Initializes and starts the DLLInjector for a given cloned DLL path.
    /// </summary>
    private static void StartInjectorForDll(string clonePath, string dllMetaPath)
    {
        // Avoid starting duplicate injectors for the exact same file path
        var injector = ActiveInjectors.GetOrAdd(clonePath, path =>
        {
            ReboundLogger.WriteToLog("DLL injection", dllMetaPath,
            LogMessageSeverity.Error);

            var newInjector = new DLLInjector(path);

            var meta = DllMeta.TryParse(File.ReadAllText(dllMetaPath));
            foreach (var proc in meta.TargetProcesses)
                newInjector.TargetProcesses.Add(proc);
            foreach (var proc in meta.ExcludedProcesses)
                newInjector.ExcludedProcesses.Add(proc);

            ReboundLogger.WriteToLog("DLL injection", string.Join(";", meta.TargetProcesses),
            LogMessageSeverity.Error);
            return newInjector;
        });

        try
        {
            // Call StartInjection from the provided snippet
            injector.StartInjection();
            ReboundLogger.WriteToLog("DLL injection", $"Started injection monitoring for: {clonePath}",
            LogMessageSeverity.Error);
        }
        catch (Exception ex)
        {
            ReboundLogger.WriteToLog("DLL injection",
                $"Injector start failed for { clonePath}: { ex}",
            LogMessageSeverity.Error);
        }
    }

    private static void RemoveCloneAndNotify(string originalPath)
    {
        string fileName = Path.GetFileName(originalPath);
        string clonePath = Path.Combine(CloneDirectory, fileName);

        // Remove from tracking dictionary if present
        if (ActiveInjectors.TryRemove(clonePath, out var injector))
            injector.StopInjection();

        try
        {
            if (File.Exists(clonePath))
                File.Delete(clonePath);
        }
        catch (IOException ex)
        {
            ReboundLogger.WriteToLog("DLL injection",
                $"Could not delete clone {clonePath}: {ex.Message}",
            LogMessageSeverity.Error);
        }
        catch (UnauthorizedAccessException ex)
        {
            ReboundLogger.WriteToLog("DLL injection",
                $"Access denied deleting clone {clonePath}: {ex.Message}",
            LogMessageSeverity.Error);
        }

        try
        {
            PipeServer?.Broadcast($"UNINJECT {fileName}");
        }
        catch (Exception ex)
        {
            ReboundLogger.WriteToLog("DLL injection",
                $"UNINJECT broadcast failed: {ex}",
            LogMessageSeverity.Error);
        }
    }

    private static void OnWatcherError(
        object sender, ErrorEventArgs e)
    {
        ReboundLogger.WriteToLog("DLL injection",
        $"DLL watcher error: {e.GetException()}",
            LogMessageSeverity.Error);
    }

    private void Win_Closed(object sender, WindowEventArgs args)
    {
        PipeServer?.Broadcast(
            "UNINJECT Rebound.Forge.Hooks.Run.dll");
    }
}