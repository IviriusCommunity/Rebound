// Copyright (C) Ivirius(TM) Community 2020 - 2025. All Rights Reserved.
// Licensed under the MIT License.

using CommunityToolkit.Mvvm.ComponentModel;
using Rebound.Core;
using Rebound.Core.SystemInformation.Software;
using Rebound.Core.UI;

namespace Rebound.Shell.ShutdownDialog
{
    public partial class ShutdownViewModel : ObservableObject
    {
        [ObservableProperty]
        public partial string WindowsVersionTitle { get; set; } = WindowsInformation.GetOSName();

        [ObservableProperty]
        public partial string WindowsVersionName { get; set; } = WindowsInformation.GetOSDisplayName();

        [ObservableProperty]
        public partial bool ShowBlurAndGlow { get; set; }

        private readonly SettingsListener _listener;

        public ShutdownViewModel()
        {
            UpdateSettings();
            _listener = new SettingsListener();
            _listener.SettingChanged += Listener_SettingChanged;
        }


        private void Listener_SettingChanged(object? sender, SettingChangedEventArgs e) => UpdateSettings();

        private void UpdateSettings()
        {
            UIThreadQueue.QueueAction(async () =>
            {
                ShowBlurAndGlow = SettingsManager.GetValue("ShowBlurAndGlow", "rebound", true);
            });
        }
    }
}
