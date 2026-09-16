// FILE: src/PhoneBook.Tests/LayoutEngineTests.cs
using FluentAssertions;
using PhoneBook.Core.Layout;
using PhoneBook.Domain.Entities;
using PhoneBook.Domain.Enums;
using Xunit;

namespace PhoneBook.Tests;

public sealed class LayoutEngineTests
{
    private readonly LayoutEngine _engine = new();
    private readonly TestDataSeeder.DeterministicTextMeasurer _measurer = new();

    [Fact]
    public void Priority_One_Two_And_Three_Land_At_The_Top_Of_Page_One_Columns()
    {
        IReadOnlyList<PhoneBookGroup> groups = TestDataSeeder.CreateSeedGroups();

        LayoutResult result = _engine.CreateLayout(groups, TestDataSeeder.CreateSettings(), _measurer);

        result.Pages[0].Columns[0][0].Group.Priority.Should().Be(1);
        result.Pages[0].Columns[1][0].Group.Priority.Should().Be(2);
        result.Pages[0].Columns[2][0].Group.Priority.Should().Be(3);
    }

    [Fact]
    public void Priority_Four_Lands_In_The_Second_Row_Of_Exactly_One_Page_One_Column()
    {
        LayoutResult result = _engine.CreateLayout(
            TestDataSeeder.CreateSeedGroups(),
            TestDataSeeder.CreateSettings(),
            _measurer);

        List<PlacedGroup> placements = result.Pages[0].Columns
            .SelectMany(column => column)
            .Where(item => item.Group.Priority == 4)
            .ToList();

        placements.Should().ContainSingle();
        placements[0].RowIndex.Should().Be(1);
    }

    [Fact]
    public void Seed_Dataset_Fits_On_One_Page_With_Default_Settings()
    {
        IReadOnlyList<PhoneBookGroup> groups = TestDataSeeder.CreateSeedGroups();

        LayoutResult result = _engine.CreateLayout(groups, TestDataSeeder.CreateSettings(), _measurer);

        result.LayoutFailed.Should().BeFalse();
        result.Pages.Should().ContainSingle();
        result.Pages[0].Columns.SelectMany(column => column).Should().HaveCount(groups.Count);
    }

    [Fact]
    public void Preferred_Column_Is_Respected_When_Placement_Costs_Are_Otherwise_Equal()
    {
        PhoneBookGroup preferred = TestDataSeeder.CreateGroups(1, 3)[0];
        preferred.PreferredColumn = ColumnPosition.Left;

        LayoutResult result = _engine.CreateLayout(
            [preferred],
            TestDataSeeder.CreateSettings(),
            _measurer);

        result.Pages[0].Columns[2].Should().ContainSingle(item => item.Group == preferred);
        result.Pages[0].Columns[0].Should().BeEmpty();
        result.Pages[0].Columns[1].Should().BeEmpty();
    }

    [Fact]
    public void Cost_Function_Balances_A_Set_Of_Equal_Groups()
    {
        IReadOnlyList<PhoneBookGroup> groups = TestDataSeeder.CreateGroups(6, 4);

        LayoutResult result = _engine.CreateLayout(groups, TestDataSeeder.CreateSettings(), _measurer);

        result.Pages.Should().ContainSingle();
        result.Pages[0].Columns.Select(column => column.Count).Should().Equal(2, 2, 2);
        Variance(result.Pages[0].ColumnHeightsMm).Should().BeApproximately(0, 0.000001);
    }

    [Fact]
    public void Cost_Function_Reduces_Variance_For_An_Uneven_Handcrafted_Set()
    {
        int[] rowCounts = [12, 10, 8, 6, 4, 2];
        List<PhoneBookGroup> groups = rowCounts
            .Select((rows, index) => TestDataSeeder.CreateGroups(1, rows)[0])
            .ToList();
        for (int index = 0; index < groups.Count; index++)
        {
            groups[index].Id = index + 1;
            groups[index].Title = $"نامتوازن {index + 1}";
            groups[index].DisplayOrder = index + 1;
        }

        AppSettings settings = TestDataSeeder.CreateSettings();
        HeightEstimator estimator = new();
        double[] naiveHeights =
        [
            groups.Sum(group => estimator.EstimateGroupHeightMm(group, settings, _measurer))
                + ((groups.Count - 1) * settings.GroupGapMm),
            0,
            0
        ];

        LayoutResult result = _engine.CreateLayout(groups, settings, _measurer);

        result.Pages.Should().ContainSingle();
        Variance(result.Pages[0].ColumnHeightsMm).Should().BeLessThan(Variance(naiveHeights));
    }

    [Fact]
    public void Layout_With_Huge_Dataset_Produces_Multiple_Pages()
    {
        IReadOnlyList<PhoneBookGroup> groups = TestDataSeeder.CreateGroups(
            groupCount: 40,
            rowsPerGroup: 18,
            includePriorities: true);

        LayoutResult result = _engine.CreateLayout(groups, TestDataSeeder.CreateSettings(), _measurer);

        result.Pages.Count.Should().BeGreaterThanOrEqualTo(2);
        result.LayoutFailed.Should().BeFalse();
        result.Pages[0].Columns.SelectMany(column => column)
            .Where(item => item.Group.Priority is >= 1 and <= 4)
            .Select(item => item.Group.Priority)
            .Should().BeEquivalentTo([1, 2, 3, 4]);

        int highestPageOneOrder = result.Pages[0].Columns
            .SelectMany(column => column)
            .Max(item => item.Group.DisplayOrder);
        result.Pages.Skip(1)
            .SelectMany(page => page.Columns)
            .SelectMany(column => column)
            .Should().OnlyContain(item => item.Group.DisplayOrder > highestPageOneOrder);
    }

    [Fact]
    public void Layout_With_Extreme_Dataset_Produces_Many_Pages()
    {
        IReadOnlyList<PhoneBookGroup> groups = TestDataSeeder.CreateGroups(
            groupCount: 500,
            rowsPerGroup: 20);
        AppSettings settings = TestDataSeeder.CreateSettings();

        LayoutResult result = _engine.CreateLayout(groups, settings, _measurer);

        result.Pages.Count.Should().BeGreaterThanOrEqualTo(3);
        result.LayoutFailed.Should().BeFalse();
        double printableHeight = settings.PageHeightMm - settings.MarginTopMm - settings.MarginBottomMm;
        result.Pages.SelectMany(page => page.ColumnHeightsMm)
            .Should().OnlyContain(height => height <= printableHeight + 0.0001);
    }

    [Fact]
    public void Layout_Order_Is_Preserved_Across_Pages()
    {
        IReadOnlyList<PhoneBookGroup> groups = TestDataSeeder.CreateGroups(
            groupCount: 12,
            rowsPerGroup: 20);

        LayoutResult result = _engine.CreateLayout(groups, TestDataSeeder.CreateSettings(), _measurer);
        int[] traversalOrder = result.Pages
            .SelectMany(page => page.Columns)
            .SelectMany(column => column)
            .Select(item => item.Group.DisplayOrder)
            .ToArray();

        result.Pages.Count.Should().BeGreaterThanOrEqualTo(2);
        traversalOrder.Should().Equal(Enumerable.Range(1, groups.Count));
    }

    private static double Variance(IEnumerable<double> values)
    {
        double[] materialized = values.ToArray();
        double mean = materialized.Average();
        return materialized.Average(value => Math.Pow(value - mean, 2));
    }
}
