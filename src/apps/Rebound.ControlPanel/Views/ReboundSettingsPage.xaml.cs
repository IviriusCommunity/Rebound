using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.Input;
using Microsoft.UI.Xaml.Controls;
using Rebound.ControlPanel.ViewModels;
using Windows.System;

namespace Rebound.ControlPanel.Views;

public sealed partial class ReboundSettingsPage : Page
{
    public ReboundSettingsViewModel ViewModel { get; } = new();

    public ReboundSettingsPage()
    {
        InitializeComponent();
    }

    [RelayCommand]
    public async Task NavigateToWallpapersFolderAsync()
    {
        await Launcher.LaunchFolderPathAsync(Path.Combine(AppContext.BaseDirectory, "Assets", "Backgrounds"));
    }
}
