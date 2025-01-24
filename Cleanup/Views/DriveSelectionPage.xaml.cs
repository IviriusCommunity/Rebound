using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text;
using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using Rebound.Cleanup.Helpers;
using Windows.Foundation;
using Windows.Foundation.Collections;


namespace Rebound.Cleanup.Views;

[ObservableObject]
public sealed partial class DriveSelectionPage : Page
{
    [ObservableProperty]
    public partial List<DriveComboBoxItem> ComboBoxItems { get; set; }

    public DriveSelectionPage()
    {
        this.InitializeComponent();
        ComboBoxItems = DriveHelper.GetDriveItems();
    }
}
