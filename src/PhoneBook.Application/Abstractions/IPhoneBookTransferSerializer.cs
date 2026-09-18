using PhoneBook.Application.Models;

namespace PhoneBook.Application.Abstractions;

public interface IPhoneBookTransferSerializer
{
    byte[] Serialize(PhoneBookTransferDocument document);

    PhoneBookTransferDocument Deserialize(ReadOnlySpan<byte> utf8Json);
}
