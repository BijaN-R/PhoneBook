using PhoneBook.Core.Layout;
using PhoneBook.Domain.Entities;

namespace PhoneBook.Application.Models;

public sealed record PhoneBookDocument(
    DocumentHeader Header,
    IReadOnlyList<PhoneBookGroup> Groups,
    AppSettings Settings,
    LayoutResult Layout);
