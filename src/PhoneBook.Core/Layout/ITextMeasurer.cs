// FILE: src/PhoneBook.Core/Layout/ITextMeasurer.cs
namespace PhoneBook.Core.Layout;

public interface ITextMeasurer
{
    double MeasureTextWidthMm(string text, string fontFamily, double fontSizePt);

    double MeasureTextHeightMm(string fontFamily, double fontSizePt);
}
