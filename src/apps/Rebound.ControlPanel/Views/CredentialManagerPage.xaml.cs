// Copyright (C) Ivirius(TM) Community 2020 - 2026. All Rights Reserved.
// Licensed under the MIT License.

using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using Rebound.ControlPanel.ViewModels;
using WinUIEx;
using static TerraFX.Interop.Windows.Windows;

namespace Rebound.ControlPanel.Views;

internal sealed partial class CredentialManagerPage : Page
{
    public CredentialManagerPage()
    {
        InitializeComponent();

        // Always start fresh so search queries and such don't stick around when navigating back to the page
        CredentialManagerViewModel.Singleton.ReloadState();
    }

    protected override void OnNavigatedFrom(NavigationEventArgs e)
    {
        base.OnNavigatedFrom(e);

        // Since the credentials page may have been censored by authentication when viewing a password, we want to restore screen capture capabilities when leaving the page
        unsafe { SetWindowDisplayAffinity(new((void*)App.MainWindow!.GetWindowHandle()), WDA_NONE); }
    }
}