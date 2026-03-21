// Copyright (C) Ivirius(TM) Community 2020 - 2026. All Rights Reserved.
// Licensed under the MIT License.

namespace Rebound.Core.ICC.Curves;

/// <summary>
/// Represents a single gamma correction curve for one color channel.
/// </summary>
public class GammaCurve
{
    /// <summary>
    /// Number of entries in the curve. Standard ICC uses 256.
    /// </summary>
    public const int EntryCount = 256;

    /// <summary>
    /// The raw 16-bit curve values.
    /// </summary>
#pragma warning disable CA1819 // Properties should not return arrays
    public ushort[] Values { get; }
#pragma warning restore CA1819 // Properties should not return arrays

    /// <summary>
    /// Generates a gamma curve from calibration parameters.
    /// </summary>
    /// <param name="gamma">Gamma exponent. 1.0 = linear, 2.2 = standard display.</param>
    /// <param name="brightness">Brightness offset. 0.0 = no change.</param>
    /// <param name="contrast">Contrast multiplier. 1.0 = no change.</param>
    public GammaCurve(double gamma = 2.2, double brightness = 0.0, double contrast = 1.0)
    {
        Values = new ushort[EntryCount];
        for (var i = 0; i < EntryCount; i++)
        {
            var normalized = i / 255.0;
            var adjusted = Math.Pow(normalized, 1.0 / gamma);
            adjusted = (adjusted * contrast) + brightness;
            Values[i] = (ushort)(Math.Clamp(adjusted, 0.0, 1.0) * 65535);
        }
    }

    /// <summary>
    /// Creates a gamma curve from raw 16-bit values.
    /// </summary>
    public GammaCurve(ushort[] values)
    {
        ArgumentOutOfRangeException.ThrowIfNotEqual(values?.Length, EntryCount);
        Values = values!;
    }

    public static GammaCurve Forward(double gamma)
    {
        var values = new ushort[EntryCount];
        for (var i = 0; i < EntryCount; i++)
        {
            var normalized = i / 255.0;
            var v = Math.Pow(normalized, gamma);
            values[i] = (ushort)(Math.Clamp(v, 0.0, 1.0) * 65535);
        }
        return new GammaCurve(values);
    }

    /// <summary>
    /// Computes the inverse of this gamma curve.
    /// Used to cancel out an existing calibration before applying a new one.
    /// </summary>
    public GammaCurve Invert()
    {
        var inverted = new ushort[EntryCount];

        for (var i = 0; i < EntryCount; i++)
        {
            // Find where this output value maps back to as an input
            var target = (ushort)(i * 65535 / (EntryCount - 1));

            // Binary search through the curve values for closest match
            var lo = 0;
            var hi = EntryCount - 1;

            while (lo < hi)
            {
                var mid = (lo + hi) / 2;
                if (Values[mid] < target)
                    lo = mid + 1;
                else
                    hi = mid;
            }

            inverted[i] = (ushort)(lo * 65535 / (EntryCount - 1));
        }

        return new GammaCurve(inverted);
    }
}