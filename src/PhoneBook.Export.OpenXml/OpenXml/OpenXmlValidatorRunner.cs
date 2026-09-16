// FILE: src/PhoneBook.Export.OpenXml/OpenXml/OpenXmlValidatorRunner.cs
using System.Text;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Validation;

namespace PhoneBook.Export.OpenXml.OpenXml;

public static class OpenXmlValidatorRunner
{
    public static void Validate(byte[] documentBytes)
    {
        ArgumentNullException.ThrowIfNull(documentBytes);

        using MemoryStream stream = new(documentBytes, writable: false);
        using WordprocessingDocument document = WordprocessingDocument.Open(stream, isEditable: false);
        OpenXmlValidator validator = new(FileFormatVersions.Office2019);
        List<ValidationErrorInfo> errors = validator.Validate(document).ToList();

        if (errors.Count == 0)
        {
            return;
        }

        StringBuilder message = new("The generated DOCX failed Open XML validation:");
        foreach (ValidationErrorInfo error in errors)
        {
            message.AppendLine()
                .Append("- ")
                .Append(error.Description ?? "Unknown validation error")
                .Append(" [Path: ")
                .Append(error.Path?.XPath ?? "unknown")
                .Append(']');
        }

        throw new InvalidDataException(message.ToString());
    }
}
