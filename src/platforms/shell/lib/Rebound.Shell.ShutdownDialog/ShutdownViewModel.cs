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
        public static string CurrentUser
        {
            get
            {
                var fullName = UserInformation.GetDisplayName();
                return fullName;
            }
        }

        [ObservableProperty] public partial bool ShowUserInfo { get; set; }

        [ObservableProperty] public partial bool UseShutdownScreen { get; set; }

        [ObservableProperty] public partial int OperationMode { get; set; }

        [ObservableProperty] public partial int OperationReason { get; set; }

        [ObservableProperty]
        public partial string WindowsVersionTitle { get; set; } = WindowsInformation.GetOSName();

        [ObservableProperty]
        public partial string WindowsVersionName { get; set; } = WindowsInformation.GetOSDisplayName();

        [ObservableProperty]
        public partial bool ShowBlurAndGlow { get; set; }

        public bool IsWindowsServerPanelVisible = WindowsInformation.IsServerShutdownUIEnabled();

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
                ShowUserInfo = SettingsManager.GetValue("ShowUserInfo", "rshutdown", true);
                UseShutdownScreen = SettingsManager.GetValue("UseShutdownScreen", "rshutdown", false);
                ShowBlurAndGlow = SettingsManager.GetValue("ShowBlurAndGlow", "rebound", true);
            });
        }
    }
}
