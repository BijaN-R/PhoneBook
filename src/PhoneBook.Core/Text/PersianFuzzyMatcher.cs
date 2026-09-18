// FILE: src/PhoneBook.Core/Text/PersianFuzzyMatcher.cs
namespace PhoneBook.Core.Text;

public static class PersianFuzzyMatcher
{
    public static int OptimalStringAlignmentDistance(string a, string b)
    {
        ArgumentNullException.ThrowIfNull(a);
        ArgumentNullException.ThrowIfNull(b);

        int[,] distances = new int[a.Length + 1, b.Length + 1];
        for (int row = 0; row <= a.Length; row++)
        {
            distances[row, 0] = row;
        }

        for (int column = 0; column <= b.Length; column++)
        {
            distances[0, column] = column;
        }

        for (int row = 1; row <= a.Length; row++)
        {
            for (int column = 1; column <= b.Length; column++)
            {
                int substitutionCost = a[row - 1] == b[column - 1] ? 0 : 1;
                int distance = Math.Min(
                    Math.Min(distances[row - 1, column] + 1, distances[row, column - 1] + 1),
                    distances[row - 1, column - 1] + substitutionCost);

                if (row > 1
                    && column > 1
                    && a[row - 1] == b[column - 2]
                    && a[row - 2] == b[column - 1])
                {
                    distance = Math.Min(distance, distances[row - 2, column - 2] + 1);
                }

                distances[row, column] = distance;
            }
        }

        return distances[a.Length, b.Length];
    }

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

        if (normalizedTarget.StartsWith(normalizedQuery, StringComparison.Ordinal)
            || (normalizedQuery.Length >= 3
                && normalizedTarget.Contains(normalizedQuery, StringComparison.Ordinal)))
        {
            return true;
        }

        if (SearchTextTokenizer.IsNumericStyle(normalizedQuery)
            || normalizedQuery.Any(char.IsWhiteSpace))
        {
            return false;
        }

        int maximumDistance = normalizedQuery.Length switch
        {
            <= 2 => 0,
            <= 7 => 1,
            _ => 2
        };
        int distance = OptimalStringAlignmentDistance(normalizedQuery, normalizedTarget);
        if (distance > maximumDistance)
        {
            return false;
        }

        return distance < 2
            || 1.0 - (double)distance / Math.Max(normalizedQuery.Length, normalizedTarget.Length) >= 0.80;
    }
}
