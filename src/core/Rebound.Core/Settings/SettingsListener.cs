// Copyright (C) Ivirius(TM) Community 2020 - 2026. All Rights Reserved.
// Licensed under the MIT License.

namespace Rebound.Core.Settings;

public sealed class SettingsListener : IDisposable
{
    public event EventHandler<SettingChangedEventArgs>? SettingChanged;

    private readonly string _basePath;
    private readonly Timer _timer;
    private readonly int _pollIntervalMs = 500; // check twice per second
    private FileSystemSnapshot _lastSnapshot;

    public SettingsListener()
    {
        _basePath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
            ".rebound"
        );

        if (!Directory.Exists(_basePath))
            Directory.CreateDirectory(_basePath);

        _lastSnapshot = FileSystemSnapshot.Capture(_basePath);

        _timer = new Timer(OnTimerTick, null, _pollIntervalMs, _pollIntervalMs);
    }

    private void OnTimerTick(object? state)
    {
        try
        {
            var current = FileSystemSnapshot.Capture(_basePath);

            if (!_lastSnapshot.Equals(current))
            {
                _lastSnapshot = current;
                SettingChanged?.Invoke(this, new SettingChangedEventArgs("", ""));
            }
        }
        catch
        {
            // Ignore transient IO issues
        }
    }

    public void Dispose()
    {
        _timer.Dispose();
    }

    // Helper to capture file state
    private class FileSystemSnapshot
    {
        public readonly (string path, DateTime lastWrite)[] Files;

        private FileSystemSnapshot((string, DateTime)[] files)
        {
            Files = files;
        }

        public static FileSystemSnapshot Capture(string folder)
        {
            var files = Directory.GetFiles(folder, "*", SearchOption.AllDirectories)
                                 .Select(f => (f, File.GetLastWriteTimeUtc(f)))
                                 .ToArray();
            return new FileSystemSnapshot(files);
        }

        public override bool Equals(object? obj)
        {
            if (obj is not FileSystemSnapshot other) return false;
            if (Files.Length != other.Files.Length) return false;

            for (int i = 0; i < Files.Length; i++)
            {
                if (Files[i].path != other.Files[i].path || Files[i].lastWrite != other.Files[i].lastWrite)
                    return false;
            }

            return true;
        }

        public override int GetHashCode() => 0; // not used
    }
}

public class SettingChangedEventArgs(string name, string value) : EventArgs
{
    public string Name { get; } = name;
    public string Value { get; } = value;
}