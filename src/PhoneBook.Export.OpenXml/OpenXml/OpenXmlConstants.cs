// FILE: src/PhoneBook.Export.OpenXml/OpenXml/OpenXmlConstants.cs
namespace PhoneBook.Export.OpenXml.OpenXml;

public static class OpenXmlConstants
{
    public const string WordprocessingNamespace =
        "http://schemas.openxmlformats.org/wordprocessingml/2006/main";

    public const string OfficeDocumentRelationshipsNamespace =
        "http://schemas.openxmlformats.org/officeDocument/2006/relationships";

    public const string PackageRelationshipsNamespace =
        "http://schemas.openxmlformats.org/package/2006/relationships";

    public const int A4PageWidthDxa = 11_906;
    public const int A4PageHeightDxa = 16_838;
    public const int A4WidthPixelsAt96Dpi = 794;
    public const int A4HeightPixelsAt96Dpi = 1_123;
    public const double DxaPerMillimeter = 56.6929;
    public const int ColumnCount = 3;
    public const uint ThinBorderSizeEighthPoints = 4U;
    public const uint TitleFontSizeHalfPoints = 32U;
    public const uint GroupHeaderFontSizeHalfPoints = 20U;
    public const uint BodyFontSizeHalfPoints = 18U;
}
