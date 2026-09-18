using PhoneBook.Domain.Entities;

namespace PhoneBook.Application.Models;

public sealed record SettingsUpdateModel(
    int Id,
    long ExpectedRevision,
    double PageWidthMm,
    double PageHeightMm,
    double MarginTopMm,
    double MarginBottomMm,
    double MarginLeftMm,
    double MarginRightMm,
    double GroupGapMm,
    double CellPaddingMm,
    string PrimaryFontFamily,
    bool UsePersianDigits,
    double MinFontSizePt,
    double DefaultFontSizePt,
    double HeaderFontSizePt,
    double GroupHeaderFontSizePt,
    int PriorityTopLimit)
{
    public static SettingsUpdateModel FromEntity(AppSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);
        return new(
            settings.Id,
            settings.Revision,
            settings.PageWidthMm,
            settings.PageHeightMm,
            settings.MarginTopMm,
            settings.MarginBottomMm,
            settings.MarginLeftMm,
            settings.MarginRightMm,
            settings.GroupGapMm,
            settings.CellPaddingMm,
            settings.PrimaryFontFamily,
            settings.UsePersianDigits,
            settings.MinFontSizePt,
            settings.DefaultFontSizePt,
            settings.HeaderFontSizePt,
            settings.GroupHeaderFontSizePt,
            settings.PriorityTopLimit);
    }
}
