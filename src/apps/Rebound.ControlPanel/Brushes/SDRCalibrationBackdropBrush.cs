// Copyright (C) Ivirius(TM) Community 2020 - 2026. All Rights Reserved.
// Licensed under the MIT License.

using Microsoft.Graphics.Canvas.Effects;
using Rebound.Core.ICC.Display;
using Rebound.Core.UI;
using Rebound.Core.UI.Threading;
using System;
using System.Threading;
using System.Threading.Tasks;
using Windows.UI.Composition;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Media;

namespace Rebound.ControlPanel.Brushes;

internal partial class SDRCalibrationBackdropBrush : XamlCompositionBrushBase, IDisposable
{
    private CompositionEffectBrush? _brush;

    // Baseline (what's currently applied to the display)
    private double _baselineGamma = 1.0;
    private double _baselineBrightness;
    private double _baselineContrast = 1.0;

    // Pending user calibration values
    private volatile float _pendingGamma = 1.0F;
    private volatile float _pendingBrightness;
    private volatile float _pendingContrast = 1.0F;
    private volatile bool _hasPendingUpdate;

    private readonly PeriodicTimer _timer = new(TimeSpan.FromSeconds(0.3));
    private CancellationTokenSource _cts = new();

    // Current user values
    private double _gamma = 1.0;
    private double _brightness;
    private double _contrast = 1.0;

    protected override void OnConnected()
    {
        // Read baseline values directly from current profile
        var profilePath = DisplayProfile.GetCurrentProfilePath();
        if (profilePath != null)
        {
            var calibration = DisplayProfile.ReadCalibrationValues(profilePath);
            if (calibration.HasValue)
            {
                _baselineGamma = calibration.Value.gamma;
                _baselineBrightness = calibration.Value.brightness;
                _baselineContrast = calibration.Value.contrast;
            }
        }

        BuildBrush();
        StartPolling();
    }

    protected override void OnDisconnected()
        => Dispose();

    public void UpdateCalibration(double gamma, double brightness, double contrast)
    {
        _pendingGamma = (float)gamma;
        _pendingBrightness = (float)brightness;
        _pendingContrast = (float)contrast;
        _hasPendingUpdate = true;
    }

    private void StartPolling()
    {
        _cts = new CancellationTokenSource();
        Task.Run(async () =>
        {
            while (await _timer.WaitForNextTickAsync(_cts.Token).ConfigureAwait(false))
            {
                if (!_hasPendingUpdate) continue;

                _gamma = _pendingGamma;
                _brightness = _pendingBrightness;
                _contrast = _pendingContrast;
                _hasPendingUpdate = false;

                UIThread.QueueAction(BuildBrush);
            }
        }, _cts.Token);
    }

    private void BuildBrush()
    {
        // Net gamma: cancel baseline gamma, apply user gamma
        var gammaExponent = (float)(_baselineGamma / _gamma);

        // Net contrast: cancel baseline contrast, apply user contrast
        var netContrast = (float)(_contrast / _baselineContrast);

        // Net brightness: cancel baseline brightness, apply user brightness  
        var netBrightness = (float)(_brightness - _baselineBrightness);

        var gammaEffect = new GammaTransferEffect
        {
            Name = "Gamma",
            RedExponent = gammaExponent,
            GreenExponent = gammaExponent,
            BlueExponent = gammaExponent,
            RedAmplitude = 1f,
            GreenAmplitude = 1f,
            BlueAmplitude = 1f,
            RedOffset = 0f,
            GreenOffset = 0f,
            BlueOffset = 0f,
            Source = new CompositionEffectSourceParameter("backdrop")
        };

        // Fancy composition effect
        using var linearEffect = new LinearTransferEffect
        {
            Name = "Linear",
            RedSlope = netContrast,
            GreenSlope = netContrast,
            BlueSlope = netContrast,
            RedOffset = netBrightness,
            GreenOffset = netBrightness,
            BlueOffset = netBrightness,
            Source = gammaEffect
        };

        var animatableProperties = new[]
        {
            "Gamma.RedExponent",   "Gamma.GreenExponent",   "Gamma.BlueExponent",
            "Linear.RedSlope",     "Linear.GreenSlope",     "Linear.BlueSlope",
            "Linear.RedOffset",    "Linear.GreenOffset",    "Linear.BlueOffset",
        };

        var factory = Window.Current.Compositor?.CreateEffectFactory(linearEffect, animatableProperties);

        _brush?.Dispose();
        _brush = factory?.CreateBrush();
        _brush?.SetSourceParameter("backdrop", Window.Current.Compositor?.CreateBackdropBrush());
        CompositionBrush = _brush;
    }

    public void Dispose()
    {
        _cts.Cancel();
        _cts.Dispose();
        _brush?.Dispose();
        _timer.Dispose();
    }
}