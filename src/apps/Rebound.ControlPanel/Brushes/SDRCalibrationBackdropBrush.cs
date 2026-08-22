// Copyright (C) Ivirius(TM) Community 2020 - 2026. All Rights Reserved.
// Licensed under the MIT License.

using CommunityToolkit.WinUI;
using Microsoft.Graphics.Canvas.Effects;
using Microsoft.UI.Composition;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;
using Rebound.Core.ICC.Display;
using System;
using System.Diagnostics;

namespace Rebound.ControlPanel.Brushes;

/// <summary>
/// A composition backdrop brush that takes the values of the current display color profile 
/// and inverts them to render the display's native colors.
/// </summary>
internal partial class SDRCalibrationBackdropBrush : XamlCompositionBrushBase, IDisposable
{
    private CompositionEffectBrush? _brush;
    private GammaTransferEffect? _gamma;

    // Baseline calibration (queried from current active display profile)
    private double _baselineGamma = 1.0;
    private double _baselineRedGain = 1.0;
    private double _baselineGreenGain = 1.0;
    private double _baselineBlueGain = 1.0;

    // Modified calibration values
    [GeneratedDependencyProperty(DefaultValue = 1.0F)] public partial float Gamma { get; set; }
    [GeneratedDependencyProperty(DefaultValue = 1.0F)] public partial float RedGain { get; set; }
    [GeneratedDependencyProperty(DefaultValue = 1.0F)] public partial float GreenGain { get; set; }
    [GeneratedDependencyProperty(DefaultValue = 1.0F)] public partial float BlueGain { get; set; }

    protected override void OnConnected()
    {
        ReloadCurrentColorProfile(false);
        BuildBrush();
    }

    public void ReloadCurrentColorProfile(bool updateBrush = true)
    {
        // Read baseline parameters from active ICC profile
        var profilePath = DisplayProfile.GetCurrentProfilePath();
        if (profilePath != null)
        {
            var calibration = DisplayProfile.ReadCalibrationValues(profilePath);
            if (calibration.HasValue)
            {
                Debug.WriteLine(calibration.Value.greenGain);

                _baselineGamma = calibration.Value.gamma;
                _baselineRedGain = calibration.Value.redGain;
                _baselineGreenGain = calibration.Value.greenGain;
                _baselineBlueGain = calibration.Value.blueGain;
            }
        }

        if (updateBrush)
            UpdateBrushProperties();
    }

    protected override void OnDisconnected()
        => Dispose();

    partial void OnPropertyChanged(DependencyPropertyChangedEventArgs e) 
        => UpdateBrushProperties();

    partial void OnGammaChanged(float newValue)
        => UpdateBrushProperties();

    partial void OnRedGainChanged(float newValue)
        => UpdateBrushProperties();

    partial void OnGreenGainChanged(float newValue)
        => UpdateBrushProperties();

    partial void OnBlueGainChanged(float newValue)
        => UpdateBrushProperties();

    private void BuildBrush()
    {
        var compositor = CompositionTarget.GetCompositorForCurrentThread();

        // 1. Gain scaling runs FIRST on incoming pixels
        var linearEffect = new LinearTransferEffect
        {
            Name = "Linear",
            RedSlope = 1.0f,
            GreenSlope = 1.0f,
            BlueSlope = 1.0f,
            RedOffset = 0.0f,
            GreenOffset = 0.0f,
            BlueOffset = 0.0f,
            Source = new CompositionEffectSourceParameter("backdrop")
        };

        // 2. Gamma exponent runs SECOND
        _gamma = new GammaTransferEffect
        {
            Name = "Gamma",
            RedExponent = 1.0f,
            GreenExponent = 1.0f,
            BlueExponent = 1.0f,
            RedAmplitude = 1.0f,
            GreenAmplitude = 1.0f,
            BlueAmplitude = 1.0f,
            Source = linearEffect
        };

        var animatableProperties = new[]
        {
            "Gamma.RedExponent", "Gamma.GreenExponent", "Gamma.BlueExponent",
            "Linear.RedSlope",   "Linear.GreenSlope",   "Linear.BlueSlope"
        };

        var factory = compositor.CreateEffectFactory(_gamma, animatableProperties);

        _brush?.Dispose();
        _brush = factory.CreateBrush();
        _brush.SetSourceParameter("backdrop", compositor.CreateBackdropBrush());
        CompositionBrush = _brush;

        UpdateBrushProperties();
    }

    private void UpdateBrushProperties()
    {
        if (_brush == null || _gamma == null) return;

        var gammaOffset = (float)-(_baselineGamma - 1);

        _brush.Properties.InsertScalar("Gamma.RedExponent", gammaOffset + Gamma);
        _brush.Properties.InsertScalar("Gamma.GreenExponent", gammaOffset + Gamma);
        _brush.Properties.InsertScalar("Gamma.BlueExponent", gammaOffset + Gamma);

        var redOffset = (float)-(_baselineRedGain - 1);
        var greenOffset = (float)-(_baselineGreenGain - 1);
        var blueOffset = (float)-(_baselineBlueGain - 1);

        _brush.Properties.InsertScalar("Linear.RedSlope", redOffset + RedGain);
        _brush.Properties.InsertScalar("Linear.GreenSlope", greenOffset + GreenGain);
        _brush.Properties.InsertScalar("Linear.BlueSlope", blueOffset + BlueGain);
    }

    public void Dispose()
    {
        _brush?.Dispose();
        _brush = null;
        _gamma?.Dispose();
        _gamma = null;
    }
}