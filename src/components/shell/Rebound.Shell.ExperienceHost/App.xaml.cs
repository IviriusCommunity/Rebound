using System;
using H.NotifyIcon.Core;
using Microsoft.UI.Xaml;
using Rebound.Generators;
using Rebound.Helpers.Services;
using Rebound.Shell.Desktop;
using Rebound.ShellExperiencePack;
using WinUIEx;

#nullable enable

namespace Rebound.ShellExperienceHost;

[ReboundApp("Rebound.ShellExperienceHost", "Legacy Shell")]
public partial class App : Application
{
    public App()
    {
        InitializeComponent();
    }

    private void OnSingleInstanceLaunched(object? sender, SingleInstanceLaunchEventArgs e)
    {
        if (e.IsFirstLaunch)
        {
            // Tray icon
            using System.Drawing.Icon icon = new($"{AppContext.BaseDirectory}\\Assets\\ReboundIcon.ico");
            var trayIcon = new TrayIcon
            {
                Icon = icon.Handle,
                ToolTip = "Rebound Shell"
            };
            trayIcon.Create();
            trayIcon.Show();

            // Desktop
            DesktopWindow = new DesktopWindow();
            DesktopWindow.Activate();
            DesktopWindow.AttachToProgMan();
        }
    }

    public static WindowEx? RunWindow { get; set; }
    public static WindowEx? DesktopWindow { get; set; }
    public static WindowEx? ShutdownDialog { get; set; }
}