// FILE: src/PhoneBook.Domain/Entities/PhoneBookEntry.cs
namespace PhoneBook.Domain.Entities;

public sealed class PhoneBookEntry
{
    public int Id { get; set; }

    public int GroupId { get; set; }

    public PhoneBookGroup Group { get; set; } = null!;

    public string Name { get; set; } = string.Empty;

    public string? Extension { get; set; }

    public int DisplayOrder { get; set; }

    public bool IsActive { get; set; } = true;
}
