// FILE: src/PhoneBook.Core/Text/PersianTextNormalizer.cs
using System.Text;

namespace PhoneBook.Core.Text;

public static class PersianTextNormalizer
{
    private const char ZeroWidthNonJoiner = '\u200C';

    public static string Normalize(string input)
    {
        ArgumentNullException.ThrowIfNull(input);

        StringBuilder result = new(input.Length);
        bool previousWasWhitespace = true;

        foreach (char sourceCharacter in input.Normalize(NormalizationForm.FormC))
        {
            if (sourceCharacter == ZeroWidthNonJoiner)
            {
                continue;
            }

            char character = sourceCharacter switch
            {
                '\u064A' => '\u06CC', // Arabic yeh -> Persian yeh
                '\u0643' => '\u06A9', // Arabic kaf -> Persian kaf
                '\u06C0' => '\u0647', // heh with yeh above -> heh
                _ => sourceCharacter
            };

            if (char.IsWhiteSpace(character))
            {
                if (!previousWasWhitespace)
                {
                    result.Append(' ');
                    previousWasWhitespace = true;
                }

                continue;
            }

            result.Append(character);
            previousWasWhitespace = false;
        }

        if (result.Length > 0 && result[^1] == ' ')
        {
            result.Length--;
        }

        return result.ToString();
    }

    public static string ToPersianDigits(string input)
    {
        ArgumentNullException.ThrowIfNull(input);

        return string.Create(input.Length, input, static (destination, source) =>
        {
            for (int index = 0; index < source.Length; index++)
            {
                char character = source[index];
                destination[index] = character is >= '0' and <= '9'
                    ? (char)('\u06F0' + character - '0')
                    : character;
            }
        });
    }

    public static string ToEnglishDigits(string input)
    {
        ArgumentNullException.ThrowIfNull(input);

        return string.Create(input.Length, input, static (destination, source) =>
        {
            for (int index = 0; index < source.Length; index++)
            {
                char character = source[index];
                destination[index] = character switch
                {
                    >= '\u06F0' and <= '\u06F9' => (char)('0' + character - '\u06F0'),
                    >= '\u0660' and <= '\u0669' => (char)('0' + character - '\u0660'),
                    _ => character
                };
            }
        });
    }

    public static string NormalizeForSearch(string input)
    {
        return ToEnglishDigits(Normalize(input)).ToLowerInvariant();
    }
}
