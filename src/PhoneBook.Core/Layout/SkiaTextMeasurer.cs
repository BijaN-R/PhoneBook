// FILE: src/PhoneBook.Core/Layout/SkiaTextMeasurer.cs
using SkiaSharp;

namespace PhoneBook.Core.Layout;

public sealed class SkiaTextMeasurer(FontProvider fontProvider) : ITextMeasurer
{
    private const double MillimetersPerPoint = 25.4 / 72.0;

    public double MeasureTextWidthMm(string text, string fontFamily, double fontSizePt)
    {
        ArgumentNullException.ThrowIfNull(text);
        ValidateFontArguments(fontFamily, fontSizePt);

        using SKFont font = new(fontProvider.GetRegular(), checked((float)fontSizePt));
        return font.MeasureText(text) * MillimetersPerPoint;
    }

    public double MeasureTextHeightMm(string fontFamily, double fontSizePt)
    {
        ValidateFontArguments(fontFamily, fontSizePt);

        using SKFont font = new(fontProvider.GetRegular(), checked((float)fontSizePt));
        SKFontMetrics metrics = font.Metrics;
        double heightPoints = metrics.Descent - metrics.Ascent + metrics.Leading;
        return Math.Max(0, heightPoints) * MillimetersPerPoint;
    }

    private static void ValidateFontArguments(string fontFamily, double fontSizePt)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(fontFamily);

        if (!double.IsFinite(fontSizePt) || fontSizePt <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(fontSizePt), "Font size must be finite and greater than zero.");
        }
    }
}
