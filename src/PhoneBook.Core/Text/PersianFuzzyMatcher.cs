// FILE: src/PhoneBook.Core/Text/PersianFuzzyMatcher.cs
namespace PhoneBook.Core.Text;

public static class PersianFuzzyMatcher
{
    public static int LevenshteinDistance(string a, string b)
    {
        ArgumentNullException.ThrowIfNull(a);
        ArgumentNullException.ThrowIfNull(b);

        if (a.Length > b.Length)
        {
            (a, b) = (b, a);
        }

        if (a.Length == 0)
        {
            return b.Length;
        }

        int[] previous = new int[a.Length + 1];
        int[] current = new int[a.Length + 1];

        for (int column = 0; column <= a.Length; column++)
        {
            previous[column] = column;
        }

        for (int row = 1; row <= b.Length; row++)
        {
            current[0] = row;

            for (int column = 1; column <= a.Length; column++)
            {
                int substitutionCost = a[column - 1] == b[row - 1] ? 0 : 1;
                current[column] = Math.Min(
                    Math.Min(current[column - 1] + 1, previous[column] + 1),
                    previous[column - 1] + substitutionCost);
            }

            (previous, current) = (current, previous);
        }

        return previous[a.Length];
    }

    public static bool IsMatch(string normalizedQuery, string normalizedTarget)
    {
        ArgumentNullException.ThrowIfNull(normalizedQuery);
        ArgumentNullException.ThrowIfNull(normalizedTarget);

        if (normalizedQuery.Length == 0)
        {
            return true;
        }

        return normalizedTarget.StartsWith(normalizedQuery, StringComparison.Ordinal)
            || normalizedTarget.Contains(normalizedQuery, StringComparison.Ordinal)
            || LevenshteinDistance(normalizedQuery, normalizedTarget) <= 2;
    }
}
