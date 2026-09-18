using System.Globalization;
using System.Text;

namespace PhoneBook.Export.Image.Skia;

internal static class BidirectionalText
{
    public static IReadOnlyList<DirectionalTextRun> GetVisualRuns(string text)
    {
        ArgumentNullException.ThrowIfNull(text);

        if (text.Length == 0)
        {
            return [];
        }

        List<DirectionalTextRun> logicalRuns = [];
        StringBuilder currentText = new();
        TextDirection? currentDirection = null;
        TextDirection? paragraphDirection = null;

        foreach (Rune rune in text.EnumerateRunes())
        {
            TextDirection? runeDirection = GetStrongDirection(rune);
            if (runeDirection is null)
            {
                currentText.Append(rune.ToString());
                continue;
            }

            paragraphDirection ??= runeDirection;
            if (currentDirection is not null && currentDirection != runeDirection)
            {
                logicalRuns.Add(new DirectionalTextRun(currentText.ToString(), currentDirection.Value));
                currentText.Clear();
            }

            currentDirection = runeDirection;
            currentText.Append(rune.ToString());
        }

        TextDirection resolvedDirection = currentDirection ?? paragraphDirection ?? TextDirection.LeftToRight;
        logicalRuns.Add(new DirectionalTextRun(currentText.ToString(), resolvedDirection));

        if (paragraphDirection == TextDirection.RightToLeft)
        {
            logicalRuns.Reverse();
        }

        return logicalRuns;
    }

    private static TextDirection? GetStrongDirection(Rune rune)
    {
        if (rune.Value == 0x200E)
        {
            return TextDirection.LeftToRight;
        }

        if (rune.Value == 0x200F)
        {
            return TextDirection.RightToLeft;
        }

        UnicodeCategory category = Rune.GetUnicodeCategory(rune);
        if (category == UnicodeCategory.DecimalDigitNumber)
        {
            return TextDirection.LeftToRight;
        }

        if (category is not (UnicodeCategory.UppercaseLetter
            or UnicodeCategory.LowercaseLetter
            or UnicodeCategory.TitlecaseLetter
            or UnicodeCategory.ModifierLetter
            or UnicodeCategory.OtherLetter))
        {
            return null;
        }

        return IsRightToLeftScript(rune.Value)
            ? TextDirection.RightToLeft
            : TextDirection.LeftToRight;
    }

    private static bool IsRightToLeftScript(int value)
    {
        return value is >= 0x0590 and <= 0x08FF
            or >= 0xFB1D and <= 0xFDFF
            or >= 0xFE70 and <= 0xFEFF
            or >= 0x1EE00 and <= 0x1EEFF;
    }
}

internal readonly record struct DirectionalTextRun(string Text, TextDirection Direction);

internal enum TextDirection
{
    LeftToRight,
    RightToLeft
}
