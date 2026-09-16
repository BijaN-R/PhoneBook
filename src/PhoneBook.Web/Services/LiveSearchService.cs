// FILE: src/PhoneBook.Web/Services/LiveSearchService.cs
using Microsoft.EntityFrameworkCore;
using PhoneBook.Core.Text;
using PhoneBook.Infrastructure.Data;

namespace PhoneBook.Web.Services;

public sealed record SearchHit(
    int GroupId,
    string GroupTitle,
    string Name,
    string? Extension);

public sealed class LiveSearchService(IDbContextFactory<AppDbContext> contextFactory)
{
    private readonly object _sync = new();
    private IndexedHit[] _index = [];

    public async Task RefreshAsync(CancellationToken ct = default)
    {
        await using AppDbContext context = await contextFactory.CreateDbContextAsync(ct);
        IndexedHit[] refreshed = await context.PhoneBookEntries
            .AsNoTracking()
            .Where(entry => entry.IsActive && entry.Group.IsActive)
            .OrderBy(entry => entry.Group.DisplayOrder)
            .ThenBy(entry => entry.DisplayOrder)
            .Select(entry => new SearchHit(
                entry.GroupId,
                entry.Group.Title,
                entry.Name,
                entry.Extension))
            .AsAsyncEnumerable()
            .Select(hit => new IndexedHit(
                hit,
                PersianTextNormalizer.NormalizeForSearch(hit.GroupTitle),
                PersianTextNormalizer.NormalizeForSearch(hit.Name),
                PersianTextNormalizer.NormalizeForSearch(hit.Extension ?? string.Empty)))
            .ToArrayAsync(ct);

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
