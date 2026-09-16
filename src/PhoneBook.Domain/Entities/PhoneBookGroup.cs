// FILE: src/PhoneBook.Domain/Entities/PhoneBookGroup.cs
using PhoneBook.Domain.Enums;

namespace PhoneBook.Domain.Entities;

public sealed class PhoneBookGroup
{
    public int Id { get; set; }

    public string Title { get; set; } = string.Empty;

    public int Priority { get; set; }

    public ColumnPosition? PreferredColumn { get; set; }

    public int DisplayOrder { get; set; }

    public bool Required { get; set; } = true;

    public bool KeepTogether { get; set; } = true;

    public bool IsActive { get; set; } = true;

    public ICollection<PhoneBookEntry> Entries { get; set; } = [];
}
