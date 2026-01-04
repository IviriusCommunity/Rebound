// Copyright (C) Ivirius(TM) Community 2020 - 2025. All Rights Reserved.
// Licensed under the MIT License.

using CommunityToolkit.Mvvm.Input;
using Microsoft.UI.Xaml.Controls;
using System.Threading.Tasks;

namespace Rebound.Installer;

public sealed partial class MainPage : Page
{
    public MainViewModel ViewModel { get; } = new();

    public MainPage()
    {
        InitializeComponent();
    }

    [RelayCommand]
    public void Cancel()
    {
        App._window.Close();
    }

    [RelayCommand]
    public void Begin()
    {
        ViewModel.CurrentPage = "Second";
    }

    [RelayCommand]
    public async Task StartAsync()
    {
        App.canClose = false;
        ViewModel.CurrentPage = "Third";
        await ViewModel.RunActionAsync();
        App.canClose = true;
    }
}