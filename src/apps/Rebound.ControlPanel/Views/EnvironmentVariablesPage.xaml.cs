using CommunityToolkit.Mvvm.Input;
using Microsoft.UI.Windowing;
using Rebound.ControlPanel.ViewModels;
using Rebound.Core.UI.Windowing;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading;
using System.Threading.Tasks;
using TerraFX.Interop.Windows;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using Windows.Win32;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=234238

namespace Rebound.ControlPanel.Views
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class EnvironmentVariablesPage : Page
    {
        private EnvironmentVariablesViewModel ViewModel { get; } = new();

        public EnvironmentVariablesPage()
        {
            InitializeComponent();
        }

        [RelayCommand]
        public async Task CreateUserEnvVar()
        {
            // Yes constructing the XAML tree manually in the big 26
            var sp = new StackPanel()
            {
                Spacing = 16
            };
            var variableTextBox = new TextBox()
            {
                Header = "Variable",
                TextWrapping = TextWrapping.Wrap
            };
            sp.Children.Add(variableTextBox);
            var valueTextBox = new TextBox()
            {
                Header = "Value",
                TextWrapping = TextWrapping.Wrap
            };
            sp.Children.Add(valueTextBox);
            var dialog = new IslandsWindow()
            {
                IsMaximizable = false,
                IsMinimizable = false,
                Width = 500,
                Height = 500
            };
            dialog.AppWindowInitialized += (s, e) =>
            {
                // Set owner
                TerraFX.Interop.Windows.Windows.SetWindowLongPtrW(
                    dialog.Handle,
                    GWLP.GWLP_HWNDPARENT,
                    App.MainWindow!.Handle);

                TerraFX.Interop.Windows.Windows.EnableWindow(App.MainWindow.Handle, false);
            };
            dialog.XamlInitialized += (s, e) => {
                dialog.Content = sp;
            };
            dialog.OnClosed += (s, e) =>
            {
                TerraFX.Interop.Windows.Windows.EnableWindow(App.MainWindow.Handle, true);
            };
            dialog.Create();
            dialog.CenterWindow();
        }
    }
}
