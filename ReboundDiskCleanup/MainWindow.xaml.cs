using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Foundation.Collections;
using WinUIEx;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace ReboundDiskCleanup
{
    /// <summary>
    /// An empty window that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainWindow : WindowEx
    {
        public MainWindow(string disk)
        {
            this.InitializeComponent();
            this.AppWindow.DefaultTitleBarShouldMatchAppModeTheme = true;
            this.SetWindowSize(375, 235);
            this.IsMaximizable = false;
            this.IsMinimizable = false;
            this.IsResizable = false;
            this.CenterOnScreen();
            this.Title = "Disk Cleanup : Drive Selection";
            this.SystemBackdrop = new MicaBackdrop();
            this.SetIcon($@"{AppContext.BaseDirectory}\Assets\cleanmgr.ico");
            var x = Directory.GetLogicalDrives();
            foreach (var i in x)
            {
                DrivesBox.Items.Add(i.Substring(0, 2));
            }
            DrivesBox.SelectedIndex = 0;
        }

        private async void Button_Click(object sender, RoutedEventArgs e)
        {
            Working.IsIndeterminate = true;
            OkButton.IsEnabled = false;
            CancelButton.IsEnabled = false;
            await Task.Delay(50);

            await OpenWindow(DrivesBox.SelectedItem.ToString());

            this.Close();
        }

        public async Task ArgumentsLaunch(string disk)
        {
            Working.IsIndeterminate = true;
            OkButton.IsEnabled = false;
            CancelButton.IsEnabled = false;
            await Task.Delay(50);

            await OpenWindow(disk);

            this.Close();
        }

        public Task OpenWindow(string disk)
        {
            this.Title = "Disk Cleanup : Calculating total cache size... (This may take a while)";
            var win = new DiskWindow(disk);
            win.AppWindow.DefaultTitleBarShouldMatchAppModeTheme = true;
            win.SetWindowSize(450, 640);
            win.IsMaximizable = false;
            win.IsMinimizable = false;
            win.IsResizable = false;
            win.Move(50, 50);
            win.Title = $"Disk Cleanup for ({disk})";
            win.SystemBackdrop = new MicaBackdrop();
            win.SetIcon($@"{AppContext.BaseDirectory}\Assets\cleanmgr.ico");
            win.Show();
            return Task.CompletedTask;
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            Process.GetCurrentProcess().Kill();
        }
    }
}
