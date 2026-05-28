// Copyright (C) Ivirius(TM) Community 2020 - 2026. All Rights Reserved.
// Licensed under the MIT License.

using CommunityToolkit.Mvvm.Input;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Rebound.ControlPanel.ViewModels;
using Rebound.ControlPanel.Views.Secondary;
using Rebound.Core.Native.Storage;
using System;
using System.Runtime.InteropServices;
using WinUIEx;

namespace Rebound.ControlPanel.Views;

internal sealed partial class BackupAndRestorePage : Page
{
    private BackupAndRestoreViewModel ViewModel { get; } = new();

    public BackupAndRestorePage()
    {
        InitializeComponent();
    }

    // Called when the page is unloaded so the registry monitor thread exits.
    private void OnUnloaded(object sender, RoutedEventArgs e)
        => ViewModel.StopRegistryMonitor();

    // ── Child window helpers ──────────────────────────────────────────────────

    private static void MakeChildModal(WindowEx child)
    {
        /*// Set owner
        NativeMethods.SetWindowLongPtr(
            child.Handle,
            NativeMethods.GWLP_HWNDPARENT,
            App.MainWindow!.Handle);

        // Set modal
        if (child.AppWindow?.Presenter is OverlappedPresenter op)
            op.IsModal = true;*/
    }

    // ── Create Restore Point ─────────────────────────────────────────────────

    [RelayCommand]
    public void OpenCreateRestorePointWindow()
    {
        /*var child = new IslandsWindow
        {
            Width = 400,
            Height = 260
        };

        child.AppWindowInitialized += (s, e) =>
        {
            MakeChildModal(child);
            child.AppWindow!.TitleBar.ExtendsContentIntoTitleBar = true;
        };

        child.XamlInitialized += (s, e) =>
        {
            child.Title = "Create restore point";

            var page = new CreateRestorePointPage();
            page.Confirmed += async (description, eventType) =>
            {
                child.Close();
                await ViewModel.CreateRestorePointAsync(description, eventType);
            };
            page.Cancelled += () => child.Close();

            var frame = new Frame();
            frame.Content = page;
            child.Content = frame;
        };

        child.Create();*/
    }

    // ── Configure Auto Backup ────────────────────────────────────────────────

    [RelayCommand]
    public void OpenConfigureAutoBackupWindow()
    {
        /*var child = new IslandsWindow
        {
            Width = 440,
            Height = 320
        };

        child.AppWindowInitialized += (s, e) =>
        {
            MakeChildModal(child);
            child.AppWindow!.TitleBar.ExtendsContentIntoTitleBar = true;
        };

        child.XamlInitialized += (s, e) =>
        {
            child.Title = "Configure automatic backup";

            var page = new ConfigureAutoBackupPage();
            page.Closed += () =>
            {
                child.Close();
                // The page wrote to registry directly; reload from there.
                ViewModel.OnAutoBackupConfigured();
            };

            var frame = new Frame();
            frame.Content = page;
            child.Content = frame;
        };

        child.Create();*/
    }

    // ── Restore from file ────────────────────────────────────────────────────

    [RelayCommand]
    public async void RestoreFromFile()
    {
        /*var result = FilePickers.PickOpenFile(
            App.MainWindow!.Handle,
            "Select a backup file",
            [
                new("Backup files", ".vhd;.vhdx;.wbcat"),
                new("All files", "*")
            ]);

        if (result.IsCancelled == true || string.IsNullOrWhiteSpace(result.Path))
            return;

        await ViewModel.RestoreFromFileAsync(result.Path);*/
    }
}

// ── Native interop ────────────────────────────────────────────────────────────

file static class NativeMethods
{
    internal const int GWLP_HWNDPARENT = -8;

    [DllImport("user32.dll")]
    internal static extern IntPtr SetWindowLongPtr(IntPtr hWnd, int nIndex, IntPtr dwNewLong);
}