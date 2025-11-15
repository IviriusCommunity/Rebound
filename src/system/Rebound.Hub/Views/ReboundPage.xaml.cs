// Copyright (C) Ivirius(TM) Community 2020 - 2025. All Rights Reserved.
// Licensed under the MIT License.

using Rebound.Core;
using Rebound.Core.Helpers;
using Rebound.Forge;
using Rebound.Hub.ViewModels;
using System;
using Windows.UI.Text;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Documents;

namespace Rebound.Hub.Views;

internal class ModToTasksListConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        if (value is Mod mod)
        {
            TextBlock tb = new()
            {
                TextWrapping = TextWrapping.WrapWholeWords,
                Width = 320,
                MaxWidth = 320
            };
            
            tb.Inlines.Add(new Run()
            {
                Text = "This mod does the following:\n",
                FontWeight = FontWeights.SemiBold
            });

            foreach (var cog in mod.Cogs)
            {
                tb.Inlines.Add(new Run()
                {
                    Text = $"\n•   {cog.TaskDescription}",
                });
            }

            return tb;
        }
        else return null;
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
        => throw new NotImplementedException();
}

internal partial class ReboundPage : Page
{
    public ReboundPage()
    {
        InitializeComponent();
    }

    private async void ReboundView_Loaded(object sender, RoutedEventArgs e)
    {
        //App.ReboundService.CheckForUpdates();
    }
}