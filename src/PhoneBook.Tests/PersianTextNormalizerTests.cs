// FILE: src/PhoneBook.Tests/PersianTextNormalizerTests.cs
using FluentAssertions;
using PhoneBook.Core.Text;
using Xunit;

namespace PhoneBook.Tests;

public sealed class PersianTextNormalizerTests
{
    [Fact]
    public void English_And_Persian_Digits_Round_Trip()
    {
        const string english = "Phone 0123456789";

        string persian = PersianTextNormalizer.ToPersianDigits(english);
        string roundTrip = PersianTextNormalizer.ToEnglishDigits(persian);

        persian.Should().Be("Phone ۰۱۲۳۴۵۶۷۸۹");
        roundTrip.Should().Be(english);
    }

    [Fact]
    public void Normalize_Unifies_Arabic_And_Persian_Yeh_And_Kaf()
    {
        string normalized = PersianTextNormalizer.Normalize("ي ی ك ک");

        normalized.Should().Be("ی ی ک ک");
    }

    [Fact]
    public void Normalize_Removes_Zero_Width_Non_Joiner()
    {
        string normalized = PersianTextNormalizer.Normalize("نرم\u200Cافزار");

        normalized.Should().Be("نرمافزار");
    }

    [Fact]
    public void NormalizeForSearch_Is_Idempotent()
    {
        const string source = "  علي\u200Cرضا   كريمي ١٢۳  ";

        string once = PersianTextNormalizer.NormalizeForSearch(source);
        string twice = PersianTextNormalizer.NormalizeForSearch(once);

        twice.Should().Be(once);
        once.Should().Be("علیرضا کریمی 123");
    }
}
