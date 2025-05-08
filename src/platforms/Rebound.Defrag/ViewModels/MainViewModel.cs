using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using Rebound.Defrag.Controls;
using Rebound.Defrag.Helpers;
using Rebound.Helpers;

namespace Rebound.Defrag.ViewModels;

internal partial class MainViewModel : ObservableObject
{
    [ObservableProperty]
    public partial bool ShowAdvanced { get; set; }

    [ObservableProperty]
    public partial bool AreItemsEnabled { get; set; } = true;

    [ObservableProperty]
    public partial bool IsLoading { get; set; } = false;

    [ObservableProperty]
    public partial bool IsOptimizeEnabled { get; set; } = true;

    [ObservableProperty]
    public partial bool IsStopEnabled { get; set; } = true;

    public MainViewModel()
    {
        ShowAdvanced = SettingsHelper.GetValue("ViewAdvanced", "dfrgui", false);
        DriveItems = DriveHelper.GetDriveItems(ShowAdvanced);
    }

    public async void ReloadListItems()
    {
        SetLoadingState(true);
        await Task.Delay(75).ConfigureAwait(true);

        var items = DriveHelper.GetDriveItems(ShowAdvanced);
        DriveItems.Clear();

        foreach (var item in items)
        {
            DriveItems.Add(item);
        }

        SetLoadingState(false);
        CheckEnabledActions();
    }

    private void SetLoadingState(bool isLoading)
    {
        AreItemsEnabled = !isLoading;
        IsLoading = isLoading;
        DisableActions();
    }

    public async void CheckEnabledActions()
    {
        await Task.Delay(10).ConfigureAwait(true);

        if (DriveItems is null || DriveItems.Count == 0)
        {
            DisableActions();
            return;
        }

        var selectedItems = DriveItems.Where(item => item.IsChecked).ToList();

        if (selectedItems.Count == 0)
        {
            DisableActions();
            return;
        }

        IsOptimizeEnabled = selectedItems.All(item => item.CanBeOptimized && item.PowerShellProcess == null);
        IsStopEnabled = selectedItems.All(item => item.CanBeOptimized && item.PowerShellProcess != null);
    }

    private void DisableActions()
    {
        IsOptimizeEnabled = false;
        IsStopEnabled = false;
    }

    partial void OnShowAdvancedChanged(bool oldValue, bool newValue)
    {
        SettingsHelper.SetValue("ViewAdvanced", "dfrgui", newValue);
        ReloadListItems();
    }

    public ObservableCollection<DriveListViewItem> DriveItems { get; set; } = [];
}