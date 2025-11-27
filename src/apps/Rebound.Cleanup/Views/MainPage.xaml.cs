// Copyright (C) Ivirius(TM) Community 2020 - 2025. All Rights Reserved.
// Licensed under the MIT License.

using System.Threading.Tasks;
using CommunityToolkit.Mvvm.Input;
using Microsoft.UI.Xaml.Controls;
using Rebound.Cleanup.ViewModels;

namespace Rebound.Cleanup.Views;

internal sealed partial class MainPage : Page
{
    private MainViewModel ViewModel { get; } = new MainViewModel();

    public MainPage()
    {
        InitializeComponent();
    }

    [RelayCommand]
    public void Refresh()
    {
        Load();
    }

    [RelayCommand]
    public static void Close() => App.MainAppWindow.Close();

    private async void Load()
    {
        await Task.Delay(500).ConfigureAwait(true);
        await ViewModel.RefreshAsync(ViewModel.ComboBoxItems[ViewModel.SelectedDriveIndex]).ConfigureAwait(true);
    }

    private void ComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        Load();
    }

    private async void CheckBox_Checked(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        await Task.Delay(50).ConfigureAwait(true);

        var selectedItems = 0;
        foreach (var item in ViewModel.CleanItems)
        {
            if (item.IsChecked)
            {
                selectedItems++;
            }
        }
        var totalItems = ViewModel.CleanItems.Count; // Store the count in a variable
        switch (selectedItems)
        {
            case 0:
                ViewModel.IsEverythingSelected = false;
                break;
            case var count when count == totalItems: // Use a pattern matching case
                ViewModel.IsEverythingSelected = true;
                break;
            default:
                ViewModel.IsEverythingSelected = null;
                break;
        }
    }
}