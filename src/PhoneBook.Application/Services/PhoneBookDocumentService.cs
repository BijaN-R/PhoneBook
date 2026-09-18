using PhoneBook.Application.Models;
using PhoneBook.Core.Layout;
using PhoneBook.Domain.Entities;

namespace PhoneBook.Application.Services;

public sealed class PhoneBookDocumentService(
    PhoneBookQueryService queries,
    SettingsService settingsService,
    LayoutEngine layoutEngine,
    ITextMeasurer textMeasurer)
{
    public async Task<PhoneBookDocument> PrepareAsync(CancellationToken ct = default)
    {
        Task<DocumentHeader> headerTask = queries.GetDocumentHeaderAsync(ct);
        Task<IReadOnlyList<PhoneBookGroup>> groupsTask = queries.GetGroupsAsync(
            activeOnly: true,
            ct);
        Task<AppSettings> settingsTask = settingsService.GetAsync(ct);

        await Task.WhenAll(headerTask, groupsTask, settingsTask);
        DocumentHeader header = await headerTask;
        IReadOnlyList<PhoneBookGroup> groups = await groupsTask;
        AppSettings settings = await settingsTask;
        LayoutResult layout = layoutEngine.CreateLayout(groups, settings, textMeasurer);

        return new PhoneBookDocument(header, groups, settings, layout);
    }
}
