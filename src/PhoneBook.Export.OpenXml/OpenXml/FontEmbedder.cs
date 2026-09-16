// FILE: src/PhoneBook.Export.OpenXml/OpenXml/FontEmbedder.cs
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;

namespace PhoneBook.Export.OpenXml.OpenXml;

public sealed class FontEmbedder
{
    private const string FontFamily = "Vazirmatn";
    private readonly string _regularFontPath;
    private readonly string _boldFontPath;

    public FontEmbedder(string baseFontsDirectoryPath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(baseFontsDirectoryPath);

        string fontsDirectory = Path.GetFullPath(baseFontsDirectoryPath);
        _regularFontPath = Path.Combine(fontsDirectory, "Vazirmatn-Regular.ttf");
        _boldFontPath = Path.Combine(fontsDirectory, "Vazirmatn-Bold.ttf");
    }

    public async Task EmbedAsync(MainDocumentPart mainDocumentPart, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(mainDocumentPart);

        byte[] regularFont = await ReadFontAsync(_regularFontPath, cancellationToken);
        byte[] boldFont = await ReadFontAsync(_boldFontPath, cancellationToken);

        FontTablePart fontTablePart = mainDocumentPart.AddNewPart<FontTablePart>();
        FontPart regularPart = fontTablePart.AddFontPart(FontPartType.FontOdttf);
        FontPart boldPart = fontTablePart.AddFontPart(FontPartType.FontOdttf);

        Guid regularKey = Guid.NewGuid();
        Guid boldKey = Guid.NewGuid();
        ObfuscateFont(regularFont, regularKey);
        ObfuscateFont(boldFont, boldKey);

        using (MemoryStream regularStream = new(regularFont, writable: false))
        {
            regularPart.FeedData(regularStream);
        }

        using (MemoryStream boldStream = new(boldFont, writable: false))
        {
            boldPart.FeedData(boldStream);
        }

        string regularRelationshipId = fontTablePart.GetIdOfPart(regularPart);
        string boldRelationshipId = fontTablePart.GetIdOfPart(boldPart);
        string regularFontKey = regularKey.ToString("B").ToUpperInvariant();
        string boldFontKey = boldKey.ToString("B").ToUpperInvariant();

        Font font = new() { Name = FontFamily };
        font.Append(
            new FontFamily { Val = FontFamilyValues.Swiss },
            new Pitch { Val = FontPitchValues.Variable },
            new EmbedRegularFont
            {
                Id = regularRelationshipId,
                FontKey = regularFontKey
            },
            new EmbedBoldFont
            {
                Id = boldRelationshipId,
                FontKey = boldFontKey
            });

        fontTablePart.Fonts = new Fonts(font);
        fontTablePart.Fonts.Save();

        AddDefaultFontStyle(mainDocumentPart);
        AddFontEmbeddingSettings(mainDocumentPart);
    }

    internal static void ObfuscateFont(byte[] fontBytes, Guid key)
    {
        ArgumentNullException.ThrowIfNull(fontBytes);

        if (fontBytes.Length < 32)
        {
            throw new InvalidDataException("A font must contain at least 32 bytes to be obfuscated.");
        }

        byte[] keyBytes = key.ToByteArray();
        for (int index = 0; index < 32; index++)
        {
            fontBytes[index] ^= keyBytes[15 - (index % 16)];
        }
    }

    private static async Task<byte[]> ReadFontAsync(string path, CancellationToken cancellationToken)
    {
        if (!File.Exists(path))
        {
            throw new FileNotFoundException($"Required font file was not found: '{path}'.", path);
        }

        return await File.ReadAllBytesAsync(path, cancellationToken);
    }

    private static void AddDefaultFontStyle(MainDocumentPart mainDocumentPart)
    {
        StyleDefinitionsPart stylesPart = mainDocumentPart.AddNewPart<StyleDefinitionsPart>();
        RunFonts runFonts = new()
        {
            Ascii = FontFamily,
            HighAnsi = FontFamily,
            ComplexScript = FontFamily
        };

        stylesPart.Styles = new Styles(
            new DocDefaults(
                new RunPropertiesDefault(
                    new RunPropertiesBaseStyle(runFonts)),
                new ParagraphPropertiesDefault(
                    new ParagraphPropertiesBaseStyle(
                        new BiDi { Val = true }))));
        stylesPart.Styles.Save();
    }

    private static void AddFontEmbeddingSettings(MainDocumentPart mainDocumentPart)
    {
        DocumentSettingsPart settingsPart = mainDocumentPart.AddNewPart<DocumentSettingsPart>();
        settingsPart.Settings = new Settings(
            new EmbedTrueTypeFonts { Val = true },
            new SaveSubsetFonts { Val = false });
        settingsPart.Settings.Save();
    }
}
