// FILE: src/PhoneBook.Domain/Entities/DocumentHeader.cs
namespace PhoneBook.Domain.Entities;

public sealed class DocumentHeader
{
    public int Id { get; set; }

    public string Title { get; set; } = string.Empty;

    public string? Subtitle { get; set; }

    public DateOnly UpdatedAt { get; set; }
}
