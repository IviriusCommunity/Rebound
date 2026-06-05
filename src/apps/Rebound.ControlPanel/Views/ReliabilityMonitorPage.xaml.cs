// Copyright (C) Ivirius(TM) Community 2020 - 2026. All Rights Reserved.
// Licensed under the MIT License.

using Microsoft.UI.Xaml.Controls;

namespace Rebound.ControlPanel.Views;

internal sealed partial class ReliabilityMonitorPage : Page
{
    private string[] yaxis = ["0", "1", "2", "3", "4", "5", "6", "7", "8", "9", "10"];

    private string[] xaxis = [string.Empty];

    public ReliabilityMonitorPage()
    {
        InitializeComponent();
    }
}
