// Copyright (C) Ivirius(TM) Community 2020 - 2026. All Rights Reserved.
// Licensed under the MIT License.

using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.WinUI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Imaging;
using Microsoft.UI.Xaml.Navigation;
using Rebound.ControlPanel.ViewModels;
using Rebound.Core.Native.Wrappers;
using Rebound.Core.SystemInformation.Software;
using Rebound.Core.UI.Converters;
using Rebound.Forge.Engines;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using TerraFX.Interop.Windows;
using Windows.System;
using static TerraFX.Interop.Windows.Windows;

#pragma warning disable CA1031 // Do not catch general exception types

namespace Rebound.ControlPanel.Views;

internal sealed partial class RootPage : Page
{
    // The \\\\ is a workaround for this thing: https://github.com/CommunityToolkit/Labs-Windows/issues/788
    // Remove once fixed
    [GeneratedDependencyProperty(DefaultValue = "C:\\\\")] public partial string UserPicturePath { get; set; }

    private RootViewModel ViewModel { get; set; }

    public RootPage()
    {
        // Prepare the ViewModel
        ViewModel = new RootViewModel();

        SizeChanged += (s, e) =>
        {
            // Collapse items depending on window size
            ViewModel.CollapseLeftItems = e.NewSize.Width < 400;
            ViewModel.CollapseRightItems = e.NewSize.Width < 640;
        };
        Loaded += async (s, e) =>
        {
            // Construct navigation items
            BuildNavItems(NavView.MenuItems, CplItemPairs.CplItems);

            // Let the app know that the window is ready
            App.WindowReady = true;

            // Check if Rebound is installed or not
            ViewModel.IsReboundInstalled = ReboundPresenceEngine.IsReboundInstalled();

            // Load profile picture and username
            UserPicturePath = UserInformation.GetUserPicturePath();
            ViewModel.Username = $"Hello, {UserInformation.GetDisplayName()}!";
        };

        // Initialize the page
        InitializeComponent();
    }

    /// <summary>
    /// Construct the navigation view items list from the given source.
    /// </summary>
    /// <param name="target">
    /// The target list to add the navigation items to.
    /// </param>
    /// <param name="source">
    /// The source list of <see cref="CplItem"/> to construct the navigation items from.
    /// </param>
    private static void BuildNavItems(IList<object> target, IEnumerable<CplItem> source)
    {
        foreach (var item in source)
        {
            var navItem = new NavigationViewItem
            {
                Content = item.Name,
                Tag = item,
                IsEnabled = item.IsEnabled,
                SelectsOnInvoked = item.SelectsOnInvoked,
                Icon = (IconElement?)IconStringToIconSourceConverter.ConvertIcon(item.Icon!, typeof(UIElement), null, null)
            };

            if (item.Children.Count > 0)
                BuildNavItems(navItem.MenuItems, item.Children);

            target.Add(navItem);
        }
    }

    /// <summary>
    /// Retrieves a <see cref="NavigationViewItemBase"/> from the navigation view's menu items based on the given tag.
    /// </summary>
    /// <param name="tag">
    /// A tag string that corresponds to the <see cref="CplItem"/> associated with the desired <see cref="NavigationViewItemBase"/>.
    /// </param>
    /// <returns>
    /// The <see cref="NavigationViewItemBase"/> that matches the given tag, or null if no match is found.
    /// </returns>
    private NavigationViewItemBase? GetNavViewItemFromTag(string? tag)
    {
        if (string.IsNullOrEmpty(tag))
            return null;
        return SearchItems(NavView.MenuItems, tag);
    }

    /// <summary>
    /// Recursively searches through a list of navigation view items to find an item with a matching tag.
    /// </summary>
    /// <param name="items">
    /// The list of navigation view items to search through.
    /// </param>
    /// <param name="tag">
    /// The tag string to match against the <see cref="CplItem"/> associated with each navigation view item.
    /// </param>
    /// <returns>
    /// The <see cref="NavigationViewItemBase"/> that matches the given tag, or null if no match is found.
    /// </returns>
    private static NavigationViewItemBase? SearchItems(in IList<object> items, string tag)
    {
        foreach (var item in items)
        {
            if (item is NavigationViewItem navItem
                && navItem.Tag is CplItem cplItem)
            {
                if (cplItem.Tag == tag)
                    return navItem;
                var result = SearchItems(navItem.MenuItems, tag);
                if (result != null)
                    return result;
            }
        }
        return null;
    }

    /// <summary>
    /// Handles the ItemInvoked event of the NavigationView control. When a navigation item is invoked,
    /// this method retrieves the associated <see cref="CplItem"/> and invokes its action asynchronously.
    /// </summary>
    private async void NavigationView_ItemInvoked(NavigationView sender, NavigationViewItemInvokedEventArgs args)
    {
        try
        {
            if (args.InvokedItemContainer is not NavigationViewItem navItem)
                return;
            if (navItem.Tag is not CplItem item)
                return;
            await CplItemPairs.InvokeAsync(RootFrame, item).ConfigureAwait(true);
        }
        catch { }
    }

    /// <summary>
    /// Handles the Navigated event of the root frame. This method synchronizes the navigation view's
    /// selected item and address bar text
    /// </summary>
    private async void RootFrame_Navigated(object sender, NavigationEventArgs e)
    {
        // Sync nav item selection to current page
        var item = CplItemPairs.GetFromPage(e.SourcePageType);
        NavView.SelectedItem = item?.Tag != null
            ? GetNavViewItemFromTag(item.Tag)
            : null;

        ViewModel.PageAddress = item?.Name ?? string.Empty;

        // Create legacy launch items
        CreateLegacyLaunchItems(LegacyLaunchDropDownButtonFlyout.Items, item?.LegacyLaunchItems ?? []);
        CreateLegacyLaunchItems(LegacyLaunchFlyoutItem.Items, item?.LegacyLaunchItems ?? []);

        // Create docs items
        CreateDocsItems(DocsDropDownButtonFlyout.Items, item?.DocsItems ?? []);
        CreateDocsItems(DocsFlyoutItem.Items, item?.DocsItems ?? []);

        ViewModel.CanGoBack = RootFrame.CanGoBack;
        ViewModel.CanGoForward = RootFrame.CanGoForward;
    }

    private static void CreateLegacyLaunchItems(in IList<MenuFlyoutItemBase> flyoutItems, Collection<CplLegacyLaunchItem> legacyLaunchItems)
    {
        flyoutItems.Clear();
        if (legacyLaunchItems.Count > 0)
        {
            foreach (var legacyLaunchItem in legacyLaunchItems)
            {
                var menuItem = new MenuFlyoutItem
                {
                    Text = legacyLaunchItem.Name,
                    Command = new RelayCommand(async () =>
                    {
                        try
                        {
                            if (Application.Current is App app)
                                app.LaunchLegacy(legacyLaunchItem.Executable, legacyLaunchItem.Path);
                        }
                        catch { }
                    })
                };
                flyoutItems.Add(menuItem);
            }
        }
        else
        {
            flyoutItems.Add(new MenuFlyoutItem()
            {
                Text = "No legacy launch options available",
                IsEnabled = false,
            });
        }
    }

    private static void CreateDocsItems(in IList<MenuFlyoutItemBase> flyoutItems, Collection<CplDocsItem> docsItems)
    {
        flyoutItems.Clear();
        if (docsItems.Count > 0)
        {
            foreach (var docsItem in docsItems)
            {
                var menuItem = new MenuFlyoutItem
                {
                    Text = docsItem.Name,
                    Command = new RelayCommand(async () =>
                    {
                        try
                        {
                            await Launcher.LaunchUriAsync(new Uri(docsItem.Link));
                        }
                        catch { }
                    })
                };
                flyoutItems.Add(menuItem);
            }
        }
        else
        {
            flyoutItems.Add(new MenuFlyoutItem()
            {
                Text = "No documentation available",
                IsEnabled = false,
            });
        }
    }

    [RelayCommand]
    public static async Task OpenSettingsAsync()
        => await Launcher.LaunchUriAsync(new Uri("ms-settings:"));

    [RelayCommand]
    public static async Task ManageUserAsync()
        => await Launcher.LaunchUriAsync(new Uri("ms-settings:accounts"));

    [RelayCommand]
    public static async Task OpenUserFolderAsync()
        => await Launcher.LaunchFolderPathAsync(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile));

    [RelayCommand]
    public static void LogOut()
        => ExitWindowsEx(EWX.EWX_LOGOFF, 0);

    [RelayCommand]
    public async Task CreateShortcutAsync()
    {
        // Obtain the current page's item
        var item = CplItemPairs.GetFromPage(RootFrame.SourcePageType);
        if (item != null && item.PageOpenUri != null)
        {
            var contentDialog = new ContentDialog
            {
                Title = "Create Shortcut",
                PrimaryButtonText = "Add",
                CloseButtonText = "Cancel",
                DefaultButton = ContentDialogButton.Primary,
                XamlRoot = XamlRoot
            };

            var sp1 = new StackPanel()
            {
                Orientation = Orientation.Horizontal,
                Spacing = 16,
                Margin = new(0, 8, 0, 0)
            };
            sp1.Children.Add(new Image()
            {
                Source = new BitmapImage(new(Path.Combine(Path.GetDirectoryName(Environment.ProcessPath)!, item.PageOpenIconPath!))),
                Width = 48,
                Height = 48
            });

            var sp2 = new StackPanel() { VerticalAlignment = VerticalAlignment.Center };
            sp2.Children.Add(new TextBlock()
            {
                Text = item.Name,
                FontSize = 16,
                FontWeight = Microsoft.UI.Text.FontWeights.SemiBold
            });
            sp2.Children.Add(new TextBlock()
            {
                Text = "Location: Desktop",
                FontSize = 12
            });

            sp1.Children.Add(sp2);
            contentDialog.Content = sp1;

            var result = await contentDialog.ShowAsync();
            if (result == ContentDialogResult.Primary)
                CreateShortcutNative(item);
        }
    }

    [RelayCommand]
    public void Refresh()
    {
        try
        {
            // Save the previous stack
            IList<PageStackEntry>? previousBackStack = [];
            IList<PageStackEntry>? previousForwardStack = [];
            RootFrame.BackStack.ToList().ForEach(i => previousBackStack.Add(i));
            RootFrame.ForwardStack.ToList().ForEach(i => previousForwardStack.Add(i));

            RootFrame.Navigate(RootFrame.CurrentSourcePageType);

            // Restore the previous stack
            RootFrame.BackStack.Clear();
            RootFrame.ForwardStack.Clear();
            previousBackStack.ToList().ForEach(i => RootFrame.BackStack.Add(i));
            previousForwardStack.ToList().ForEach(i => RootFrame.ForwardStack.Add(i));

            // Update the navigation state
            ViewModel.CanGoBack = RootFrame.CanGoBack;
            ViewModel.CanGoForward = RootFrame.CanGoForward;
        }
        catch { }
    }

    [RelayCommand]
    public void GoBack()
    {
        try
        {
            if (RootFrame.CanGoBack)
                RootFrame.GoBack();

            // Update the navigation state
            ViewModel.CanGoBack = RootFrame.CanGoBack;
            ViewModel.CanGoForward = RootFrame.CanGoForward;
        }
        catch { }
    }

    [RelayCommand]
    public void GoForward()
    {
        try
        {
            if (RootFrame.CanGoForward)
                RootFrame.GoForward();

            // Update the navigation state
            ViewModel.CanGoBack = RootFrame.CanGoBack;
            ViewModel.CanGoForward = RootFrame.CanGoForward;
        }
        catch { }
    }

    [RelayCommand]
    public void GoHome()
    {
        try
        {
            RootFrame.Navigate(typeof(HomePage));

            // Update the navigation state
            ViewModel.CanGoBack = RootFrame.CanGoBack;
            ViewModel.CanGoForward = RootFrame.CanGoForward;
        }
        catch { }
    }

    [RelayCommand]
    public void TogglePane()
        => NavView.IsPaneOpen = !NavView.IsPaneOpen;

    private unsafe void CreateShortcutNative(CplItem item)
    {
        // Get the actual Desktop folder path
        using NativeString desktopPath = NativeString.Alloc(Environment.GetFolderPath(Environment.SpecialFolder.Desktop));
        using NativeString shortcutPath = NativeString.Alloc(Path.Combine(desktopPath.ToManagedString(), $"{item.Name} - Control Panel.lnk"));

        using NativeValue<Guid> clsidShellLink = NativeValue<Guid>.Alloc(CLSID.CLSID_ShellLink);
        using NativeValue<Guid> iidShellLink = NativeValue<Guid>.Alloc(IID.IID_IShellLinkW);
        using NativeValue<Guid> iidPersistFile = NativeValue<Guid>.Alloc(IID.IID_IPersistFile);

        // Create an instance of the ShellLink component
        using ComPtr<IShellLinkW> shellLink = default;
        HRESULT hr = CoCreateInstance(
            clsidShellLink,
            null,
            (uint)CLSCTX.CLSCTX_INPROC_SERVER,
            iidShellLink,
            (void**)shellLink.GetAddressOf());

        if (hr.FAILED)
            throw new Win32Exception($"Failed to create IShellLink instance. HRESULT: {hr}");

        // Set the path to the application/executable the shortcut launches
        using NativeString target = NativeString.Alloc(item.PageOpenUri);
        shellLink.Get()->SetPath(target.CharPointer);

        // Set the shortcut's description
        using NativeString description = NativeString.Alloc($"{item.Name} - Control Panel Shortcut");
        shellLink.Get()->SetDescription(description.CharPointer);

        // Set the custom icon
        using NativeString icon = NativeString.Alloc(Path.Combine(Path.GetDirectoryName(Environment.ProcessPath)!, item.PageOpenIconPath!));
        shellLink.Get()->SetIconLocation(icon.CharPointer, 0);

        // Query for IPersistFile to save the shortcut to disk
        using ComPtr<IPersistFile> persistFile = default;
        shellLink.Get()->QueryInterface(iidPersistFile, (void**)persistFile.GetAddressOf());

        // Save the shortcut (.lnk) file
        persistFile.Get()->Save(shortcutPath.CharPointer, true);
    }
}