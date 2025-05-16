using CommunityToolkit.Mvvm.ComponentModel;

namespace Rebound.Shell.Run
{
    public partial class RunViewModel : ObservableObject
    {
        [ObservableProperty]
        public partial string Path { get; set; }

        [ObservableProperty]
        public partial bool RunAsAdmin { get; set; }
    }
}