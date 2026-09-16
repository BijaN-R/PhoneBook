// FILE: src/PhoneBook.Core/Layout/LayoutDiagnostics.cs
using System.Globalization;
using System.Text;
using PhoneBook.Domain.Entities;

namespace PhoneBook.Core.Layout;

public static class LayoutDiagnostics
{
    private static readonly string[] ColumnNames = ["Right", "Middle", "Left"];

    public static string Dump(LayoutResult result)
    {
        ArgumentNullException.ThrowIfNull(result);

        StringBuilder report = new();

        for (int pageIndex = 0; pageIndex < result.Pages.Count; pageIndex++)
        {
            PageLayout page = result.Pages[pageIndex];
            report.Append("=== PAGE ")
                .Append(pageIndex + 1)
                .AppendLine(" ===");

            for (int columnIndex = 0; columnIndex < page.Columns.Count; columnIndex++)
            {
                double height = columnIndex < page.ColumnHeightsMm.Length
                    ? page.ColumnHeightsMm[columnIndex]
                    : 0;
                string columnName = columnIndex < ColumnNames.Length
                    ? ColumnNames[columnIndex]
                    : $"Column {columnIndex}";

                report.Append(columnName)
                    .Append(" [")
                    .Append(height.ToString("0.###", CultureInfo.InvariantCulture))
                    .AppendLine(" mm]");

                foreach (PlacedGroup placedGroup in page.Columns[columnIndex])
                {
                    report.Append("  Row ")
                        .Append(placedGroup.RowIndex + 1)
                        .Append(": ")
                        .Append(placedGroup.Group.Title)
                        .Append(" (")
                        .Append(placedGroup.HeightMm.ToString("0.###", CultureInfo.InvariantCulture))
                        .AppendLine(" mm)");
                }
            }

            report.AppendLine();
        }

        AppendSettings(report, result.EffectiveSettings);
        report.Append("LayoutFailed: ")
            .AppendLine(result.LayoutFailed.ToString(CultureInfo.InvariantCulture));
        report.Append("FailureReason: ")
            .AppendLine(result.FailureReason ?? "None");

        return report.ToString();
    }

    private static void AppendSettings(StringBuilder report, AppSettings settings)
    {
        report.AppendLine("=== EFFECTIVE SETTINGS ===");
        AppendSetting(report, nameof(settings.PageWidthMm), settings.PageWidthMm);
        AppendSetting(report, nameof(settings.PageHeightMm), settings.PageHeightMm);
        AppendSetting(report, nameof(settings.MarginTopMm), settings.MarginTopMm);
        AppendSetting(report, nameof(settings.MarginBottomMm), settings.MarginBottomMm);
        AppendSetting(report, nameof(settings.MarginLeftMm), settings.MarginLeftMm);
        AppendSetting(report, nameof(settings.MarginRightMm), settings.MarginRightMm);
        AppendSetting(report, nameof(settings.GroupGapMm), settings.GroupGapMm);
        AppendSetting(report, nameof(settings.CellPaddingMm), settings.CellPaddingMm);
        report.Append(nameof(settings.PrimaryFontFamily))
            .Append(": ")
            .AppendLine(settings.PrimaryFontFamily);
        report.Append(nameof(settings.UsePersianDigits))
            .Append(": ")
            .AppendLine(settings.UsePersianDigits.ToString(CultureInfo.InvariantCulture));
        AppendSetting(report, nameof(settings.MinFontSizePt), settings.MinFontSizePt);
        AppendSetting(report, nameof(settings.DefaultFontSizePt), settings.DefaultFontSizePt);
        AppendSetting(report, nameof(settings.HeaderFontSizePt), settings.HeaderFontSizePt);
        AppendSetting(report, nameof(settings.GroupHeaderFontSizePt), settings.GroupHeaderFontSizePt);
        report.Append(nameof(settings.PriorityTopLimit))
            .Append(": ")
            .AppendLine(settings.PriorityTopLimit.ToString(CultureInfo.InvariantCulture));
    }

    private static void AppendSetting(StringBuilder report, string name, double value)
    {
        report.Append(name)
            .Append(": ")
            .AppendLine(value.ToString("0.###", CultureInfo.InvariantCulture));
    }
}
