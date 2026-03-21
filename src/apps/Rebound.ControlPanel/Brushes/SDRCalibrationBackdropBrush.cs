// Copyright (C) Ivirius(TM) Community 2020 - 2026. All Rights Reserved.
// Licensed under the MIT License.

using Microsoft.Graphics.Canvas.Effects;
using Rebound.Core.ICC.Curves;
using Rebound.Core.ICC.Display;
using Rebound.Core.ICC.Profiles;
using Rebound.Core.UI;
using System;
using System.Collections;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;
using TerraFX.Interop.Windows;
using TerraFX.Interop.WinRT;
using Windows.System;
using Windows.UI.Composition;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Hosting;
using Windows.UI.Xaml.Media;
using WinRT;
using CurveChannel = Rebound.Core.ICC.Curves.CurveChannel;

namespace Rebound.ControlPanel.Brushes;

internal partial class SDRCalibrationBackdropBrush(Compositor compositor) : XamlCompositionBrushBase
{
    private Compositor _compositor = compositor;

    private CompositionEffectBrush? _brush;
    private Core.ICC.Curves.CurveChannel? _inverseBaseline;

    // Current user calibration
    private volatile float _pendingGamma = 1.0F;
    private volatile float _pendingBrightness = 0.0F;
    private volatile float _pendingContrast = 1.0F;
    private volatile bool _hasPendingUpdate = false;
    private readonly PeriodicTimer _timer = new(TimeSpan.FromSeconds(0.3));
    private CancellationTokenSource _cts = new();
    private double _gamma = 1.0;      // exponent of 1.0 = no gamma change
    private double _brightness = 0.0;
    private double _contrast = 1.0;

    protected override void OnConnected()
    {
        // Read and invert current ICC profile as baseline
        var profilePath = DisplayProfile.GetCurrentProfilePath(); 
        var existing = profilePath != null
    ? DisplayProfile.ReadCalibration(profilePath)
    : null;

        _inverseBaseline = existing != null
            ? new CurveChannel(
                existing.Red.Invert(),
                existing.Green.Invert(),
                existing.Blue.Invert())
            : null;

        BuildBrush();
        StartPolling();
    }

    protected override void OnDisconnected()
    {
        _cts.Cancel();
        _timer.Dispose();
        _brush?.Dispose();
        _brush = null;
        CompositionBrush = null;
    }

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
            while (await _timer.WaitForNextTickAsync(_cts.Token))
            {
                if (!_hasPendingUpdate) continue;

                _gamma = _pendingGamma;
                _brightness = _pendingBrightness;
                _contrast = _pendingContrast;
                _hasPendingUpdate = false;

                // Marshal back to UI thread
                UIThreadQueue.QueueAction(BuildBrush);
            }
        }, _cts.Token);
    }

    private void BuildBrush()
    {
        var baselineExponents = FitBaselineExponents();

        var gammaEffect = new GammaTransferEffect
        {
            Name = "Gamma",
            // Combine inverse baseline exponent with user gamma
            RedExponent = baselineExponents.R * (float)(1.0 / _gamma),
            GreenExponent = baselineExponents.G * (float)(1.0 / _gamma),
            BlueExponent = baselineExponents.B * (float)(1.0 / _gamma),
            RedAmplitude = 1f,
            GreenAmplitude = 1f,
            BlueAmplitude = 1f,
            RedOffset = 0f,
            GreenOffset = 0f,
            BlueOffset = 0f,
            Source = new CompositionEffectSourceParameter("backdrop")
        };

        var linearEffect = new LinearTransferEffect
        {
            Name = "Linear",
            RedSlope = (float)_contrast,
            GreenSlope = (float)_contrast,
            BlueSlope = (float)_contrast,
            RedOffset = (float)_brightness,
            GreenOffset = (float)_brightness,
            BlueOffset = (float)_brightness,
            Source = gammaEffect
        };

        var animatableProperties = new string[]
        {
    "Gamma.RedExponent",
    "Gamma.GreenExponent",
    "Gamma.BlueExponent",
    "Linear.RedSlope",
    "Linear.GreenSlope",
    "Linear.BlueSlope",
    "Linear.RedOffset",
    "Linear.GreenOffset",
    "Linear.BlueOffset",
        };

        var factory = _compositor.CreateEffectFactory(linearEffect, animatableProperties);


        _brush?.Dispose();
        _brush = factory.CreateBrush();
        _brush.SetSourceParameter("backdrop", _compositor.CreateBackdropBrush());
        CompositionBrush = _brush;
    }

    private (float R, float G, float B) FitBaselineExponents()
    {
        if (_inverseBaseline == null)
            return (1f, 1f, 1f);

        return (
            1.0f / FitExponent(_inverseBaseline.Red),
            1.0f / FitExponent(_inverseBaseline.Green),
            1.0f / FitExponent(_inverseBaseline.Blue)
        );
    }

    private static float FitExponent(GammaCurve curve)
    {
        var samples = new[] { 64, 128, 192 };
        var sum = 0.0;
        var count = 0;

        foreach (var s in samples)
        {
            var normalized = curve.Values[s] / 65535.0;
            if (normalized is <= 0 or >= 1) continue;
            var input = s / 255.0;
            if (input is <= 0 or >= 1) continue;
            sum += Math.Log(normalized) / Math.Log(input);
            count++;
        }

        var result = count == 0 ? 1f : (float)(sum / count);

        return result;
    }

    private float[] CombineCurves(GammaCurve? inverse, double gamma, double brightness, double contrast)
    {
        var userCurve = new GammaCurve(gamma, brightness, contrast);
        var result = new float[GammaCurve.EntryCount];

        for (var i = 0; i < GammaCurve.EntryCount; i++)
        {
            // Start with inverse baseline if available
            var normalized = inverse != null
                ? inverse.Values[i] / 65535.0
                : i / 255.0;

            // Map through user curve using the inverse as input index
            var index = (int)(normalized * (GammaCurve.EntryCount - 1));
            index = Math.Clamp(index, 0, GammaCurve.EntryCount - 1);

            result[i] = userCurve.Values[index] / 65535f;
        }

        return result;
    }
}