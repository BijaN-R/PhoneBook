// FILE: src/PhoneBook.Tests/SkiaExportTests.cs
using System.Text;
using FluentAssertions;
using PhoneBook.Core.Layout;
using PhoneBook.Domain.Entities;
using PhoneBook.Export.Image.Skia;
using SkiaSharp;
using SkiaSharp.HarfBuzz;
using Xunit;

namespace PhoneBook.Tests;

public sealed class SkiaExportTests : IDisposable
{
    private static readonly byte[] PngSignature = [0x89, 0x50, 0x4E, 0x47];
    private readonly TestDataSeeder.DeterministicTextMeasurer _measurer = new();
    private readonly SkiaFontRegistry _fonts;
    private readonly SkiaSharpImageGenerator _imageGenerator;
    private readonly SkiaSharpPdfGenerator _pdfGenerator;

    public SkiaExportTests()
    {
        _fonts = new SkiaFontRegistry(TestDataSeeder.GetFontsPath());
        SkiaPageRenderer renderer = new(_fonts);
        _imageGenerator = new SkiaSharpImageGenerator(renderer);
        _pdfGenerator = new SkiaSharpPdfGenerator(renderer);
    }

    [Fact]
    public void Mixed_Persian_And_Latin_Text_Preserves_The_Latin_Run()
    {
        IReadOnlyList<DirectionalTextRun> runs = BidirectionalText.GetVisualRuns("واحد IT");

        runs.Select(run => run.Text).Should().Equal("IT", "واحد ");
        runs.Select(run => run.Direction).Should().Equal(
            TextDirection.LeftToRight,
            TextDirection.RightToLeft);
    }

    [Theory]
    [InlineData("۱۸۸")]
    [InlineData("188")]
    public void Numeric_Text_Is_Shaped_Left_To_Right(string text)
    {
        DirectionalTextRun run = BidirectionalText.GetVisualRuns(text).Should().ContainSingle().Subject;
        using SKFont font = new(_fonts.Regular, 12);
        using SKShaper shaper = new(_fonts.Regular);

        SKShaper.Result result = SkiaPageRenderer.ShapeTextRun(shaper, run, font);

        run.Direction.Should().Be(TextDirection.LeftToRight);
        result.Clusters.Should().BeInAscendingOrder();
    }

    [Fact]
    public void Persian_Text_Is_Shaped_As_A_Right_To_Left_Joined_Run()
    {
        const string text = "سلام";
        using SKFont font = new(_fonts.Regular, 12);
        using SKShaper shaper = new(_fonts.Regular);

        SKShaper.Result result = shaper.Shape(text, font);

        result.Codepoints.Should().NotBeEmpty();
        result.Clusters.Should().NotBeEmpty();
        result.Clusters.First().Should().BeGreaterThan(result.Clusters.Last());
        result.Codepoints.Should().NotEqual(font.GetGlyphs(text).Select(glyph => (uint)glyph));
    }

    [Fact]
    public void Png_And_Pdf_Entry_Columns_Use_Thirty_Seventy_Proportions()
    {
        SkiaPageRenderer.ExtensionColumnRatio.Should().Be(0.30f);
    }

    [Fact]
    public async Task Every_Generated_Png_Starts_With_The_Png_Signature()
    {
        LayoutResult layout = CreateSeedLayout();

        IReadOnlyList<byte[]> pages = await _imageGenerator.GeneratePngPagesAsync(
            TestDataSeeder.CreateHeader(),
            layout,
            dpi: 300);

        pages.Should().NotBeEmpty();
        pages.Should().OnlyContain(page => HasPngSignature(page));
    }

    [Fact]
    public async Task Generated_Pdf_Has_A_Pdf_Signature_And_Expected_Page_Count()
    {
        LayoutResult layout = CreateSeedLayout();

        byte[] pdf = await _pdfGenerator.GeneratePdfAsync(TestDataSeeder.CreateHeader(), layout);

        Encoding.ASCII.GetString(pdf, 0, 4).Should().Be("%PDF");
        _pdfGenerator.CountPages(pdf).Should().Be(layout.Pages.Count);
    }

    [Fact]
    public async Task Png_At_300_Dpi_Has_Exact_A4_Pixel_Dimensions()
    {
        LayoutResult layout = CreateSeedLayout();
        int expectedWidth = checked((int)Math.Round(210.0 / 25.4 * 300));
        int expectedHeight = checked((int)Math.Round(297.0 / 25.4 * 300));

        IReadOnlyList<byte[]> pages = await _imageGenerator.GeneratePngPagesAsync(
            TestDataSeeder.CreateHeader(),
            layout,
            dpi: 300);
        using SKData data = SKData.CreateCopy(pages[0]);
        using SKCodec codec = SKCodec.Create(data)
            ?? throw new InvalidDataException("The generated PNG could not be decoded.");

        codec.Info.Width.Should().Be(expectedWidth);
        codec.Info.Height.Should().Be(expectedHeight);
    }

    [Fact]
    public async Task Pdf_With_Multi_Page_Layout_Produces_Correct_Page_Count()
    {
        LayoutResult layout = CreateTwoPageLayout();

        byte[] pdf = await _pdfGenerator.GeneratePdfAsync(TestDataSeeder.CreateHeader(), layout);

        layout.Pages.Should().HaveCount(2);
        _pdfGenerator.CountPages(pdf).Should().Be(2);
    }

    [Fact]
    public async Task Png_Pages_With_Two_Page_Layout_Returns_Two_Images()
    {
        LayoutResult layout = CreateTwoPageLayout();

        IReadOnlyList<byte[]> pages = await _imageGenerator.GeneratePngPagesAsync(
            TestDataSeeder.CreateHeader(),
            layout,
            dpi: 300);

        pages.Should().HaveCount(2);
        pages.Should().OnlyContain(page => HasPngSignature(page));
    }

    public void Dispose()
    {
        _fonts.Dispose();
    }

    private LayoutResult CreateSeedLayout()
    {
        return new LayoutEngine().CreateLayout(
            TestDataSeeder.CreateSeedGroups(),
            TestDataSeeder.CreateSettings(),
            _measurer);
    }

    private LayoutResult CreateTwoPageLayout()
    {
        return new LayoutEngine().CreateLayout(
            TestDataSeeder.CreateGroups(4, 40),
            TestDataSeeder.CreateSettings(),
            _measurer);
    }

    private static bool HasPngSignature(byte[] bytes)
    {
        return bytes.Length >= PngSignature.Length
            && bytes.AsSpan(0, PngSignature.Length).SequenceEqual(PngSignature);
    }
}
