using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Microsoft.UI.Xaml;
using Rebound.ControlPanel.Views;
using Rebound.Forge;
using Rebound.Generators;
using Rebound.Helpers.AppEnvironment;

namespace Rebound.ControlPanel;

[ReboundApp("Rebound.Control", "Legacy Control Panel*legacy*ms-appx:///Assets/ControlPanelLegacy.ico")]
public partial class App : Application
{
    public bool ArgsMatchKnownEntries(List<string> matches, string args)
    {
        return matches.Contains(args, StringComparer.InvariantCultureIgnoreCase);
    }

    private async void OnSingleInstanceLaunched(object? sender, Rebound.Helpers.Services.SingleInstanceLaunchEventArgs e)
    {
        Type? pageToLaunch = null;

        Debug.WriteLine(e.Arguments);

        var controlPanelPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.System), "control.exe");

        if (ArgsMatchKnownEntries(["reboundsettings", "/name Rebound.Settings"], e.Arguments))
        {
            pageToLaunch = typeof(ReboundSettingsPage);
        }
        else if (ArgsMatchKnownEntries([controlPanelPath, "control"], e.Arguments))
        {
            pageToLaunch = typeof(HomePage);
        }
        else if (ArgsMatchKnownEntries([
            $"{Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.System), "intl.cpl")},,/p:date",
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.System), "intl.cpl"), 
            $"{controlPanelPath} {Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.System), "intl.cpl")},,/p:date", 
            $"{controlPanelPath} {Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.System), "intl.cpl")}"], e.Arguments))
        {
            // Placeholder for date and time settings
            pageToLaunch = typeof(WindowsToolsPage);
        }
        else if (ArgsMatchKnownEntries([
            "admintools",
            "/name Microsoft.AdministrativeTools",
            $"{controlPanelPath} admintools",
            $"{controlPanelPath} /name Microsoft.AdministrativeTools"], e.Arguments))
        {
            pageToLaunch = typeof(WindowsToolsPage);
        }
        else if (!string.IsNullOrEmpty(e.Arguments))
        {
            if (!this.IsRunningAsAdmin())
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = Environment.ProcessPath,
                    UseShellExecute = true,
                    Verb = "runas",
                    Arguments = "legacy"
                });
                Current.Exit();
                return;
            }
            await IFEOEngine.PauseIFEOEntryAsync("control.exe").ConfigureAwait(true);
            Process.Start(new ProcessStartInfo
            {
                FileName = "control.exe",
                UseShellExecute = true
            });
            await IFEOEngine.ResumeIFEOEntryAsync("control.exe").ConfigureAwait(true);
            Current.Exit();
            return;
        }

        if (e.IsFirstLaunch)
        {
            MainAppWindow = new MainWindow();
            MainAppWindow.Activate();
        }
        else
        {
            MainAppWindow.BringToFront();
        }
        if (pageToLaunch != null)
        {
            MainAppWindow.DispatcherQueue.TryEnqueue(() =>
            {
                var frame = ((MainAppWindow as MainWindow).RootFrame.Content as RootPage)?.RootFrame;
                if (frame?.Content != pageToLaunch) _ = frame.Navigate(pageToLaunch);
            });
        }
    }
}