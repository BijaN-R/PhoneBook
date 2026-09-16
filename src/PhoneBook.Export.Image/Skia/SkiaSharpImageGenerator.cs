// FILE: src/PhoneBook.Export.Image/Skia/SkiaSharpImageGenerator.cs
using PhoneBook.Core.Layout;
using PhoneBook.Domain.Entities;
using SkiaSharp;

namespace PhoneBook.Export.Image.Skia;

public sealed class SkiaSharpImageGenerator
{
    private const double A4WidthMm = 210.0;
    private const double A4HeightMm = 297.0;
    private readonly SkiaPageRenderer _renderer;

    public SkiaSharpImageGenerator(SkiaPageRenderer renderer)
    {
        _renderer = renderer ?? throw new ArgumentNullException(nameof(renderer));
    }

    public Task<IReadOnlyList<byte[]>> GeneratePngPagesAsync(
        DocumentHeader header,
        LayoutResult layout,
        int dpi = 300,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(header);
        ValidateLayout(layout);

        if (dpi <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(dpi), "DPI must be greater than zero.");
        }

        int width = checked((int)Math.Round(A4WidthMm * dpi / 25.4, MidpointRounding.AwayFromZero));
        int height = checked((int)Math.Round(A4HeightMm * dpi / 25.4, MidpointRounding.AwayFromZero));
        double dpiScale = dpi / 72.0;
        SKImageInfo imageInfo = new(width, height, SKColorType.Rgba8888, SKAlphaType.Premul);
        List<byte[]> pages = new(layout.Pages.Count);

        for (int pageIndex = 0; pageIndex < layout.Pages.Count; pageIndex++)
        {
            ct.ThrowIfCancellationRequested();

            using SKSurface surface = SKSurface.Create(imageInfo)
                ?? throw new InvalidOperationException("SkiaSharp could not create the PNG drawing surface.");
            _renderer.Render(
                surface.Canvas,
                header,
                layout.Pages[pageIndex],
                layout.EffectiveSettings,
                isFirstPage: pageIndex == 0,
                dpiScale);
            surface.Canvas.Flush();

            using SKImage image = surface.Snapshot();
            using SKData data = image.Encode(SKEncodedImageFormat.Png, 100)
                ?? throw new InvalidOperationException("SkiaSharp could not encode the rendered page as PNG.");
            pages.Add(data.ToArray());
        }

        return Task.FromResult<IReadOnlyList<byte[]>>(pages);
    }

    private static void ValidateLayout(LayoutResult layout)
    {
        ArgumentNullException.ThrowIfNull(layout);

        if (layout.LayoutFailed)
        {
            throw new InvalidOperationException(
                $"Cannot export a failed layout. {layout.FailureReason ?? string.Empty}".TrimEnd());
        }

        if (layout.Pages.Count == 0)
        {
            throw new ArgumentException("A layout must contain at least one page.", nameof(layout));
        }
    }
}
