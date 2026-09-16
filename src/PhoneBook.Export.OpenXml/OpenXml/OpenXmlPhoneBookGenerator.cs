// FILE: src/PhoneBook.Export.OpenXml/OpenXml/OpenXmlPhoneBookGenerator.cs
using System.Xml;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using PhoneBook.Core.Layout;
using PhoneBook.Core.Text;
using PhoneBook.Domain.Entities;

namespace PhoneBook.Export.OpenXml.OpenXml;

public sealed class OpenXmlPhoneBookGenerator
{
    private readonly ITextMeasurer _textMeasurer;
    private readonly FontEmbedder _fontEmbedder;

    public OpenXmlPhoneBookGenerator(ITextMeasurer textMeasurer)
        : this(
            textMeasurer,
            Path.Combine(AppContext.BaseDirectory, "wwwroot", "fonts"))
    {
    }

    public OpenXmlPhoneBookGenerator(ITextMeasurer textMeasurer, string baseFontsDirectoryPath)
    {
        _textMeasurer = textMeasurer ?? throw new ArgumentNullException(nameof(textMeasurer));
        _fontEmbedder = new FontEmbedder(baseFontsDirectoryPath);
    }

    public async Task<byte[]> GenerateAsync(
        DocumentHeader header,
        IReadOnlyList<PhoneBookGroup> groups,
        LayoutResult layout,
        AppSettings settings,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(header);
        ArgumentNullException.ThrowIfNull(groups);
        ArgumentNullException.ThrowIfNull(layout);
        ArgumentNullException.ThrowIfNull(settings);

        if (layout.LayoutFailed)
        {
            throw new InvalidOperationException(
                $"Cannot export a failed layout. {layout.FailureReason ?? string.Empty}".TrimEnd());
        }

        if (layout.Pages.Count == 0)
        {
            throw new ArgumentException("A layout must contain at least one page.", nameof(layout));
        }

        ValidateLayoutGroups(groups, layout);
        ct.ThrowIfCancellationRequested();

        AppSettings effectiveSettings = layout.EffectiveSettings;
        int leftMarginDxa = MillimetersToDxa(effectiveSettings.MarginLeftMm);
        int rightMarginDxa = MillimetersToDxa(effectiveSettings.MarginRightMm);
        int topMarginDxa = MillimetersToDxa(effectiveSettings.MarginTopMm);
        int bottomMarginDxa = MillimetersToDxa(effectiveSettings.MarginBottomMm);
        int contentWidthDxa = OpenXmlConstants.A4PageWidthDxa - leftMarginDxa - rightMarginDxa;

        if (contentWidthDxa <= 0)
        {
            throw new ArgumentException("Page margins leave no usable content width.", nameof(layout));
        }

        using MemoryStream stream = new();
        using (WordprocessingDocument document = WordprocessingDocument.Create(
                   stream,
                   WordprocessingDocumentType.Document,
                   autoSave: true))
        {
            MainDocumentPart mainPart = document.AddMainDocumentPart();
            mainPart.Document = new Document();
            Body body = mainPart.Document.AppendChild(new Body());

            await _fontEmbedder.EmbedAsync(mainPart, ct);
            ct.ThrowIfCancellationRequested();

            body.Append(CreateTitleTable(header, effectiveSettings, contentWidthDxa));

            for (int pageIndex = 0; pageIndex < layout.Pages.Count; pageIndex++)
            {
                ct.ThrowIfCancellationRequested();

                if (pageIndex > 0)
                {
                    body.Append(ParagraphFactory.CreatePageBreak());
                }

                body.Append(TableFactory.CreateMotherTable(
                    layout.Pages[pageIndex],
                    effectiveSettings,
                    _textMeasurer));
            }

            body.Append(new SectionProperties(
                new PageSize
                {
                    Width = checked((uint)OpenXmlConstants.A4PageWidthDxa),
                    Height = checked((uint)OpenXmlConstants.A4PageHeightDxa),
                    Orient = PageOrientationValues.Portrait
                },
                new PageMargin
                {
                    Top = topMarginDxa,
                    Right = checked((uint)rightMarginDxa),
                    Bottom = bottomMarginDxa,
                    Left = checked((uint)leftMarginDxa),
                    Header = 0U,
                    Footer = 0U,
                    Gutter = 0U
                },
                new BiDi { Val = true }));

            mainPart.Document.Save();
        }

        byte[] result = stream.ToArray();
        OpenXmlValidatorRunner.Validate(result);
        return result;
    }

    private static Table CreateTitleTable(
        DocumentHeader header,
        AppSettings settings,
        int contentWidthDxa)
    {
        TableBorders borders = new(
            CreateBorder<TopBorder>(),
            CreateBorder<LeftBorder>(),
            CreateBorder<BottomBorder>(),
            CreateBorder<RightBorder>(),
            CreateBorder<InsideHorizontalBorder>(),
            CreateBorder<InsideVerticalBorder>());
        TableProperties properties = new(
            new TableWidth
            {
                Width = XmlConvert.ToString(contentWidthDxa),
                Type = TableWidthUnitValues.Dxa
            },
            borders,
            new TableLayout { Type = TableLayoutValues.Fixed });
        TableGrid grid = new(
            new GridColumn { Width = XmlConvert.ToString(contentWidthDxa) });

        string title = settings.UsePersianDigits
            ? PersianTextNormalizer.ToPersianDigits(header.Title)
            : header.Title;
        TableCell cell = new(
            new TableCellProperties(
                new TableCellWidth
                {
                    Width = XmlConvert.ToString(contentWidthDxa),
                    Type = TableWidthUnitValues.Dxa
                },
                new TableCellVerticalAlignment { Val = TableVerticalAlignmentValues.Center }),
            ParagraphFactory.CreateCentered(
                title,
                settings.PrimaryFontFamily,
                settings.HeaderFontSizePt,
                bold: true,
                rtl: true));

        return new Table(properties, grid, new TableRow(cell));
    }

    private static TBorder CreateBorder<TBorder>()
        where TBorder : BorderType, new()
    {
        return new TBorder
        {
            Val = BorderValues.Single,
            Color = "000000",
            Size = OpenXmlConstants.ThinBorderSizeEighthPoints,
            Space = 0U
        };
    }

    private static void ValidateLayoutGroups(
        IReadOnlyList<PhoneBookGroup> groups,
        LayoutResult layout)
    {
        HashSet<PhoneBookGroup> expected = new(groups, ReferenceEqualityComparer.Instance);
        List<PhoneBookGroup> placed = layout.Pages
            .SelectMany(page => page.Columns)
            .SelectMany(column => column)
            .Select(item => item.Group)
            .ToList();

        if (placed.Count != groups.Count
            || placed.Any(group => !expected.Contains(group))
            || placed.Distinct(ReferenceEqualityComparer.Instance).Count() != placed.Count)
        {
            throw new ArgumentException(
                "The layout must contain every supplied group exactly once.",
                nameof(layout));
        }
    }

    private static int MillimetersToDxa(double millimeters)
    {
        if (!double.IsFinite(millimeters) || millimeters < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(millimeters));
        }

        return checked((int)Math.Round(
            millimeters * OpenXmlConstants.DxaPerMillimeter,
            MidpointRounding.AwayFromZero));
    }
}
