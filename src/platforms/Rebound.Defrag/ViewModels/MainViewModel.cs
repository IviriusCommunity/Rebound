using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using Rebound.Defrag.Controls;
using Rebound.Defrag.Helpers;
using Rebound.Helpers;

namespace Rebound.Defrag.ViewModels;
public partial class MainViewModel : ObservableObject
{
    [ObservableProperty]
    public partial bool ShowAdvanced { get; set; } = false;

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
        ShowAdvanced = SettingsHelper.GetValue<bool?>("ViewAdvanced", "dfrgui") != null && SettingsHelper.GetValue<bool>("ViewAdvanced", "dfrgui");
        DriveItems = DriveHelper.GetDriveItems(ShowAdvanced);
    }

    public async void ReloadListItems()
    {
        IsOptimizeEnabled = false;
        IsStopEnabled = false;
        AreItemsEnabled = false;
        IsLoading = true;
        await Task.Delay(75);
        DriveItems = DriveHelper.GetDriveItems(ShowAdvanced);
        AreItemsEnabled = true;
        IsLoading = false;
        CheckA();
    }

    public async void CheckA()
    {
        await Task.Delay(10); // Small delay to allow UI updates

        if (DriveItems is null)
        {
            IsOptimizeEnabled = false;
            IsStopEnabled = false;
            return;
        }

        var canOptimize = true;
        var canStop = true;
        var selectedItems = 0;

        foreach (var item in DriveItems)
        {
            if (item.IsChecked)
            {
                selectedItems++;
                if (!item.CanBeOptimized || item.PowerShellProcess != null)
                {
                    canOptimize = false;
                }

                if (!item.CanBeOptimized || item.PowerShellProcess == null)
                {
                    canStop = false;
                }
            }
        }

        if (selectedItems == 0)
        {
            canOptimize = false;
            canStop = false;
        }

        IsOptimizeEnabled = canOptimize;
        IsStopEnabled = canStop;
    }

    partial void OnShowAdvancedChanged(bool oldValue, bool newValue)
    {
        SettingsHelper.SetValue("ViewAdvanced", "dfrgui", newValue);
        ReloadListItems();
    }

    public ObservableCollection<DriveListViewItem> DriveItems { get; set; } = [];
}
