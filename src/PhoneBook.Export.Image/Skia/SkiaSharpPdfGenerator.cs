// FILE: src/PhoneBook.Export.Image/Skia/SkiaSharpPdfGenerator.cs
using PhoneBook.Core.Layout;
using PhoneBook.Domain.Entities;
using SkiaSharp;

namespace PhoneBook.Export.Image.Skia;

public sealed class SkiaSharpPdfGenerator
{
    private const float A4WidthPoints = 595.28f;
    private const float A4HeightPoints = 841.89f;
    private static ReadOnlySpan<byte> PageMarker => "/Type /Page"u8;

    private readonly SkiaPageRenderer _renderer;

    public SkiaSharpPdfGenerator(SkiaPageRenderer renderer)
    {
        _renderer = renderer ?? throw new ArgumentNullException(nameof(renderer));
    }

    public Task<byte[]> GeneratePdfAsync(
        DocumentHeader header,
        LayoutResult layout,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(header);
        ValidateLayout(layout);
        ct.ThrowIfCancellationRequested();

        using MemoryStream stream = new();
        using (SKDocument document = SKDocument.CreatePdf(stream)
               ?? throw new InvalidOperationException("SkiaSharp could not create the PDF document."))
        {
            for (int pageIndex = 0; pageIndex < layout.Pages.Count; pageIndex++)
            {
                ct.ThrowIfCancellationRequested();

                SKCanvas canvas = document.BeginPage(A4WidthPoints, A4HeightPoints)
                    ?? throw new InvalidOperationException($"SkiaSharp could not begin PDF page {pageIndex + 1}.");
                _renderer.Render(
                    canvas,
                    header,
                    layout.Pages[pageIndex],
                    layout.EffectiveSettings,
                    isFirstPage: pageIndex == 0,
                    dpiScale: 1.0);
                document.EndPage();
            }

            document.Close();
        }

        byte[] pdfBytes = stream.ToArray();
        int actualPageCount = CountPages(pdfBytes);
        if (actualPageCount != layout.Pages.Count)
        {
            throw new InvalidDataException(
                $"Generated PDF page count mismatch. Expected {layout.Pages.Count}, found {actualPageCount}.");
        }

        return Task.FromResult(pdfBytes);
    }

    public int CountPages(byte[] pdf)
    {
        ArgumentNullException.ThrowIfNull(pdf);

        ReadOnlySpan<byte> source = pdf;
        ReadOnlySpan<byte> marker = PageMarker;
        int count = 0;

        for (int index = 0; index <= source.Length - marker.Length; index++)
        {
            if (!MatchesAsciiCaseInsensitive(source.Slice(index, marker.Length), marker))
            {
                continue;
            }

            int followingIndex = index + marker.Length;
            if (followingIndex < source.Length
                && (source[followingIndex] == (byte)'s' || source[followingIndex] == (byte)'S'))
            {
                continue;
            }

            count++;
            index += marker.Length - 1;
        }

        return count;
    }

    private static bool MatchesAsciiCaseInsensitive(ReadOnlySpan<byte> value, ReadOnlySpan<byte> expected)
    {
        for (int index = 0; index < expected.Length; index++)
        {
            byte actualByte = value[index];
            byte expectedByte = expected[index];
            if (actualByte is >= (byte)'A' and <= (byte)'Z')
            {
                actualByte = (byte)(actualByte + ('a' - 'A'));
            }

            if (expectedByte is >= (byte)'A' and <= (byte)'Z')
            {
                expectedByte = (byte)(expectedByte + ('a' - 'A'));
            }

            if (actualByte != expectedByte)
            {
                return false;
            }
        }

        return true;
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
