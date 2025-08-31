using Microsoft.UI.Windowing;
using Rebound.Core.Helpers;
using Rebound.Generators;
using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using TerraFX.Interop.Windows;
using TerraFX.Interop.WinRT;
using Windows.System;
using Windows.UI.WindowManagement;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Hosting;
using WinRT;
using static TerraFX.Interop.Windows.SWP;
using static TerraFX.Interop.Windows.Windows;
using static TerraFX.Interop.Windows.WM;
using static TerraFX.Interop.Windows.WS;
using Colors = Windows.UI.Colors;

namespace Rebound;

//[ReboundApp("Rebound.Hub", "")]
public partial class App : Application
{
    public App()
    {
        App.Current.UnhandledException += Current_UnhandledException;
        CreateMainWindow();
    }

    private void Current_UnhandledException(object sender, Windows.UI.Xaml.UnhandledExceptionEventArgs e)
    {
        e.Handled = true;
    }

    public static void Activate(string[] args)
    {
        if (MainWindow != null)
        {
            MainWindow.Activate();
        }
        else
        {
            CreateMainWindow();
        }
    }

    public static unsafe void CreateMainWindow()
    {
        MainWindow = new();
        MainWindow.AppWindowInitialized += (s, e) =>
        {
            MainWindow.Title = "Rebound Hub";
            MainWindow.AppWindow?.TitleBar.ExtendsContentIntoTitleBar = true;
            MainWindow.AppWindow?.TitleBar.PreferredHeightOption = TitleBarHeightOption.Tall;
            MainWindow.AppWindow?.TitleBar.ButtonBackgroundColor = Colors.Transparent;
            MainWindow.AppWindow?.TitleBar.ButtonInactiveBackgroundColor = Colors.Transparent;
            MainWindow.AppWindow?.SetTaskbarIcon($"{AppContext.BaseDirectory}\\Assets\\AppIcons\\ReboundHub.ico");
        };
        MainWindow.XamlInitialized += (s, e) =>
        {
            var frame = new Frame();
            frame.Navigate(typeof(Views.ShellPage));
            MainWindow.Content = frame;
        };
        MainWindow.Create();
    }

    public static IslandsWindow MainWindow { get; private set; }
}