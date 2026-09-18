using System.Text.Json;
using System.Text.Json.Serialization;
using PhoneBook.Application.Abstractions;
using PhoneBook.Application.Models;

namespace PhoneBook.Infrastructure.Serialization;

public sealed class JsonPhoneBookTransferSerializer : IPhoneBookTransferSerializer
{
    private static readonly JsonSerializerOptions Options = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = false,
        UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow,
        RespectRequiredConstructorParameters = true,
        AllowTrailingCommas = false,
        ReadCommentHandling = JsonCommentHandling.Disallow,
        WriteIndented = true,
        MaxDepth = 64,
        Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase, allowIntegerValues: false) }
    };

    public byte[] Serialize(PhoneBookTransferDocument document)
    {
        ArgumentNullException.ThrowIfNull(document);
        return JsonSerializer.SerializeToUtf8Bytes(document, Options);
    }

    public PhoneBookTransferDocument Deserialize(ReadOnlySpan<byte> utf8Json)
    {
        return JsonSerializer.Deserialize<PhoneBookTransferDocument>(utf8Json, Options)
            ?? throw new JsonException("The JSON document is null.");
    }
}
