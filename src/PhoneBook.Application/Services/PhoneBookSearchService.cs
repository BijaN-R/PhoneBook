using PhoneBook.Application.Abstractions.Persistence;
using PhoneBook.Application.Models;
using PhoneBook.Core.Text;

namespace PhoneBook.Application.Services;

public sealed class PhoneBookSearchService(IPhoneBookRepository repository)
{
    private readonly object _sync = new();
    private IndexedHit[] _index = [];

    public async Task RefreshAsync(CancellationToken ct = default)
    {
        IReadOnlyList<PhoneBookSearchRecord> records = await repository.GetActiveSearchRecordsAsync(ct);
        IndexedHit[] refreshed = records
            .Select(record =>
            {
                SearchHit hit = new(
                    record.GroupId,
                    record.GroupTitle,
                    record.Name,
                    record.Extension);
                return new IndexedHit(
                    hit,
                    PersianTextNormalizer.NormalizeForSearch(hit.GroupTitle),
                    PersianTextNormalizer.NormalizeForSearch(hit.Name),
                    PersianTextNormalizer.NormalizeForSearch(hit.Extension ?? string.Empty));
            })
            .ToArray();

        lock (_sync)
        {
            _index = refreshed;
        }
    }

    public IEnumerable<SearchHit> Search(string query)
    {
        string normalizedQuery = PersianTextNormalizer.NormalizeForSearch(query ?? string.Empty);
        IndexedHit[] snapshot;
        lock (_sync)
        {
            snapshot = _index;
        }

        if (normalizedQuery.Length == 0)
        {
            return snapshot.Select(item => item.Hit).ToArray();
        }

        return snapshot
            .Where(item => IsMatch(normalizedQuery, item.GroupTitle)
                || IsMatch(normalizedQuery, item.Name)
                || IsMatch(normalizedQuery, item.Extension))
            .Select(item => item.Hit)
            .ToArray();
    }

    private static bool IsMatch(string query, string target)
    {
        if (PersianFuzzyMatcher.IsMatch(query, target))
        {
            return true;
        }

        return target.Split(' ', StringSplitOptions.RemoveEmptyEntries)
            .Any(word => PersianFuzzyMatcher.IsMatch(query, word));
    }

    private sealed record IndexedHit(
        SearchHit Hit,
        string GroupTitle,
        string Name,
        string Extension);
}
