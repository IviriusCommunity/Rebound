// Copyright (C) Ivirius(TM) Community 2020 - 2026. All Rights Reserved.
// Licensed under the MIT License.

using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;
using System.Diagnostics;
using System.Runtime.InteropServices;
using TerraFX.Interop.Windows;
using Windows.UI;
using WinUIEx;
using WinUIEx.Messaging;

namespace Rebound.Core.UI.Composition;

public partial class BlurBackdrop : CompositionBrushBackdrop
{
    private Windows.UI.Composition.CompositionBrush? brush;

    private SolidColorBrush _tint;

    public SolidColorBrush Tint
    {
        get
        {
            return _tint;
        }
        set
        {
            _tint = value;
            if (brush != null)
            {
                //brush.Color = value.Color;
            }
        }
    }

    public BlurBackdrop() : this(Colors.Transparent) { }

    public BlurBackdrop(Color tintColor) => _tint = new SolidColorBrush(tintColor);

    protected override Windows.UI.Composition.CompositionBrush CreateBrush(Windows.UI.Composition.Compositor compositor)
    {
        //var compositor = new Windows.UI.Composition.Compositor();
        return brush = compositor.CreateBackdropBrush(); //compositor.CreateColorBrush(Tint.Color);
    }

    protected override void OnTargetConnected(Microsoft.UI.Composition.ICompositionSupportsSystemBackdrop connectedTarget, XamlRoot xamlRoot)
    {
        base.OnTargetConnected(connectedTarget, xamlRoot);
    }

    protected override void OnTargetDisconnected(Microsoft.UI.Composition.ICompositionSupportsSystemBackdrop disconnectedTarget)
    {
        Windows.UI.Composition.CompositionBrush systemBackdrop = disconnectedTarget.SystemBackdrop;
        disconnectedTarget.SystemBackdrop = null;
        systemBackdrop?.Dispose();
        brush?.Dispose();
        brush = null;
        base.OnTargetDisconnected(disconnectedTarget);
    }
}