// FILE: src/PhoneBook.Domain/Entities/AppSettings.cs
namespace PhoneBook.Domain.Entities;

public sealed class AppSettings
{
    public int Id { get; set; }

    public long Revision { get; set; } = 1;

    public double PageWidthMm { get; set; } = 210;

    public double PageHeightMm { get; set; } = 297;

    public double MarginTopMm { get; set; } = 10;

    public double MarginBottomMm { get; set; } = 10;

    public double MarginLeftMm { get; set; } = 8;

    public double MarginRightMm { get; set; } = 8;

    public double GroupGapMm { get; set; } = 2.5;

    public double CellPaddingMm { get; set; } = 1.2;

    public string PrimaryFontFamily { get; set; } = "Vazirmatn";

    public bool UsePersianDigits { get; set; } = true;

    public double MinFontSizePt { get; set; } = 7;

    public double DefaultFontSizePt { get; set; } = 9;

    public double HeaderFontSizePt { get; set; } = 16;

    public double GroupHeaderFontSizePt { get; set; } = 10;

    public int PriorityTopLimit { get; set; } = 3;
}
