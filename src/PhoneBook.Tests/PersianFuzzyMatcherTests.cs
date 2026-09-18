// FILE: src/PhoneBook.Tests/PersianFuzzyMatcherTests.cs
using FluentAssertions;
using PhoneBook.Core.Text;
using Xunit;

namespace PhoneBook.Tests;

public sealed class PersianFuzzyMatcherTests
{
    [Theory]
    [InlineData("کتاب", "کتاب", true)]
    [InlineData("کتا", "کتابخانه", true)]
    [InlineData("تاب", "کتابخانه", true)]
    [InlineData("کتاب", "کباب", true)]
    [InlineData("کتاب", "خراب", false)]
    [InlineData("کتاب", "سلام", false)]
    public void IsMatch_Handles_Exact_Prefix_Substring_And_Edit_Distances(
        string query,
        string target,
        bool expected)
    {
        PersianFuzzyMatcher.IsMatch(query, target).Should().Be(expected);
    }

    [Fact]
    public void LevenshteinDistance_Distinguishes_Two_From_Three_Edits()
    {
        PersianFuzzyMatcher.LevenshteinDistance("کتاب", "خراب").Should().Be(2);
        PersianFuzzyMatcher.LevenshteinDistance("کتاب", "سلام").Should().Be(3);
    }

    [Fact]
    public void OptimalStringAlignment_Counts_Adjacent_Transposition_As_One_Edit()
    {
        PersianFuzzyMatcher.OptimalStringAlignmentDistance("کتاب", "کتبا").Should().Be(1);
    }

    [Fact]
    public void Empty_Query_Matches_Every_Target()
    {
        PersianFuzzyMatcher.IsMatch(string.Empty, "هر مقدار").Should().BeTrue();
    }

    [Fact]
    public void Normalized_Persian_Query_Matches_Persian_Target()
    {
        string query = PersianTextNormalizer.NormalizeForSearch("كريمي");
        string target = PersianTextNormalizer.NormalizeForSearch("آقای کریمی راد");

        PersianFuzzyMatcher.IsMatch(query, target).Should().BeTrue();
    }

    [Fact]
    public void Normalized_Mixed_Digits_Match()
    {
        string query = PersianTextNormalizer.NormalizeForSearch("123");
        string target = PersianTextNormalizer.NormalizeForSearch("داخلی ١٢۳");

        PersianFuzzyMatcher.IsMatch(query, target).Should().BeTrue();
    }

    [Fact]
    public void Numeric_Tokens_Are_Not_Fuzzy_Matched()
    {
        PersianFuzzyMatcher.IsMatch("188", "189").Should().BeFalse();
    }
}
