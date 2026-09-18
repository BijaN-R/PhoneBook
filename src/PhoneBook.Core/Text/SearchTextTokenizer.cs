namespace PhoneBook.Core.Text;

public static class SearchTextTokenizer
{
    public static string[] NormalizeAndTokenize(string input)
    {
        ArgumentNullException.ThrowIfNull(input);
        return TokenizeNormalized(PersianTextNormalizer.NormalizeForSearch(input));
    }

    public static string[] TokenizeNormalized(string normalizedText)
    {
        ArgumentNullException.ThrowIfNull(normalizedText);

        List<string> tokens = [];
        int start = -1;
        for (int index = 0; index <= normalizedText.Length; index++)
        {
            bool tokenCharacter = index < normalizedText.Length
                && char.IsLetterOrDigit(normalizedText[index]);
            if (tokenCharacter && start < 0)
            {
                start = index;
            }
            else if (!tokenCharacter && start >= 0)
            {
                tokens.Add(normalizedText[start..index]);
                start = -1;
            }
        }

        return [.. tokens];
    }

    public static bool IsNumericStyle(string normalizedText)
    {
        ArgumentNullException.ThrowIfNull(normalizedText);

        bool hasDigit = false;
        foreach (char character in normalizedText)
        {
            if (character is >= '0' and <= '9')
            {
                hasDigit = true;
                continue;
            }

            if (!char.IsWhiteSpace(character) && character is not '-' and not '/' and not '.')
            {
                return false;
            }
        }

        return hasDigit;
    }
}
