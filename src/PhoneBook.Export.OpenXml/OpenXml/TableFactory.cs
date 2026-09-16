// FILE: src/PhoneBook.Export.OpenXml/OpenXml/TableFactory.cs
using System.Xml;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Wordprocessing;
using PhoneBook.Core.Layout;
using PhoneBook.Core.Text;
using PhoneBook.Domain.Entities;

namespace PhoneBook.Export.OpenXml.OpenXml;

public static class TableFactory
{
    public static Table CreateMotherTable(
        PageLayout page,
        AppSettings settings,
        ITextMeasurer measurer)
    {
        ArgumentNullException.ThrowIfNull(page);
        ArgumentNullException.ThrowIfNull(settings);
        ArgumentNullException.ThrowIfNull(measurer);

        if (page.Columns.Count != OpenXmlConstants.ColumnCount
            || page.ColumnHeightsMm.Length != OpenXmlConstants.ColumnCount)
        {
            throw new ArgumentException("A page layout must contain exactly three columns.", nameof(page));
        }

        int contentWidthDxa = GetPageContentWidthDxa(settings);
        int baseColumnWidth = contentWidthDxa / OpenXmlConstants.ColumnCount;
        int[] columnWidths =
        [
            baseColumnWidth,
            baseColumnWidth,
            contentWidthDxa - (2 * baseColumnWidth)
        ];

        Table table = new();
        table.Append(
            new TableProperties(
                new BiDiVisual { Val = OnOffOnlyValues.On },
                CreateTableWidth(contentWidthDxa),
                new TableLayout { Type = TableLayoutValues.Fixed }),
            new TableGrid(
                columnWidths.Select(width => new GridColumn
                {
                    Width = XmlConvert.ToString(width)
                })));

        TableRow row = new();
        for (int columnIndex = 0; columnIndex < OpenXmlConstants.ColumnCount; columnIndex++)
        {
            int columnWidth = columnWidths[columnIndex];
            TableCell cell = new(
                new TableCellProperties(
                    CreateCellWidth(columnWidth),
                    CreateZeroCellMargins(),
                    new TableCellVerticalAlignment { Val = TableVerticalAlignmentValues.Top }));

            IReadOnlyList<PlacedGroup> placedGroups = page.Columns[columnIndex];
            foreach (PlacedGroup placedGroup in placedGroups)
            {
                cell.Append(CreateGroupTable(
                    placedGroup.Group,
                    settings,
                    measurer,
                    columnWidth));
                cell.Append(CreateGapParagraph(settings.GroupGapMm));
            }

            if (placedGroups.Count == 0)
            {
                cell.Append(ParagraphFactory.CreateEmpty());
            }

            row.Append(cell);
        }

        table.Append(row);
        return table;
    }

    public static Table CreateGroupTable(
        PhoneBookGroup group,
        AppSettings settings,
        ITextMeasurer measurer)
    {
        return CreateGroupTable(
            group,
            settings,
            measurer,
            GetPageContentWidthDxa(settings) / OpenXmlConstants.ColumnCount);
    }

    private static Table CreateGroupTable(
        PhoneBookGroup group,
        AppSettings settings,
        ITextMeasurer measurer,
        int tableWidthDxa)
    {
        ArgumentNullException.ThrowIfNull(group);
        ArgumentNullException.ThrowIfNull(settings);
        ArgumentNullException.ThrowIfNull(measurer);

        HeightEstimator estimator = new();
        double rowHeightMm = estimator.EstimateRowHeightMm(settings, measurer);
        double groupHeightMm = estimator.EstimateGroupHeightMm(group, settings, measurer);
        double headerHeightMm = groupHeightMm - (group.Entries.Count * rowHeightMm);

        int extensionWidthDxa = checked((int)Math.Round(
            tableWidthDxa * 0.25,
            MidpointRounding.AwayFromZero));
        int nameWidthDxa = tableWidthDxa - extensionWidthDxa;
        int paddingDxa = MillimetersToDxa(settings.CellPaddingMm);

        Table table = new();
        table.Append(
            new TableProperties(
                new BiDiVisual { Val = OnOffOnlyValues.On },
                CreateTableWidth(tableWidthDxa),
                CreateThinBorders(),
                new TableLayout { Type = TableLayoutValues.Fixed }),
            new TableGrid(
                new GridColumn { Width = XmlConvert.ToString(nameWidthDxa) },
                new GridColumn { Width = XmlConvert.ToString(extensionWidthDxa) }));

        string title = settings.UsePersianDigits
            ? PersianTextNormalizer.ToPersianDigits(group.Title)
            : group.Title;
        TableCell headerCell = new(
            new TableCellProperties(
                CreateCellWidth(tableWidthDxa),
                new GridSpan { Val = 2 },
                new Shading { Val = ShadingPatternValues.Clear, Fill = "D9D9D9" },
                CreateCellMargins(paddingDxa),
                new TableCellVerticalAlignment { Val = TableVerticalAlignmentValues.Center }),
            ParagraphFactory.CreateCentered(
                title,
                settings.PrimaryFontFamily,
                settings.GroupHeaderFontSizePt,
                bold: true,
                rtl: true));
        table.Append(new TableRow(
            new TableRowProperties(
                new CantSplit { Val = OnOffOnlyValues.On },
                CreateExactRowHeight(headerHeightMm)),
            headerCell));

        foreach (PhoneBookEntry entry in group.Entries.OrderBy(item => item.DisplayOrder))
        {
            string name = settings.UsePersianDigits
                ? PersianTextNormalizer.ToPersianDigits(entry.Name)
                : entry.Name;
            string extension = entry.Extension ?? string.Empty;
            if (settings.UsePersianDigits)
            {
                extension = PersianTextNormalizer.ToPersianDigits(extension);
            }

            TableCell nameCell = new(
                new TableCellProperties(
                    CreateCellWidth(nameWidthDxa),
                    CreateCellMargins(paddingDxa),
                    new TableCellVerticalAlignment { Val = TableVerticalAlignmentValues.Center }),
                ParagraphFactory.CreateAligned(
                    name,
                    settings.PrimaryFontFamily,
                    settings.DefaultFontSizePt,
                    bold: false,
                    JustificationValues.Right));
            TableCell extensionCell = new(
                new TableCellProperties(
                    CreateCellWidth(extensionWidthDxa),
                    CreateCellMargins(paddingDxa),
                    new TableCellVerticalAlignment { Val = TableVerticalAlignmentValues.Center }),
                ParagraphFactory.CreateAligned(
                    extension,
                    settings.PrimaryFontFamily,
                    settings.DefaultFontSizePt,
                    bold: false,
                    JustificationValues.Center));

            table.Append(new TableRow(
                new TableRowProperties(
                    new CantSplit { Val = OnOffOnlyValues.On },
                    CreateExactRowHeight(rowHeightMm)),
                nameCell,
                extensionCell));
        }

        return table;
    }

    private static TableBorders CreateThinBorders()
    {
        return new TableBorders(
            CreateBorder<TopBorder>(),
            CreateBorder<LeftBorder>(),
            CreateBorder<BottomBorder>(),
            CreateBorder<RightBorder>(),
            CreateBorder<InsideHorizontalBorder>(),
            CreateBorder<InsideVerticalBorder>());
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

    private static TableRowHeight CreateExactRowHeight(double heightMm)
    {
        return new TableRowHeight
        {
            Val = checked((uint)MillimetersToDxa(heightMm)),
            HeightType = HeightRuleValues.Exact
        };
    }

    private static TableCellMargin CreateCellMargins(int paddingDxa)
    {
        string width = XmlConvert.ToString(paddingDxa);
        return new TableCellMargin(
            new TopMargin { Width = width, Type = TableWidthUnitValues.Dxa },
            new TableCellLeftMargin { Width = checked((short)paddingDxa), Type = TableWidthValues.Dxa },
            new BottomMargin { Width = width, Type = TableWidthUnitValues.Dxa },
            new TableCellRightMargin { Width = checked((short)paddingDxa), Type = TableWidthValues.Dxa });
    }

    private static TableCellMargin CreateZeroCellMargins()
    {
        return CreateCellMargins(0);
    }

    private static Paragraph CreateGapParagraph(double gapMm)
    {
        Paragraph paragraph = ParagraphFactory.CreateEmpty();
        SpacingBetweenLines spacing = paragraph.ParagraphProperties!
            .GetFirstChild<SpacingBetweenLines>()!;
        spacing.After = XmlConvert.ToString(MillimetersToDxa(gapMm));
        return paragraph;
    }

    private static TableWidth CreateTableWidth(int widthDxa)
    {
        return new TableWidth
        {
            Width = XmlConvert.ToString(widthDxa),
            Type = TableWidthUnitValues.Dxa
        };
    }

    private static TableCellWidth CreateCellWidth(int widthDxa)
    {
        return new TableCellWidth
        {
            Width = XmlConvert.ToString(widthDxa),
            Type = TableWidthUnitValues.Dxa
        };
    }

    private static int GetPageContentWidthDxa(AppSettings settings)
    {
        int leftMargin = MillimetersToDxa(settings.MarginLeftMm);
        int rightMargin = MillimetersToDxa(settings.MarginRightMm);
        int contentWidth = OpenXmlConstants.A4PageWidthDxa - leftMargin - rightMargin;
        if (contentWidth <= 0)
        {
            throw new ArgumentException("Page margins leave no usable content width.", nameof(settings));
        }

        return contentWidth;
    }

    internal static int MillimetersToDxa(double millimeters)
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
