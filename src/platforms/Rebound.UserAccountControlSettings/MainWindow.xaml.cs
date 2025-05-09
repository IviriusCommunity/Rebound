using System;
using Rebound.Helpers.Windowing;
using Rebound.UserAccountControlSettings.Views;
using WinUIEx;

namespace Rebound.UserAccountControlSettings;

public sealed partial class MainWindow : WindowEx
{
    public MainWindow()
    {
        InitializeComponent();
        ExtendsContentIntoTitleBar = true;
        RootFrame.Navigate(typeof(MainPage));
        this.SetWindowIcon($"{AppContext.BaseDirectory}\\Assets\\Admin.ico");
        this.CenterOnScreen();
    }
}