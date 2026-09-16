// FILE: src/PhoneBook.Core/Layout/HeightEstimator.cs
using PhoneBook.Domain.Entities;

namespace PhoneBook.Core.Layout;

public sealed class HeightEstimator
{
    public double EstimateGroupHeightMm(
        PhoneBookGroup group,
        AppSettings settings,
        ITextMeasurer measurer)
    {
        ArgumentNullException.ThrowIfNull(group);
        ArgumentNullException.ThrowIfNull(settings);
        ArgumentNullException.ThrowIfNull(measurer);

        double headerHeight = measurer.MeasureTextHeightMm(
            settings.PrimaryFontFamily,
            settings.GroupHeaderFontSizePt) + (2 * settings.CellPaddingMm);
        double rowHeight = EstimateRowHeightMm(settings, measurer);

        // Table borders are drawn inside the calculated cell boxes and add no external height.
        return headerHeight + (group.Entries.Count * rowHeight);
    }

    public double EstimateRowHeightMm(AppSettings settings, ITextMeasurer measurer)
    {
        ArgumentNullException.ThrowIfNull(settings);
        ArgumentNullException.ThrowIfNull(measurer);

        return measurer.MeasureTextHeightMm(
            settings.PrimaryFontFamily,
            settings.DefaultFontSizePt) + (2 * settings.CellPaddingMm);
    }
}
