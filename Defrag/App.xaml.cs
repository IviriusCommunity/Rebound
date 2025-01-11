using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Rebound.Helpers;
using WinUIEx;
using WinUIEx.Messaging;

#nullable enable

namespace Rebound.Defrag;

public partial class App : Application
{
    private readonly SingleInstanceDesktopApp _singleInstanceApp;

    public App()
    {
        this?.InitializeComponent();

        _singleInstanceApp = new SingleInstanceDesktopApp("Rebound.Defrag");
        _singleInstanceApp.Launched += OnSingleInstanceLaunched;
    }

    protected override void OnLaunched(LaunchActivatedEventArgs args)
    {
        MainAppWindow = new MainWindow();
        MainAppWindow.Activate();
    }

    private async void OnSingleInstanceLaunched(object? sender, SingleInstanceLaunchEventArgs e)
    {
        if (e.IsFirstLaunch)
        {
            await LaunchWork();
        }
        else
        {
            if (MainAppWindow != null)
            {
                _ = ((MainWindow)MainAppWindow).BringToFront();
            }
            else
            {
                await LaunchWork();
            }
            return;
        }
    }

    private async Task LaunchWork()
    {
        MainAppWindow = new MainWindow();
        MainAppWindow.Activate();
        mon = new WindowMessageMonitor(MainAppWindow);
        //await ((MainWindow)MainAppWindow).LoadAppAsync();

        var commandArgs = string.Join(" ", Environment.GetCommandLineArgs().Skip(1));

        if (commandArgs.Contains("TASK"))
        {
            try
            {
                ((MainWindow)MainAppWindow).OpenTaskWindow();
            }
            catch (Exception ex)
            {
                await ((MainWindow)MainAppWindow).ShowMessageDialogAsync(ex.Message);
            }
        }
        if (commandArgs.Contains("SELECTED-SYSTEM"))
        {
            try
            {
                // Extract the index after "SELECTED "
                var selectedIndex = int.Parse(commandArgs[(commandArgs.IndexOf("SELECTED-SYSTEM") + 16)..].Trim());
                ((MainWindow)MainAppWindow).MyListView.SelectedIndex = selectedIndex;
                ((MainWindow)MainAppWindow).OptimizeSelected(true);
            }
            catch (Exception ex)
            {
                await ((MainWindow)MainAppWindow).ShowMessageDialogAsync(ex.Message);
            }
        }
        else if (commandArgs.Contains("SELECTED"))
        {
            try
            {
                // Extract the index after "SELECTED "
                var selectedIndex = int.Parse(commandArgs[(commandArgs.IndexOf("SELECTED") + 9)..].Trim());
                ((MainWindow)MainAppWindow).MyListView.SelectedIndex = selectedIndex;
                ((MainWindow)MainAppWindow).OptimizeSelected(false);
            }
            catch (Exception ex)
            {
                await ((MainWindow)MainAppWindow).ShowMessageDialogAsync(ex.Message);
            }
        }
        else if (commandArgs == "OPTIMIZEALL-SYSTEM")
        {
            try
            {
                ((MainWindow)MainAppWindow).OptimizeAll(false, ((MainWindow)MainAppWindow).AdvancedView.IsOn);
            }
            catch (Exception ex)
            {
                await ((MainWindow)MainAppWindow).ShowMessageDialogAsync(ex.Message);
            }
        }
        else if (commandArgs == "OPTIMIZEALL")
        {
            try
            {
                ((MainWindow)MainAppWindow).OptimizeAll(false, ((MainWindow)MainAppWindow).AdvancedView.IsOn);
            }
            catch (Exception ex)
            {
                await ((MainWindow)MainAppWindow).ShowMessageDialogAsync(ex.Message);
            }
        }
        else if (commandArgs == "OPTIMIZEALLANDCLOSE-SYSTEM")
        {
            try
            {
                ((MainWindow)MainAppWindow).OptimizeAll(true, true);
            }
            catch (Exception ex)
            {
                await ((MainWindow)MainAppWindow).ShowMessageDialogAsync(ex.Message);
            }
        }
        else if (commandArgs == "OPTIMIZEALLANDCLOSE")
        {
            try
            {
                ((MainWindow)MainAppWindow).OptimizeAll(true, false);
            }
            catch (Exception ex)
            {
                await ((MainWindow)MainAppWindow).ShowMessageDialogAsync(ex.Message);
            }
        }
    }

    public static WindowEx MainAppWindow { get; set; }

    public static WindowMessageMonitor mon { get; set; }
}
