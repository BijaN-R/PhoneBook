// FILE: src/PhoneBook.Tests/HeightEstimatorTests.cs
using FluentAssertions;
using PhoneBook.Core.Layout;
using PhoneBook.Domain.Entities;
using Xunit;

namespace PhoneBook.Tests;

public sealed class HeightEstimatorTests
{
    private const double MillimetersPerPoint = 25.4 / 72.0;
    private readonly HeightEstimator _estimator = new();
    private readonly TestDataSeeder.DeterministicTextMeasurer _measurer = new();

    [Fact]
    public void Row_Height_Equals_Font_Height_Plus_Two_Sided_Padding()
    {
        AppSettings settings = TestDataSeeder.CreateSettings();
        double expected = (settings.DefaultFontSizePt * MillimetersPerPoint)
            + (2 * settings.CellPaddingMm);

        double actual = _estimator.EstimateRowHeightMm(settings, _measurer);

        actual.Should().BeApproximately(expected, 0.000001);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(7)]
    public void Group_Height_Equals_Header_Plus_N_Rows(int rowCount)
    {
        AppSettings settings = TestDataSeeder.CreateSettings();
        PhoneBookGroup group = TestDataSeeder.CreateGroups(1, rowCount)[0];
        double rowHeight = _estimator.EstimateRowHeightMm(settings, _measurer);
        double headerHeight = (settings.GroupHeaderFontSizePt * MillimetersPerPoint)
            + (2 * settings.CellPaddingMm);

        double actual = _estimator.EstimateGroupHeightMm(group, settings, _measurer);

        actual.Should().BeApproximately(headerHeight + (rowCount * rowHeight), 0.000001);
    }
}
