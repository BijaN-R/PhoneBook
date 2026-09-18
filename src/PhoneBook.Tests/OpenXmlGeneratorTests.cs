// FILE: src/PhoneBook.Tests/OpenXmlGeneratorTests.cs
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Validation;
using DocumentFormat.OpenXml.Wordprocessing;
using FluentAssertions;
using PhoneBook.Core.Layout;
using PhoneBook.Domain.Entities;
using PhoneBook.Export.OpenXml.OpenXml;
using Xunit;

namespace PhoneBook.Tests;

public sealed class OpenXmlGeneratorTests
{
    private readonly TestDataSeeder.DeterministicTextMeasurer _measurer = new();

    [Fact]
    public void Group_Table_Uses_Thirty_Seventy_Column_Proportions()
    {
        PhoneBookGroup group = TestDataSeeder.CreateSeedGroups().First();
        Table table = TableFactory.CreateGroupTable(
            group,
            TestDataSeeder.CreateSettings(),
            _measurer);
        GridColumn[] gridColumns = table.GetFirstChild<TableGrid>()!
            .Elements<GridColumn>()
            .ToArray();
        TableCell[] entryCells = table.Elements<TableRow>()
            .Skip(1)
            .First()
            .Elements<TableCell>()
            .ToArray();

        int nameGridWidth = int.Parse(gridColumns[0].Width!.Value!);
        int extensionGridWidth = int.Parse(gridColumns[1].Width!.Value!);
        double extensionRatio = (double)extensionGridWidth / (nameGridWidth + extensionGridWidth);
        TableCellWidth nameCellWidth = entryCells[0]
            .TableCellProperties!
            .GetFirstChild<TableCellWidth>()!;
        TableCellWidth extensionCellWidth = entryCells[1]
            .TableCellProperties!
            .GetFirstChild<TableCellWidth>()!;

        extensionRatio.Should().BeApproximately(0.30, 0.0001);
        nameCellWidth.Type!.Value.Should().Be(TableWidthUnitValues.Pct);
        nameCellWidth.Width!.Value.Should().Be("3500");
        extensionCellWidth.Type!.Value.Should().Be(TableWidthUnitValues.Pct);
        extensionCellWidth.Width!.Value.Should().Be("1500");
    }

    [Fact]
    public async Task Seed_Dataset_Generates_A_Valid_One_Page_Docx()
    {
        IReadOnlyList<PhoneBookGroup> groups = TestDataSeeder.CreateSeedGroups();
        AppSettings settings = TestDataSeeder.CreateSettings();
        LayoutResult layout = new LayoutEngine().CreateLayout(groups, settings, _measurer);
        byte[] bytes = await CreateGenerator().GenerateAsync(
            TestDataSeeder.CreateHeader(),
            groups,
            layout,
            settings);
        string temporaryPath = CreateTemporaryDocxPath();

        try
        {
            await File.WriteAllBytesAsync(temporaryPath, bytes);
            using WordprocessingDocument document = WordprocessingDocument.Open(
                temporaryPath,
                isEditable: false);
            MainDocumentPart mainPart = document.MainDocumentPart
                ?? throw new InvalidDataException("The generated DOCX has no main document part.");
            Document mainDocument = mainPart.Document
                ?? throw new InvalidDataException("The generated DOCX has no main document.");
            Body body = mainDocument.Body
                ?? throw new InvalidDataException("The generated DOCX has no document body.");
            List<Table> motherTables = body.Elements<Table>()
                .Where(table => table.Descendants<Table>().Any())
                .ToList();
            List<ValidationErrorInfo> errors = new OpenXmlValidator(FileFormatVersions.Office2019)
                .Validate(document)
                .ToList();

            errors.Should().BeEmpty();
            motherTables.Should().ContainSingle();
            body.InnerText.Should().Contain(TestDataSeeder.CreateHeader().Title);
            motherTables[0].Descendants<Table>().Should().HaveCount(groups.Count);
        }
        finally
        {
            File.Delete(temporaryPath);
        }
    }

    [Fact]
    public async Task Docx_With_N_Page_Layout_Contains_N_Minus_1_PageBreaks()
    {
        IReadOnlyList<PhoneBookGroup> groups = TestDataSeeder.CreateGroups(4, 40);
        AppSettings settings = TestDataSeeder.CreateSettings();
        LayoutResult layout = new LayoutEngine().CreateLayout(groups, settings, _measurer);
        layout.Pages.Should().HaveCount(2);
        byte[] bytes = await CreateGenerator().GenerateAsync(
            TestDataSeeder.CreateHeader(),
            groups,
            layout,
            settings);
        string temporaryPath = CreateTemporaryDocxPath();

        try
        {
            await File.WriteAllBytesAsync(temporaryPath, bytes);
            using WordprocessingDocument document = WordprocessingDocument.Open(
                temporaryPath,
                isEditable: false);
            MainDocumentPart mainPart = document.MainDocumentPart
                ?? throw new InvalidDataException("The generated DOCX has no main document part.");
            Document mainDocument = mainPart.Document
                ?? throw new InvalidDataException("The generated DOCX has no main document.");
            List<Break> pageBreaks = mainDocument
                .Descendants<Break>()
                .Where(item => item.Type?.Value == BreakValues.Page)
                .ToList();
            List<ValidationErrorInfo> errors = new OpenXmlValidator(FileFormatVersions.Office2019)
                .Validate(document)
                .ToList();

            pageBreaks.Should().HaveCount(layout.Pages.Count - 1);
            errors.Should().BeEmpty();
        }
        finally
        {
            File.Delete(temporaryPath);
        }
    }

    private OpenXmlPhoneBookGenerator CreateGenerator()
    {
        return new OpenXmlPhoneBookGenerator(_measurer, TestDataSeeder.GetFontsPath());
    }

    private static string CreateTemporaryDocxPath()
    {
        return Path.Combine(Path.GetTempPath(), $"phonebook-{Guid.NewGuid():N}.docx");
    }
}
