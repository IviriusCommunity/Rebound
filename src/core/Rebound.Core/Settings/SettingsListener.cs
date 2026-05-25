// Copyright (C) Ivirius(TM) Community 2020 - 2026. All Rights Reserved.
// Licensed under the MIT License.

namespace Rebound.Core.Settings;

public sealed class SettingsListener : IDisposable
{
    public event EventHandler<SettingChangedEventArgs>? SettingChanged;

    private readonly FileSystemWatcher _watcher;

    public SettingsListener()
    {
        var basePath = Variables.ReboundDataFolder;

        if (!Directory.Exists(basePath))
            Directory.CreateDirectory(basePath);

        _watcher = new FileSystemWatcher(basePath)
        {
            NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.FileName | NotifyFilters.DirectoryName,
            Filter = "*",
            IncludeSubdirectories = true,
            EnableRaisingEvents = true,
        };

        _watcher.Changed += OnFileEvent;
        _watcher.Created += OnFileEvent;
        _watcher.Deleted += OnFileEvent;
        _watcher.Renamed += OnFileRenamed;
    }

    private void OnFileEvent(object sender, FileSystemEventArgs e)
        => SettingChanged?.Invoke(this, new SettingChangedEventArgs(e.Name ?? "", e.FullPath));

    private void OnFileRenamed(object sender, RenamedEventArgs e)
        => SettingChanged?.Invoke(this, new SettingChangedEventArgs(e.Name ?? "", e.FullPath));

    public void Dispose()
    {
        _watcher.EnableRaisingEvents = false;
        _watcher.Dispose();
    }
}

public class SettingChangedEventArgs(string name, string value) : EventArgs
{
    public string Name { get; } = name;
    public string Value { get; } = value;
}