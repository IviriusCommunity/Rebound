using System;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Rebound.Helpers;

#nullable enable

namespace Rebound.Defrag
{
    public partial class App : Application
    {
        private readonly SingleInstanceDesktopApp _singleInstanceApp;

        public App()
        {
            this.InitializeComponent();

            _singleInstanceApp = new SingleInstanceDesktopApp("Rebound.Defrag");
            _singleInstanceApp.Launched += OnSingleInstanceLaunched;
        }

        protected override void OnLaunched(LaunchActivatedEventArgs args)
        {
            _singleInstanceApp?.Launch(args.Arguments);
        }

        private async void OnSingleInstanceLaunched(object? sender, SingleInstanceLaunchEventArgs e)
        {
            if (e.IsFirstLaunch)
            {
                await LaunchWork();
            }
            else
            {
                // Get the current process
                Process currentProcess = Process.GetCurrentProcess();

                // Start a new instance of the application
                if (currentProcess.MainModule != null) Process.Start(currentProcess.MainModule.FileName);

                // Terminate the current process
                currentProcess?.Kill();
                return;
            }
        }

        private async Task LaunchWork()
        {
            m_window = new MainWindow();
            m_window.Activate();
            await (m_window as MainWindow).LoadAppAsync();

            string commandArgs = string.Join(" ", Environment.GetCommandLineArgs().Skip(1));

            if (commandArgs.Contains("TASK"))
            {
                try
                {
                    (m_window as MainWindow).OpenTaskWindow();
                }
                catch (Exception ex)
                {
                    await (m_window as MainWindow).ShowMessageDialogAsync(ex.Message);
                }
            }
            if (commandArgs.Contains("SELECTED-SYSTEM"))
            {
                try
                {
                    // Extract the index after "SELECTED "
                    int selectedIndex = int.Parse(commandArgs[(commandArgs.IndexOf("SELECTED-SYSTEM") + 16)..].Trim());
                    (m_window as MainWindow).MyListView.SelectedIndex = selectedIndex;
                    (m_window as MainWindow).OptimizeSelected(true);
                }
                catch (Exception ex)
                {
                    await (m_window as MainWindow).ShowMessageDialogAsync(ex.Message);
                }
            }
            else if (commandArgs.Contains("SELECTED"))
            {
                try
                {
                    // Extract the index after "SELECTED "
                    int selectedIndex = int.Parse(commandArgs[(commandArgs.IndexOf("SELECTED") + 9)..].Trim());
                    (m_window as MainWindow).MyListView.SelectedIndex = selectedIndex;
                    (m_window as MainWindow).OptimizeSelected(false);
                }
                catch (Exception ex)
                {
                    await (m_window as MainWindow).ShowMessageDialogAsync(ex.Message);
                }
            }
            else if (commandArgs == "OPTIMIZEALL-SYSTEM")
            {
                try
                {
                    (m_window as MainWindow).OptimizeAll(false, (m_window as MainWindow).AdvancedView.IsOn);
                }
                catch (Exception ex)
                {
                    await (m_window as MainWindow).ShowMessageDialogAsync(ex.Message);
                }
            }
            else if (commandArgs == "OPTIMIZEALL")
            {
                try
                {
                    (m_window as MainWindow).OptimizeAll(false, (m_window as MainWindow).AdvancedView.IsOn);
                }
                catch (Exception ex)
                {
                    await (m_window as MainWindow).ShowMessageDialogAsync(ex.Message);
                }
            }
            else if (commandArgs == "OPTIMIZEALLANDCLOSE-SYSTEM")
            {
                try
                {
                    (m_window as MainWindow).OptimizeAll(true, true);
                }
                catch (Exception ex)
                {
                    await (m_window as MainWindow).ShowMessageDialogAsync(ex.Message);
                }
            }
            else if (commandArgs == "OPTIMIZEALLANDCLOSE")
            {
                try
                {
                    (m_window as MainWindow).OptimizeAll(true, false);
                }
                catch (Exception ex)
                {
                    await (m_window as MainWindow).ShowMessageDialogAsync(ex.Message);
                }
            }
        }

        private Window m_window;
    }
}
