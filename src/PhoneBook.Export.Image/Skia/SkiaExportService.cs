// FILE: src/PhoneBook.Export.Image/Skia/SkiaExportService.cs
using PhoneBook.Core.Layout;
using PhoneBook.Domain.Entities;

namespace PhoneBook.Export.Image.Skia;

public sealed class SkiaExportService
{
    private readonly SkiaSharpImageGenerator _imageGenerator;
    private readonly SkiaSharpPdfGenerator _pdfGenerator;

    public SkiaExportService(
        SkiaSharpImageGenerator imageGenerator,
        SkiaSharpPdfGenerator pdfGenerator)
    {
        _imageGenerator = imageGenerator ?? throw new ArgumentNullException(nameof(imageGenerator));
        _pdfGenerator = pdfGenerator ?? throw new ArgumentNullException(nameof(pdfGenerator));
    }

    public Task<byte[]> ExportPdfAsync(
        DocumentHeader header,
        LayoutResult layout,
        CancellationToken ct = default)
    {
        return _pdfGenerator.GeneratePdfAsync(header, layout, ct);
    }

    public Task<IReadOnlyList<byte[]>> ExportPngPagesAsync(
        DocumentHeader header,
        LayoutResult layout,
        int dpi = 300,
        CancellationToken ct = default)
    {
        return _imageGenerator.GeneratePngPagesAsync(header, layout, dpi, ct);
    }
}
