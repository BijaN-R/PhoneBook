// FILE: src/PhoneBook.Export.OpenXml/OpenXml/ParagraphFactory.cs
using DocumentFormat.OpenXml.Wordprocessing;

namespace PhoneBook.Export.OpenXml.OpenXml;

public static class ParagraphFactory
{
    public static Paragraph CreateCentered(
        string text,
        string font,
        double sizePt,
        bool bold = false,
        bool rtl = true)
    {
        ParagraphProperties properties = new(
            new BiDi { Val = rtl },
            new Justification { Val = JustificationValues.Center });

        return new Paragraph(
            properties,
            RunFactory.CreateTextRun(text, font, sizePt, bold, rtl));
    }

    public static Paragraph CreateEmpty()
    {
        return new Paragraph(
            new ParagraphProperties(
                new BiDi { Val = true },
                new SpacingBetweenLines
                {
                    Before = "0",
                    After = "0",
                    Line = "1",
                    LineRule = LineSpacingRuleValues.Exact
                }));
    }

    public static Paragraph CreatePageBreak()
    {
        return new Paragraph(
            new ParagraphProperties(new BiDi { Val = true }),
            new Run(
                new RunProperties(
                    new RightToLeftText { Val = true },
                    new Languages { Bidi = "fa-IR" }),
                new Break { Type = BreakValues.Page }));
    }

    internal static Paragraph CreateAligned(
        string text,
        string font,
        double sizePt,
        bool bold,
        JustificationValues alignment)
    {
        return new Paragraph(
            new ParagraphProperties(
                new BiDi { Val = true },
                new Justification { Val = alignment }),
            RunFactory.CreateTextRun(text, font, sizePt, bold, rtl: true));
    }
}
