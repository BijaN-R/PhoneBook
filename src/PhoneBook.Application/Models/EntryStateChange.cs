namespace PhoneBook.Application.Models;

public sealed record EntryStateChange(int EntryId, long ExpectedRevision);
