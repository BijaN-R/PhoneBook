// FILE: src/PhoneBook.Export.Image/Skia/SkiaPageRenderer.cs
using PhoneBook.Core.Layout;
using PhoneBook.Core.Text;
using PhoneBook.Domain.Entities;
using SkiaSharp;
using SkiaSharp.HarfBuzz;
using HarfBuzzBuffer = HarfBuzzSharp.Buffer;
using HarfBuzzDirection = HarfBuzzSharp.Direction;

namespace PhoneBook.Export.Image.Skia;

public sealed class SkiaPageRenderer
{
    private const int ColumnCount = 3;
    private const double A4WidthMm = 210.0;
    private const double A4HeightMm = 297.0;
    private const float BorderWidthPt = 0.5f;
    internal const float ExtensionColumnRatio = 0.30f;

    private static readonly SKColor GroupHeaderColor = new(0xD9, 0xD9, 0xD9);
    private readonly SkiaFontRegistry _fonts;

    public SkiaPageRenderer(SkiaFontRegistry fonts)
    {
        _fonts = fonts ?? throw new ArgumentNullException(nameof(fonts));
    }

    public void Render(
        SKCanvas canvas,
        DocumentHeader header,
        PageLayout page,
        AppSettings settings,
        bool isFirstPage,
        double dpiScale)
    {
        ArgumentNullException.ThrowIfNull(canvas);
        ArgumentNullException.ThrowIfNull(header);
        ArgumentNullException.ThrowIfNull(page);
        ArgumentNullException.ThrowIfNull(settings);

        if (!double.IsFinite(dpiScale) || dpiScale <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(dpiScale), "DPI scale must be finite and greater than zero.");
        }

        if (page.Columns.Count != ColumnCount)
        {
            throw new ArgumentException($"A page must contain exactly {ColumnCount} columns.", nameof(page));
        }

        ValidateSettings(settings);

        canvas.Clear(SKColors.White);
        int restoreCount = canvas.Save();

        try
        {
            canvas.Scale(checked((float)dpiScale));

            float pageWidth = MmToPt(A4WidthMm);
            float pageHeight = MmToPt(A4HeightMm);
            using SKPaint backgroundPaint = CreateFillPaint(SKColors.White);
            canvas.DrawRect(new SKRect(0, 0, pageWidth, pageHeight), backgroundPaint);

            float left = MmToPt(settings.MarginLeftMm);
            float right = pageWidth - MmToPt(settings.MarginRightMm);
            float top = MmToPt(settings.MarginTopMm);
            float bottom = pageHeight - MmToPt(settings.MarginBottomMm);
            float availableWidth = right - left;
            if (availableWidth <= 0 || bottom <= top)
            {
                throw new ArgumentException("Page margins leave no usable drawing area.", nameof(settings));
            }

            using SKPaint borderPaint = CreateStrokePaint(SKColors.Black, BorderWidthPt);
            using SKPaint textPaint = CreateFillPaint(SKColors.Black);
            using SKPaint groupHeaderPaint = CreateFillPaint(GroupHeaderColor);
            using SKFont titleFont = CreateFont(_fonts.Bold, settings.HeaderFontSizePt);
            using SKFont groupFont = CreateFont(_fonts.Bold, settings.GroupHeaderFontSizePt);
            using SKFont rowFont = CreateFont(_fonts.Regular, settings.DefaultFontSizePt);
            using SKShaper boldShaper = new(_fonts.Bold);
            using SKShaper regularShaper = new(_fonts.Regular);

            float columnsTop = top;
            if (isFirstPage)
            {
                float titleHeight = GetLineHeight(titleFont) + (2 * MmToPt(settings.CellPaddingMm));
                SKRect titleBounds = new(left, top, right, top + titleHeight);
                canvas.DrawRect(titleBounds, borderPaint);
                DrawClippedText(
                    canvas,
                    DisplayText(header.Title, settings),
                    boldShaper,
                    titleFont,
                    textPaint,
                    titleBounds,
                    HorizontalTextAlignment.Center,
                    MmToPt(settings.CellPaddingMm));
                columnsTop = titleBounds.Bottom + MmToPt(settings.GroupGapMm);
            }

            float columnWidth = availableWidth / ColumnCount;
            float groupGap = MmToPt(settings.GroupGapMm);

            for (int logicalColumn = 0; logicalColumn < ColumnCount; logicalColumn++)
            {
                float columnRight = right - (logicalColumn * columnWidth);
                float columnLeft = logicalColumn == ColumnCount - 1
                    ? left
                    : columnRight - columnWidth;
                float y = columnsTop;

                foreach (PlacedGroup placedGroup in page.Columns[logicalColumn]
                             .OrderBy(item => item.RowIndex))
                {
                    DrawGroup(
                        canvas,
                        placedGroup,
                        settings,
                        columnLeft,
                        columnRight,
                        y,
                        boldShaper,
                        regularShaper,
                        groupFont,
                        rowFont,
                        textPaint,
                        groupHeaderPaint,
                        borderPaint);

                    y += MmToPt(placedGroup.HeightMm) + groupGap;
                }
            }
        }
        finally
        {
            canvas.RestoreToCount(restoreCount);
        }
    }

    public static float MmToPt(double millimeters)
    {
        if (!double.IsFinite(millimeters))
        {
            throw new ArgumentOutOfRangeException(nameof(millimeters), "Millimeters must be finite.");
        }

        return checked((float)(millimeters * 72.0 / 25.4));
    }

    private static void DrawGroup(
        SKCanvas canvas,
        PlacedGroup placedGroup,
        AppSettings settings,
        float left,
        float right,
        float top,
        SKShaper groupShaper,
        SKShaper rowShaper,
        SKFont groupFont,
        SKFont rowFont,
        SKPaint textPaint,
        SKPaint groupHeaderPaint,
        SKPaint borderPaint)
    {
        ArgumentNullException.ThrowIfNull(placedGroup.Group);

        IReadOnlyList<PhoneBookEntry> entries = placedGroup.Group.Entries
            .OrderBy(entry => entry.DisplayOrder)
            .ThenBy(entry => entry.Id)
            .ToList();

        float totalHeight = MmToPt(placedGroup.HeightMm);
        float padding = MmToPt(settings.CellPaddingMm);
        float estimatedRowHeight = GetLineHeight(rowFont) + (2 * padding);
        float minimumHeaderHeight = GetLineHeight(groupFont) + (2 * padding);
        float headerHeight = entries.Count == 0
            ? totalHeight
            : Math.Max(minimumHeaderHeight, totalHeight - (entries.Count * estimatedRowHeight));
        headerHeight = Math.Min(totalHeight, headerHeight);
        float rowHeight = entries.Count == 0
            ? 0
            : Math.Max(0, (totalHeight - headerHeight) / entries.Count);

        SKRect headerBounds = new(left, top, right, top + headerHeight);
        canvas.DrawRect(headerBounds, groupHeaderPaint);
        canvas.DrawRect(headerBounds, borderPaint);
        DrawClippedText(
            canvas,
            DisplayText(placedGroup.Group.Title, settings),
            groupShaper,
            groupFont,
            textPaint,
            headerBounds,
            HorizontalTextAlignment.Center,
            padding);

        float rowTop = headerBounds.Bottom;
        float extensionRight = left + ((right - left) * ExtensionColumnRatio);

        foreach (PhoneBookEntry entry in entries)
        {
            SKRect extensionCell = new(left, rowTop, extensionRight, rowTop + rowHeight);
            SKRect nameCell = new(extensionRight, rowTop, right, rowTop + rowHeight);

            canvas.DrawRect(extensionCell, borderPaint);
            canvas.DrawRect(nameCell, borderPaint);

            DrawClippedText(
                canvas,
                DisplayText(entry.Extension ?? string.Empty, settings),
                rowShaper,
                rowFont,
                textPaint,
                extensionCell,
                HorizontalTextAlignment.Center,
                padding);
            DrawClippedText(
                canvas,
                DisplayText(entry.Name, settings),
                rowShaper,
                rowFont,
                textPaint,
                nameCell,
                HorizontalTextAlignment.Right,
                padding);

            rowTop += rowHeight;
        }
    }

    private static void DrawClippedText(
        SKCanvas canvas,
        string text,
        SKShaper shaper,
        SKFont font,
        SKPaint paint,
        SKRect bounds,
        HorizontalTextAlignment alignment,
        float padding)
    {
        if (string.IsNullOrEmpty(text) || bounds.Width <= 0 || bounds.Height <= 0)
        {
            return;
        }

        float contentLeft = bounds.Left + padding;
        float contentRight = bounds.Right - padding;
        if (contentRight <= contentLeft)
        {
            return;
        }

        SKFontMetrics metrics = font.Metrics;
        float baseline = bounds.MidY - ((metrics.Ascent + metrics.Descent) / 2);
        int restoreCount = canvas.Save();
        try
        {
            canvas.ClipRect(new SKRect(contentLeft, bounds.Top, contentRight, bounds.Bottom));
            IReadOnlyList<DirectionalTextRun> visualRuns = BidirectionalText.GetVisualRuns(text);
            SKShaper.Result[] shapedRuns = new SKShaper.Result[visualRuns.Count];
            float textWidth = 0;
            for (int index = 0; index < visualRuns.Count; index++)
            {
                SKShaper.Result shapedRun = ShapeTextRun(shaper, visualRuns[index], font);
                shapedRuns[index] = shapedRun;
                textWidth += shapedRun.Width;
            }

            float x = alignment switch
            {
                HorizontalTextAlignment.Left => contentLeft,
                HorizontalTextAlignment.Center => bounds.MidX - (textWidth / 2),
                HorizontalTextAlignment.Right => contentRight - textWidth,
                _ => throw new ArgumentOutOfRangeException(nameof(alignment))
            };

            for (int index = 0; index < visualRuns.Count; index++)
            {
                DrawShapedRun(canvas, shapedRuns[index], font, paint, x, baseline);
                x += shapedRuns[index].Width;
            }
        }
        finally
        {
            canvas.RestoreToCount(restoreCount);
        }
    }

    internal static SKShaper.Result ShapeTextRun(
        SKShaper shaper,
        DirectionalTextRun run,
        SKFont font)
    {
        using HarfBuzzBuffer buffer = new();
        buffer.AddUtf16(run.Text);
        buffer.GuessSegmentProperties();
        buffer.Direction = run.Direction == TextDirection.RightToLeft
            ? HarfBuzzDirection.RightToLeft
            : HarfBuzzDirection.LeftToRight;

        return shaper.Shape(buffer, font);
    }

    private static void DrawShapedRun(
        SKCanvas canvas,
        SKShaper.Result shapedRun,
        SKFont font,
        SKPaint paint,
        float x,
        float baseline)
    {
        if (shapedRun.Codepoints.Length == 0)
        {
            return;
        }

        using SKTextBlobBuilder builder = new();
        SKPositionedRunBuffer runBuffer = builder.AllocatePositionedRun(
            font,
            shapedRun.Codepoints.Length);

        for (int index = 0; index < shapedRun.Codepoints.Length; index++)
        {
            runBuffer.Glyphs[index] = checked((ushort)shapedRun.Codepoints[index]);
            runBuffer.Positions[index] = shapedRun.Points[index];
        }

        using SKTextBlob blob = builder.Build()
            ?? throw new InvalidOperationException("SkiaSharp could not build the shaped text run.");
        canvas.DrawText(blob, x, baseline, paint);
    }

    private static SKFont CreateFont(SKTypeface typeface, double sizePt)
    {
        if (!double.IsFinite(sizePt) || sizePt <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(sizePt), "Font size must be finite and greater than zero.");
        }

        return new SKFont(typeface, checked((float)sizePt))
        {
            Edging = SKFontEdging.Antialias,
            Subpixel = true
        };
    }

    private static SKPaint CreateFillPaint(SKColor color)
    {
        return new SKPaint
        {
            Color = color,
            IsAntialias = true,
            Style = SKPaintStyle.Fill
        };
    }

    private static SKPaint CreateStrokePaint(SKColor color, float strokeWidth)
    {
        return new SKPaint
        {
            Color = color,
            IsAntialias = true,
            Style = SKPaintStyle.Stroke,
            StrokeWidth = strokeWidth
        };
    }

    private static float GetLineHeight(SKFont font)
    {
        SKFontMetrics metrics = font.Metrics;
        return Math.Max(0, metrics.Descent - metrics.Ascent + metrics.Leading);
    }

    private static string DisplayText(string text, AppSettings settings)
    {
        return settings.UsePersianDigits
            ? PersianTextNormalizer.ToPersianDigits(text)
            : text;
    }

    private static void ValidateSettings(AppSettings settings)
    {
        ValidateNonNegative(settings.MarginTopMm, nameof(settings.MarginTopMm));
        ValidateNonNegative(settings.MarginBottomMm, nameof(settings.MarginBottomMm));
        ValidateNonNegative(settings.MarginLeftMm, nameof(settings.MarginLeftMm));
        ValidateNonNegative(settings.MarginRightMm, nameof(settings.MarginRightMm));
        ValidateNonNegative(settings.GroupGapMm, nameof(settings.GroupGapMm));
        ValidateNonNegative(settings.CellPaddingMm, nameof(settings.CellPaddingMm));
    }

    private static void ValidateNonNegative(double value, string propertyName)
    {
        if (!double.IsFinite(value) || value < 0)
        {
            throw new ArgumentOutOfRangeException(propertyName, "The value must be finite and non-negative.");
        }
    }

    private enum HorizontalTextAlignment
    {
        Left,
        Center,
        Right
    }
}
