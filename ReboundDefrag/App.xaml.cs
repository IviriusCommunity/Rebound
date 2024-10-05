using Microsoft.UI.Xaml;
using System;
using System.Linq;

namespace ReboundDefrag
{
    public partial class App : Application
    {
        public App()
        {
            this.InitializeComponent();
        }

        protected override async void OnLaunched(LaunchActivatedEventArgs args)
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
                    (m_window as MainWindow).OptimizeAll(false, true);
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
                    (m_window as MainWindow).OptimizeAll(false, false);
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
