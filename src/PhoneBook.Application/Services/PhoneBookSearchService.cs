using PhoneBook.Application.Abstractions.Persistence;
using PhoneBook.Application.Models;
using PhoneBook.Core.Text;

namespace PhoneBook.Application.Services;

public sealed class PhoneBookSearchService(IPhoneBookRepository repository)
{
    private static readonly HashSet<string> TitleTokens = ["آقای", "خانم", "دکتر", "مهندس", "جناب", "سرکار"];
    private static readonly HashSet<string> NumericIntentTokens = ["داخلی", "شماره", "تلفن", "extension"];
    private readonly object _sync = new();
    private IndexedEntry[] _index = [];

    public async Task RefreshAsync(CancellationToken ct = default)
    {
        IReadOnlyList<PhoneBookSearchRecord> records = await repository.GetActiveSearchRecordsAsync(ct);
        IndexedEntry[] refreshed = records.Select(CreateIndexedEntry).ToArray();
        lock (_sync)
        {
            _index = refreshed;
        }
    }

    public IReadOnlyList<SearchHit> Search(string query)
    {
        string normalizedQuery = PersianTextNormalizer.NormalizeForSearch(query ?? string.Empty);
        IndexedEntry[] snapshot;
        lock (_sync)
        {
            snapshot = _index;
        }

        if (normalizedQuery.Length == 0)
        {
            return snapshot.OrderBy(x => x.Record.GroupDisplayOrder)
                .ThenBy(x => x.Record.EntryDisplayOrder)
                .ThenBy(x => x.NormalizedName, StringComparer.Ordinal)
                .ThenBy(x => x.NormalizedExtension, StringComparer.Ordinal)
                .ThenBy(x => x.Record.EntryId)
                .Select(x => CreateHit(x.Record, 0, SearchMatchKind.None)).ToArray();
        }

        string[] queryTokens = SearchTextTokenizer.TokenizeNormalized(normalizedQuery);
        if (queryTokens.Length == 0)
        {
            return [];
        }

        bool hasNumericToken = queryTokens.Any(IsNumericToken);
        string[] titleTokens = queryTokens.Where(TitleTokens.Contains).ToArray();
        string[] requiredTokens = queryTokens.Where(token => !TitleTokens.Contains(token))
            .Where(token => !(hasNumericToken && NumericIntentTokens.Contains(token))).ToArray();
        bool titleOnly = requiredTokens.Length == 0 && titleTokens.Length > 0;
        if (titleOnly)
        {
            requiredTokens = titleTokens;
            titleTokens = [];
        }

        List<RankedHit> matches = [];
        foreach (IndexedEntry candidate in snapshot)
        {
            MatchQuality[] qualities = new MatchQuality[requiredTokens.Length];
            bool accepted = true;
            for (int index = 0; index < requiredTokens.Length; index++)
            {
                qualities[index] = titleOnly
                    ? FindExactTitleMatch(requiredTokens[index], candidate)
                    : FindBestMatch(requiredTokens[index], candidate);
                if (!qualities[index].Accepted)
                {
                    accepted = false;
                    break;
                }
            }

            if (!accepted)
            {
                continue;
            }

            MatchQuality whole = FindWholeQueryExactMatch(normalizedQuery, candidate);
            int weakest = qualities.Length == 0 ? whole.Score : qualities.Min(x => x.Score);
            int aggregate = qualities.Sum(x => x.Score);
            SearchMatchKind kind = qualities.Length == 0
                ? whole.Kind
                : qualities.MaxBy(x => x.Score).Kind;
            if (whole.Accepted)
            {
                weakest = Math.Max(weakest, whole.Score);
                aggregate += whole.Score;
                kind = whole.Kind;
            }

            int titleBonus = titleTokens.Count(token => ContainsExactToken(token, candidate)) * 5;
            int score = weakest * 10_000 + aggregate + titleBonus;
            matches.Add(new(candidate, weakest, aggregate, titleBonus,
                CreateHit(candidate.Record, score, kind)));
        }

        return matches.OrderByDescending(x => x.WeakestScore)
            .ThenByDescending(x => x.AggregateScore)
            .ThenByDescending(x => x.TitleBonus)
            .ThenBy(x => x.Entry.Record.GroupDisplayOrder)
            .ThenBy(x => x.Entry.Record.EntryDisplayOrder)
            .ThenBy(x => x.Entry.NormalizedName, StringComparer.Ordinal)
            .ThenBy(x => x.Entry.NormalizedExtension, StringComparer.Ordinal)
            .ThenBy(x => x.Entry.Record.EntryId)
            .Select(x => x.Hit).ToArray();
    }

    private static IndexedEntry CreateIndexedEntry(PhoneBookSearchRecord record)
    {
        string name = PersianTextNormalizer.NormalizeForSearch(record.Name);
        string group = PersianTextNormalizer.NormalizeForSearch(record.GroupTitle);
        string extension = PersianTextNormalizer.NormalizeForSearch(record.Extension ?? string.Empty);
        return new(record, name, SearchTextTokenizer.TokenizeNormalized(name),
            group, SearchTextTokenizer.TokenizeNormalized(group), extension);
    }

    private static MatchQuality FindBestMatch(string token, IndexedEntry candidate)
    {
        bool numeric = IsNumericToken(token);
        MatchQuality best = CompareExtension(token, candidate.NormalizedExtension);
        best = Better(best, CompareTextField(token, candidate.NormalizedName, candidate.NameTokens,
            SearchMatchKind.ExactFullName, SearchMatchKind.ExactNameToken, numeric));
        return Better(best, CompareTextField(token, candidate.NormalizedGroupTitle, candidate.GroupTokens,
            SearchMatchKind.ExactFullGroupTitle, SearchMatchKind.ExactGroupToken, numeric));
    }

    private static MatchQuality CompareExtension(string token, string extension)
    {
        if (extension.Length == 0) return MatchQuality.None;
        if (extension.Equals(token, StringComparison.Ordinal)) return new(SearchMatchKind.ExactExtension, 1000);
        if (extension.StartsWith(token, StringComparison.Ordinal)) return new(SearchMatchKind.Prefix, 600);
        return extension.Contains(token, StringComparison.Ordinal)
            ? new(SearchMatchKind.Substring, 500) : MatchQuality.None;
    }

    private static MatchQuality CompareTextField(string token, string fullText, string[] targetTokens,
        SearchMatchKind exactFullKind, SearchMatchKind exactTokenKind, bool numeric)
    {
        if (fullText.Equals(token, StringComparison.Ordinal))
            return new(exactFullKind, exactFullKind == SearchMatchKind.ExactFullName ? 900 : 750);
        if (targetTokens.Contains(token, StringComparer.Ordinal))
            return new(exactTokenKind, exactTokenKind == SearchMatchKind.ExactNameToken ? 800 : 700);
        if (targetTokens.Any(x => x.StartsWith(token, StringComparison.Ordinal)))
            return new(SearchMatchKind.Prefix, 600);
        if (numeric) return MatchQuality.None;
        if (token.Length >= 3 && targetTokens.Any(x => x.Contains(token, StringComparison.Ordinal)))
            return new(SearchMatchKind.Substring, 500);

        int maximumDistance = token.Length switch { <= 2 => 0, <= 7 => 1, _ => 2 };
        if (maximumDistance == 0) return MatchQuality.None;

        MatchQuality best = MatchQuality.None;
        foreach (string target in targetTokens)
        {
            int distance = PersianFuzzyMatcher.OptimalStringAlignmentDistance(token, target);
            if (distance == 1)
            {
                best = Better(best, new(SearchMatchKind.FuzzyOneEdit, 400));
            }
            else if (distance == 2 && maximumDistance == 2
                && 1.0 - (double)distance / Math.Max(token.Length, target.Length) >= 0.80)
            {
                best = Better(best, new(SearchMatchKind.FuzzyTwoEdits, 250));
            }
        }
        return best;
    }

    private static MatchQuality FindWholeQueryExactMatch(string query, IndexedEntry candidate)
    {
        if (candidate.NormalizedExtension.Equals(query, StringComparison.Ordinal))
            return new(SearchMatchKind.ExactExtension, 1000);
        if (candidate.NormalizedName.Equals(query, StringComparison.Ordinal))
            return new(SearchMatchKind.ExactFullName, 900);
        return candidate.NormalizedGroupTitle.Equals(query, StringComparison.Ordinal)
            ? new(SearchMatchKind.ExactFullGroupTitle, 750) : MatchQuality.None;
    }

    private static MatchQuality FindExactTitleMatch(string title, IndexedEntry candidate)
    {
        if (candidate.NameTokens.Contains(title, StringComparer.Ordinal))
            return new(SearchMatchKind.ExactNameToken, 800);
        return candidate.GroupTokens.Contains(title, StringComparer.Ordinal)
            ? new(SearchMatchKind.ExactGroupToken, 700) : MatchQuality.None;
    }

    private static bool ContainsExactToken(string token, IndexedEntry candidate) =>
        candidate.NameTokens.Contains(token, StringComparer.Ordinal)
        || candidate.GroupTokens.Contains(token, StringComparer.Ordinal);

    private static bool IsNumericToken(string token) => SearchTextTokenizer.IsNumericStyle(token);
    private static MatchQuality Better(MatchQuality left, MatchQuality right) => right.Score > left.Score ? right : left;

    private static SearchHit CreateHit(PhoneBookSearchRecord record, int score, SearchMatchKind kind) =>
        new(record.EntryId, record.GroupId, record.GroupTitle, record.Name, record.Extension,
            record.GroupDisplayOrder, record.EntryDisplayOrder, score, kind);

    private sealed record IndexedEntry(PhoneBookSearchRecord Record, string NormalizedName,
        string[] NameTokens, string NormalizedGroupTitle, string[] GroupTokens, string NormalizedExtension);
    private readonly record struct MatchQuality(SearchMatchKind Kind, int Score)
    {
        public static MatchQuality None => new(SearchMatchKind.None, 0);
        public bool Accepted => Score > 0;
    }
    private sealed record RankedHit(IndexedEntry Entry, int WeakestScore, int AggregateScore,
        int TitleBonus, SearchHit Hit);
}
