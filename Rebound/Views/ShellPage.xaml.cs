using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Media;

using Rebound.Models;

namespace Rebound.Views;

public sealed partial class ShellPage : Page
{
    private NavMenuItem _selectedMenuItem;

    public ShellPage()
    {
        InitializeComponent();
        
        Debug.WriteLine(@$"{Environment.GetFolderPath(Environment.SpecialFolder.StartMenu)}\Programs\Rebound 11 Tools");

        BuildTopNavigationMenu();
        BuildBottomNavigationMenu();

        MainFrame.Navigate(_selectedMenuItem.TargetType);

        CheckLaunch();
    }

    private void BuildTopNavigationMenu()
    {
        var menuItems = new List<NavMenuItem>();

        menuItems.Add(new NavMenuItem
        {
            Id = "Home",
            NormalIcon = "",
            SelectedIcon = "",
            IconFontFamily = new FontFamily("ms-appx:///Fonts/FluentIcons.ttf#FluentSystemIcons-Resizable"),
            Title = "Home",
            TargetType = typeof(HomePage),
        });

        menuItems.Add(new NavMenuItem
        {
            Id = "Rebound11",
            NormalIcon = "",
            SelectedIcon = "",
            IconFontFamily = new FontFamily("ms-appx:///Fonts/FluentIcons.ttf#FluentSystemIcons-Resizable"),
            Title = "Rebound 11",
            TargetType = typeof(Rebound11Page),
        });

        menuItems.Add(new NavMenuItem
        {
            Id = "IviriusUI",
            NormalIcon = "",
            SelectedIcon = "",
            IconFontFamily = new FontFamily("ms-appx:///Fonts/FluentIcons.ttf#FluentSystemIcons-Resizable"),
            Title = "Ivirius.UI",
            TargetType= null,
        });

        var navigationViewItems = BuildNavigationViewItems(menuItems);

        NavigationViewControl.MenuItemsSource = navigationViewItems;
        NavigationViewControl.SelectedItem = navigationViewItems.First();

        _selectedMenuItem = navigationViewItems.First().DataContext as NavMenuItem;
        _selectedMenuItem.IsSelected = true;
    }

    private void BuildBottomNavigationMenu()
    {
        var menuItems = new List<NavMenuItem>();

        menuItems.Add(new NavMenuItem
        {
            Id = "Docs",
            NormalIcon = "",
            SelectedIcon = "",
            IconFontFamily = new FontFamily("ms-appx:///Fonts/FluentIcons.ttf#FluentSystemIcons-Resizable"),
            Title = "Docs",
        });

        menuItems.Add(new NavMenuItem
        {
            Id = "Discord",
            NormalIcon = "",
            SelectedIcon = "",
            IconFontFamily = new FontFamily("ms-appx:///Fonts/FluentIcons.ttf#FluentSystemIcons-Resizable"),
            Title = "Discord",
        });

        var navigationViewItems = BuildNavigationViewItems(menuItems);

        NavigationViewControl.FooterMenuItemsSource = navigationViewItems;
    }

    private List<NavigationViewItem> BuildNavigationViewItems(List<NavMenuItem> menuItems)
    {
        var navigationViewItems = new List<NavigationViewItem>();

        foreach (var item in menuItems)
        {
            var fontIcon = new FontIcon
            {
                FontFamily = item.IconFontFamily,
            };

            var iconBinding = new Binding()
            {
                Mode = BindingMode.OneWay,
                Source = item,
                Path = new Microsoft.UI.Xaml.PropertyPath("Icon"),
                UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged,
            };

            fontIcon.SetBinding(FontIcon.GlyphProperty, iconBinding);

            var navItem = new NavigationViewItem
            {
                DataContext = item,
                Content = item.Title,
                Icon = fontIcon,
            };

            navigationViewItems.Add(navItem);
        }

        return navigationViewItems;
    }


    public async void CheckLaunch()
    {
        return;

        await Task.Delay(500);
        if (string.Join(" ", Environment.GetCommandLineArgs().Skip(1)).Contains("INSTALLREBOUND11"))
        {
            ////TEMP
            NavigationViewControl.SelectedItem = NavigationViewControl.MenuItems[1];
        }
    }

    private void Navigate(NavigationView sender, NavigationViewItemInvokedEventArgs args)
    {
        var navMenuItem = args.InvokedItemContainer.DataContext as NavMenuItem;

        if (navMenuItem.Id == _selectedMenuItem.Id)
        {
            return;
        }

        //// We need to have target type (target page) for all items. If not, we shouldn't add such items to the nav bar.
        if (navMenuItem.TargetType is null)
        {
            return;
        }

        MainFrame.Navigate(navMenuItem.TargetType);

        _selectedMenuItem.IsSelected = false;

        _selectedMenuItem = navMenuItem;

        _selectedMenuItem.IsSelected = true;
    }
}