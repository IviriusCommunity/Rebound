// Copyright (C) Ivirius(TM) Community 2020 - 2026. All Rights Reserved.
// Licensed under the MIT License.

namespace Rebound.Core.ICC.Curves;

/// <summary>
/// Represents the three gamma correction curves for a display's RGB channels.
/// </summary>
public class CurveChannel
{
    /// <summary>
    /// The red channel gamma curve.
    /// </summary>
    public GammaCurve Red { get; }

    /// <summary>
    /// The green channel gamma curve.
    /// </summary>
    public GammaCurve Green { get; }

    /// <summary>
    /// The blue channel gamma curve.
    /// </summary>
    public GammaCurve Blue { get; }

    /// <summary>
    /// Creates a uniform RGB curve where all channels share the same parameters.
    /// </summary>
    public CurveChannel(double gamma = 2.2, double brightness = 0.0, double contrast = 1.0)
    {
        Red = new GammaCurve(gamma, brightness, contrast);
        Green = new GammaCurve(gamma, brightness, contrast);
        Blue = new GammaCurve(gamma, brightness, contrast);
    }

    /// <summary>
    /// Creates an RGB curve with independent parameters per channel.
    /// </summary>
    public CurveChannel(GammaCurve red, GammaCurve green, GammaCurve blue)
    {
        Red = red;
        Green = green;
        Blue = blue;
    }
}