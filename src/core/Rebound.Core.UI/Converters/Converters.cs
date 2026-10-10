// Copyright (C) Ivirius(TM) Community 2020 - 2025. All Rights Reserved.
// Licensed under the MIT License.

using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using System.Globalization;
using Windows.Foundation;

namespace Rebound.Core.UI.Converters;

public partial class IconStringToIconSourceConverter : IValueConverter
{
    private const string GlyphPrefix = "glyph:";
    private const string ImagePrefix = "img:";
    private const string PathPrefix = "path:";

    public static object? ConvertIcon(object value, Type targetType, object? parameter, string? language)
    {
        try
        {
            if (value is not string icon || string.IsNullOrEmpty(icon))
                return null;

            if (icon.StartsWith(GlyphPrefix, StringComparison.InvariantCultureIgnoreCase))
            {
                var glyph = icon[GlyphPrefix.Length..];
                if (string.IsNullOrEmpty(glyph))
                    return null;
                return new FontIcon { Glyph = glyph };
            }

            if (icon.StartsWith(ImagePrefix, StringComparison.InvariantCultureIgnoreCase))
            {
                var path = icon[ImagePrefix.Length..];
                if (string.IsNullOrEmpty(path))
                    return null;
                if (!Uri.TryCreate(path, UriKind.Absolute, out var uri))
                    return null;
                return new ImageIcon { Source = new BitmapImage(uri) };
            }

            if (icon.StartsWith(PathPrefix, StringComparison.InvariantCultureIgnoreCase))
            {
                var path = icon[PathPrefix.Length..];
                if (string.IsNullOrEmpty(path))
                    return null;
                return new PathIcon { Data = PathMarkupToGeometry(path) };
            }

            return null;
        }
        catch
        {
            return null;
        }
    }

    private static PathGeometry? PathMarkupToGeometry(string pathMarkup)
    {
        if (string.IsNullOrWhiteSpace(pathMarkup))
            return null;

        FillRule? rule = pathMarkup.StartsWith("F1 ", StringComparison.InvariantCultureIgnoreCase) ?
            FillRule.Nonzero : pathMarkup.StartsWith("F0 ", StringComparison.InvariantCultureIgnoreCase) ?
            FillRule.EvenOdd : null;
        if (rule == null)
            return null;

        var tokens = TokenizePath(pathMarkup.Substring(3));
        var pathGeometry = new PathGeometry { FillRule = rule.Value };

        PathFigure? currentFigure = null;
        Point currentPoint = new Point(0, 0);
        Point subpathStartPoint = new Point(0, 0);

        Point? lastControlPoint = null;
        Point? lastQuadControlPoint = null;

        char activeCommand = '\0';
        char lastCommand = '\0';
        int tokenIndex = 0;

        while (tokenIndex < tokens.Count)
        {
            var token = tokens[tokenIndex];
            if (token is char cmdChar)
            {
                activeCommand = cmdChar;
                tokenIndex++;
            }
            else if (activeCommand == '\0')
            {
                throw new FormatException("Path data started without a command letter.");
            }

            double NextDouble()
            {
                if (tokenIndex >= tokens.Count || tokens[tokenIndex] is not double d)
                {
                    throw new FormatException($"Expected numeric parameter for command '{activeCommand}' at token index {tokenIndex}.");
                }
                tokenIndex++;
                return d;
            }

            bool isRelative = char.IsLower(activeCommand);
            char upperCmd = char.ToUpperInvariant(activeCommand);

            if (upperCmd is not 'C' and not 'S')
            {
                lastControlPoint = null;
            }
            if (upperCmd is not 'Q' and not 'T')
            {
                lastQuadControlPoint = null;
            }

            switch (upperCmd)
            {
                case 'M':
                    {
                        double x = NextDouble();
                        double y = NextDouble();
                        if (isRelative)
                        {
                            x += currentPoint.X;
                            y += currentPoint.Y;
                        }

                        currentFigure = new PathFigure
                        {
                            StartPoint = new Point(x, y),
                            IsClosed = false
                        };
                        pathGeometry.Figures.Add(currentFigure);

                        currentPoint = new Point(x, y);
                        subpathStartPoint = currentPoint;
                        lastCommand = activeCommand;
                        activeCommand = isRelative ? 'l' : 'L'; // Subsequent coordinates are implicit lineto
                        break;
                    }
                case 'L':
                    {
                        if (currentFigure == null)
                            throw new FormatException("Lineto command without preceding Moveto.");
                        double x = NextDouble();
                        double y = NextDouble();
                        if (isRelative)
                        {
                            x += currentPoint.X;
                            y += currentPoint.Y;
                        }
                        var pt = new Point(x, y);
                        currentFigure.Segments.Add(new LineSegment { Point = pt });
                        currentPoint = pt;
                        lastCommand = activeCommand;
                        break;
                    }
                case 'H':
                    {
                        if (currentFigure == null)
                            throw new FormatException("Horizontal lineto command without preceding Moveto.");
                        double x = NextDouble();
                        if (isRelative)
                        {
                            x += currentPoint.X;
                        }
                        var pt = new Point(x, currentPoint.Y);
                        currentFigure.Segments.Add(new LineSegment { Point = pt });
                        currentPoint = pt;
                        lastCommand = activeCommand;
                        break;
                    }
                case 'V':
                    {
                        if (currentFigure == null)
                            throw new FormatException("Vertical lineto command without preceding Moveto.");
                        double y = NextDouble();
                        if (isRelative)
                        {
                            y += currentPoint.Y;
                        }
                        var pt = new Point(currentPoint.X, y);
                        currentFigure.Segments.Add(new LineSegment { Point = pt });
                        currentPoint = pt;
                        lastCommand = activeCommand;
                        break;
                    }
                case 'Z':
                    {
                        if (currentFigure != null)
                        {
                            currentFigure.IsClosed = true;
                        }
                        currentPoint = subpathStartPoint;
                        lastCommand = activeCommand;
                        break;
                    }
                case 'C':
                    {
                        if (currentFigure == null)
                            throw new FormatException("Cubic curve command without preceding Moveto.");
                        double x1 = NextDouble();
                        double y1 = NextDouble();
                        double x2 = NextDouble();
                        double y2 = NextDouble();
                        double x = NextDouble();
                        double y = NextDouble();

                        if (isRelative)
                        {
                            x1 += currentPoint.X;
                            y1 += currentPoint.Y;
                            x2 += currentPoint.X;
                            y2 += currentPoint.Y;
                            x += currentPoint.X;
                            y += currentPoint.Y;
                        }

                        var cp1 = new Point(x1, y1);
                        var cp2 = new Point(x2, y2);
                        var endPt = new Point(x, y);

                        currentFigure.Segments.Add(new BezierSegment
                        {
                            Point1 = cp1,
                            Point2 = cp2,
                            Point3 = endPt
                        });

                        lastControlPoint = cp2;
                        currentPoint = endPt;
                        lastCommand = activeCommand;
                        break;
                    }
                case 'S':
                    {
                        if (currentFigure == null)
                            throw new FormatException("Smooth cubic curve command without preceding Moveto.");
                        double x2 = NextDouble();
                        double y2 = NextDouble();
                        double x = NextDouble();
                        double y = NextDouble();

                        if (isRelative)
                        {
                            x2 += currentPoint.X;
                            y2 += currentPoint.Y;
                            x += currentPoint.X;
                            y += currentPoint.Y;
                        }

                        Point cp1;
                        char lastUpper = char.ToUpperInvariant(lastCommand);
                        if (lastControlPoint.HasValue && (lastUpper == 'C' || lastUpper == 'S'))
                        {
                            cp1 = new Point(
                                2.0 * currentPoint.X - lastControlPoint.Value.X,
                                2.0 * currentPoint.Y - lastControlPoint.Value.Y
                            );
                        }
                        else
                        {
                            cp1 = currentPoint;
                        }

                        var cp2 = new Point(x2, y2);
                        var endPt = new Point(x, y);

                        currentFigure.Segments.Add(new BezierSegment
                        {
                            Point1 = cp1,
                            Point2 = cp2,
                            Point3 = endPt
                        });

                        lastControlPoint = cp2;
                        currentPoint = endPt;
                        lastCommand = activeCommand;
                        break;
                    }
                case 'Q':
                    {
                        if (currentFigure == null)
                            throw new FormatException("Quadratic curve command without preceding Moveto.");
                        double x1 = NextDouble();
                        double y1 = NextDouble();
                        double x = NextDouble();
                        double y = NextDouble();

                        if (isRelative)
                        {
                            x1 += currentPoint.X;
                            y1 += currentPoint.Y;
                            x += currentPoint.X;
                            y += currentPoint.Y;
                        }

                        var cp1 = new Point(x1, y1);
                        var endPt = new Point(x, y);

                        currentFigure.Segments.Add(new QuadraticBezierSegment
                        {
                            Point1 = cp1,
                            Point2 = endPt
                        });

                        lastQuadControlPoint = cp1;
                        currentPoint = endPt;
                        lastCommand = activeCommand;
                        break;
                    }
                case 'T':
                    {
                        if (currentFigure == null)
                            throw new FormatException("Smooth quadratic curve command without preceding Moveto.");
                        double x = NextDouble();
                        double y = NextDouble();

                        if (isRelative)
                        {
                            x += currentPoint.X;
                            y += currentPoint.Y;
                        }

                        Point cp1;
                        char lastUpper = char.ToUpperInvariant(lastCommand);
                        if (lastQuadControlPoint.HasValue && (lastUpper == 'Q' || lastUpper == 'T'))
                        {
                            cp1 = new Point(
                                2.0 * currentPoint.X - lastQuadControlPoint.Value.X,
                                2.0 * currentPoint.Y - lastQuadControlPoint.Value.Y
                            );
                        }
                        else
                        {
                            cp1 = currentPoint;
                        }

                        var endPt = new Point(x, y);

                        currentFigure.Segments.Add(new QuadraticBezierSegment
                        {
                            Point1 = cp1,
                            Point2 = endPt
                        });

                        lastQuadControlPoint = cp1;
                        currentPoint = endPt;
                        lastCommand = activeCommand;
                        break;
                    }
                case 'A':
                    {
                        if (currentFigure == null)
                            throw new FormatException("Arc command without preceding Moveto.");
                        double rx = NextDouble();
                        double ry = NextDouble();
                        double rot = NextDouble();
                        double largeArcFlag = NextDouble();
                        double sweepFlag = NextDouble();
                        double x = NextDouble();
                        double y = NextDouble();

                        if (isRelative)
                        {
                            x += currentPoint.X;
                            y += currentPoint.Y;
                        }

                        var endPt = new Point(x, y);
                        var segment = CreateArcSegment(currentPoint, endPt, rx, ry, rot, largeArcFlag != 0, sweepFlag != 0);
                        currentFigure.Segments.Add(segment);

                        currentPoint = endPt;
                        lastCommand = activeCommand;
                        break;
                    }
                default:
                    throw new FormatException($"Unsupported or unknown path command: '{activeCommand}'");
            }
        }

        return pathGeometry;
    }

    private static List<object> TokenizePath(string pathMarkup)
    {
        var tokens = new List<object>();
        int i = 0;
        int len = pathMarkup.Length;

        while (i < len)
        {
            char c = pathMarkup[i];

            if (char.IsWhiteSpace(c) || c == ',')
            {
                i++;
                continue;
            }

            if (c is >= 'A' and <= 'Z' or >= 'a' and <= 'z')
            {
                tokens.Add(c);
                i++;
                continue;
            }

            int start = i;
            if (c is '+' or '-')
            {
                i++;
            }

            bool hasDecimal = false;
            bool hasDigits = false;

            if (i < len && pathMarkup[i] == '.')
            {
                hasDecimal = true;
                i++;
            }

            while (i < len && char.IsDigit(pathMarkup[i]))
            {
                hasDigits = true;
                i++;
            }

            if (!hasDecimal && i < len && pathMarkup[i] == '.')
            {
                hasDecimal = true;
                i++;
                while (i < len && char.IsDigit(pathMarkup[i]))
                {
                    hasDigits = true;
                    i++;
                }
            }

            if (hasDigits && i < len && (pathMarkup[i] == 'e' || pathMarkup[i] == 'E'))
            {
                i++;
                if (i < len && (pathMarkup[i] == '+' || pathMarkup[i] == '-'))
                {
                    i++;
                }
                while (i < len && char.IsDigit(pathMarkup[i]))
                {
                    i++;
                }
            }

            if (i == start || (i == start + 1 && (pathMarkup[start] == '+' || pathMarkup[start] == '-')))
            {
                throw new FormatException($"Invalid path token at index {start}: '{pathMarkup[start]}'");
            }

            string numStr = pathMarkup.Substring(start, i - start);
            if (double.TryParse(numStr, NumberStyles.Float, CultureInfo.InvariantCulture, out double val))
            {
                tokens.Add(val);
            }
            else
            {
                throw new FormatException($"Failed to parse number '{numStr}' at index {start}");
            }
        }

        return tokens;
    }

    private static PathSegment CreateArcSegment(Point p1, Point p2, double rx, double ry, double angleDeg, bool largeArc, bool sweep)
    {
        rx = Math.Abs(rx);
        ry = Math.Abs(ry);

        if (rx == 0 || ry == 0 || (p1.X == p2.X && p1.Y == p2.Y))
        {
            return new LineSegment { Point = p2 };
        }

        double phi = angleDeg * Math.PI / 180.0;
        double cosPhi = Math.Cos(phi);
        double sinPhi = Math.Sin(phi);

        double dx2 = (p1.X - p2.X) / 2.0;
        double dy2 = (p1.Y - p2.Y) / 2.0;

        double x1Prime = cosPhi * dx2 + sinPhi * dy2;
        double y1Prime = -sinPhi * dx2 + cosPhi * dy2;

        double rxSq = rx * rx;
        double rySq = ry * ry;
        double x1PrimeSq = x1Prime * x1Prime;
        double y1PrimeSq = y1Prime * y1Prime;

        double lambda = (x1PrimeSq / rxSq) + (y1PrimeSq / rySq);
        if (lambda > 1.0)
        {
            double sqrtLambda = Math.Sqrt(lambda);
            rx *= sqrtLambda;
            ry *= sqrtLambda;
        }

        return new ArcSegment
        {
            Point = p2,
            Size = new Size(rx, ry),
            RotationAngle = angleDeg,
            IsLargeArc = largeArc,
            SweepDirection = sweep ? SweepDirection.Clockwise : SweepDirection.Counterclockwise
        };
    }

    public object? Convert(object value, Type targetType, object parameter, string language)
    {
        return ConvertIcon(value, targetType, parameter, language);
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
        => throw new NotImplementedException();
}

public partial class StringToUriConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        if (value is string path && !string.IsNullOrEmpty(path))
        {
            return new Uri(path);
        }
        return new Uri(string.Empty);
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        if (value is Uri uri)
        {
            return uri.AbsolutePath;
        }
        return string.Empty;
    }
}
