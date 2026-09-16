// FILE: src/PhoneBook.Export.Image/Skia/SkiaPageRenderer.cs
using PhoneBook.Core.Layout;
using PhoneBook.Core.Text;
using PhoneBook.Domain.Entities;
using SkiaSharp;

namespace PhoneBook.Export.Image.Skia;

public sealed class SkiaPageRenderer
{
    private const int ColumnCount = 3;
    private const double A4WidthMm = 210.0;
    private const double A4HeightMm = 297.0;
    private const float BorderWidthPt = 0.5f;

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

            float columnsTop = top;
            if (isFirstPage)
            {
                float titleHeight = GetLineHeight(titleFont) + (2 * MmToPt(settings.CellPaddingMm));
                SKRect titleBounds = new(left, top, right, top + titleHeight);
                canvas.DrawRect(titleBounds, borderPaint);
                DrawClippedText(
                    canvas,
                    DisplayText(header.Title, settings),
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
            groupFont,
            textPaint,
            headerBounds,
            HorizontalTextAlignment.Center,
            padding);

        float rowTop = headerBounds.Bottom;
        float midpoint = left + ((right - left) / 2);

        foreach (PhoneBookEntry entry in entries)
        {
            SKRect extensionCell = new(left, rowTop, midpoint, rowTop + rowHeight);
            SKRect nameCell = new(midpoint, rowTop, right, rowTop + rowHeight);

            canvas.DrawRect(extensionCell, borderPaint);
            canvas.DrawRect(nameCell, borderPaint);

            DrawClippedText(
                canvas,
                DisplayText(entry.Extension ?? string.Empty, settings),
                rowFont,
                textPaint,
                extensionCell,
                HorizontalTextAlignment.Left,
                padding);
            DrawClippedText(
                canvas,
                DisplayText(entry.Name, settings),
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
            using SKTextBlob? blob = SKTextBlob.Create(text, font, SKPoint.Empty);
            if (blob is not null)
            {
                SKRect textBounds = blob.Bounds;
                float x = alignment switch
                {
                    HorizontalTextAlignment.Left => contentLeft - textBounds.Left,
                    HorizontalTextAlignment.Center => bounds.MidX - textBounds.MidX,
                    HorizontalTextAlignment.Right => contentRight - textBounds.Right,
                    _ => throw new ArgumentOutOfRangeException(nameof(alignment))
                };
                canvas.DrawText(blob, x, baseline, paint);
            }
            else
            {
                SKTextAlign fallbackAlignment = alignment switch
                {
                    HorizontalTextAlignment.Left => SKTextAlign.Left,
                    HorizontalTextAlignment.Center => SKTextAlign.Center,
                    HorizontalTextAlignment.Right => SKTextAlign.Right,
                    _ => throw new ArgumentOutOfRangeException(nameof(alignment))
                };
                float anchor = alignment switch
                {
                    HorizontalTextAlignment.Left => contentLeft,
                    HorizontalTextAlignment.Center => bounds.MidX,
                    HorizontalTextAlignment.Right => contentRight,
                    _ => throw new ArgumentOutOfRangeException(nameof(alignment))
                };
                canvas.DrawText(text, anchor, baseline, fallbackAlignment, font, paint);
            }
        }
        finally
        {
            canvas.RestoreToCount(restoreCount);
        }
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
