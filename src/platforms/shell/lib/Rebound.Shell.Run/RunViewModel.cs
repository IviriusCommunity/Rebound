using CommunityToolkit.Mvvm.ComponentModel;
using Rebound.Core.Helpers;
using System.Collections.ObjectModel;

namespace Rebound.Shell.Run
{
    public partial class RunViewModel : ObservableObject
    {
        [ObservableProperty]
        public partial string Path { get; set; }

        [ObservableProperty]
        public partial bool RunAsAdmin { get; set; }

        [ObservableProperty]
        public partial bool IsRunButtonEnabled { get; set; }

        public RunViewModel()
        {
            RunAsAdmin = SettingsHelper.GetValue("RunAsAdmin", "rshell.run", false);
        }

        partial void OnRunAsAdminChanged(bool value) => SettingsHelper.SetValue("RunAsAdmin", "rshell.run", value);

        public ObservableCollection<string> RunHistory { get; set; } = new();
    }
}