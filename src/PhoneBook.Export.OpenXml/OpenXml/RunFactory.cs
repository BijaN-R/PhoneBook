// FILE: src/PhoneBook.Export.OpenXml/OpenXml/RunFactory.cs
using System.Xml;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Wordprocessing;

namespace PhoneBook.Export.OpenXml.OpenXml;

public static class RunFactory
{
    public static Run CreateTextRun(
        string text,
        string font,
        double sizePt,
        bool bold,
        bool rtl)
    {
        ArgumentNullException.ThrowIfNull(text);
        ArgumentException.ThrowIfNullOrWhiteSpace(font);

        if (!double.IsFinite(sizePt) || sizePt <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(sizePt), "Font size must be finite and greater than zero.");
        }

        uint halfPoints = checked((uint)Math.Round(
            sizePt * 2,
            MidpointRounding.AwayFromZero));
        string halfPointsValue = XmlConvert.ToString(halfPoints);

        RunProperties properties = new(
            new RunFonts
            {
                Ascii = font,
                HighAnsi = font,
                ComplexScript = font
            },
            new Bold { Val = bold },
            new BoldComplexScript { Val = bold },
            new FontSize { Val = halfPointsValue },
            new FontSizeComplexScript { Val = halfPointsValue },
            new RightToLeftText { Val = rtl },
            new Languages { Bidi = "fa-IR" });

        Text value = new(text)
        {
            Space = SpaceProcessingModeValues.Preserve
        };

        return new Run(properties, value);
    }
}
