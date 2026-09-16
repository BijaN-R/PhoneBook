You are acting as a Senior .NET Software Architect and Full-Stack Engineer with deep expertise in:
- C# 14, .NET 10 (ASP.NET Core Web Application)
- Entity Framework Core 10 + SQLite (Code-First, manual migrations)
- Open XML SDK 3.x (WordprocessingML) — strict type-safety required
- SkiaSharp (both PNG raster and PDF vector backends)
- Bin-Packing / Beam Search layout optimization
- Persian/RTL typography, Vazirmatn font, Persian digit normalization

## CONTEXT
I'm building a small internal Persian phone directory for a company (Rahyaab Rayaneh Gostar).
It's intentionally SIMPLE — a single-user Blazor Server app with a 3-column A4 layout
that mirrors a provided reference screenshot. No auth. No logging. No Docker.

Export target: ONE page when possible. If content cannot be compressed to fit
on a single A4 page even after ALL fallback strategies, the layout MAY span
across as many pages as needed. No priority re-ordering is required when
spilling to subsequent pages — the natural DisplayOrder flow is preserved
(whatever doesn't fit in page-1 columns continues on next pages, in order,
starting from column 0).

## HARD RULES (apply to every phase)
1. TargetFramework = net10.0, LangVersion = 14, Nullable = enable, ImplicitUsings = enable.
2. NO Microsoft Office Interop. NO LibreOffice dependency. NO QuestPDF.
3. ALL code must be strongly typed and compile-ready. No pseudo-code, no "...", no TODOs.
4. OpenXML SDK type-safety:
   - Use direct uint assignment (e.g., Size = 18U), NOT UInt32Value.FromString().
   - Use checked((short)value) for Int16Value properties (e.g., Width = checked((short)twips)).
   - Never .ToString().Parse() round-trips for typed values.
   - Balance DXA rounding remainders on the LAST column to prevent layout shift.
5. Persian support:
   - Convert Persian digits (۰-۹) and Arabic-Indic digits (٠-٩) to English for search normalization.
   - Convert English digits to Persian for display/export when UsePersianDigits = true.
   - Normalize ی/ي, ک/ك, and remove ZWNJ variations during search.
6. Every code block must be a COMPLETE file. Include the `using` directives. Include the namespace.
   Provide the exact file path as a comment at the top: `// FILE: src/.../X.cs`.
7. Output ONLY the files requested for that phase. If the response is truncated, end with:
   `[TRUNCATED: remaining files are X, Y, Z]`.

## CONFIRMED DECISIONS (do not re-ask, do not change)
- UI: Blazor Server (single project, no separate API, no SPA).
- Auth: none. Single-user. Concurrency: none needed.
- .NET: net10.0 stable. C# 14.
- Max contacts: < 1500 → in-memory filtering for live search is fine.
- Search: contains-based after normalization + simple fuzzy
  (prefix match OR substring match OR Levenshtein distance <= 2).
- Calendar: Gregorian only.
- Migrations: manual (`dotnet ef migrations add`), NOT `Database.Migrate()` at startup.
- PriorityTopLimit = 3.
- MinFontSize = 7 pt.
- Reference layout: A 3-column A4 portrait screenshot is provided by me (user).
- Font: Vazirmatn TTF files (Regular + Bold) will be placed at
  `src/PhoneBook.Web/wwwroot/fonts/Vazirmatn-Regular.ttf` and `.../Vazirmatn-Bold.ttf`.
  They MUST be embedded inside DOCX (w:embedRegular / w:embedBold) and registered in SkiaSharp.
- Empty-name entries (e.g., "طبقه پنجم (۵۹۰)") are shown with an empty Name cell —
  only the extension is displayed.
- Priority (int) rules:
    * Priority 1, 2, 3 → placed at the TOP of column 0 (Right), 1 (Middle), 2 (Left)
      respectively.
    * Priority 4 → placed at the SECOND row of one column, chosen by the cost function.
    * Priority 5+ → normal bin-packing.
    * `IsTopPriority` boolean is REMOVED. Top-priority means `Priority in [1,2,3,4]`.
- PDF export uses SkiaSharp's built-in `SKDocument.CreatePdf()` (vector backend).
  No external process.
- Multi-page: 1 page preferred, unlimited pages allowed. No re-ordering
  on spill. Priority 1..4 constraints apply ONLY to page 1.
- Delivery: `dotnet publish` only. No Docker.
- Logging: none.
- Only `.csproj` files are needed as build config.
  No Directory.Build.props, no global.json, no .editorconfig.
- Tests: full test suite (Layout, Normalizer, Fuzzy, Export smoke tests,
  multi-page fallback).

## PHASE 3 DELIVERABLES
Assume Phase 1 & 2 done. Files under src/PhoneBook.Export.OpenXml/.

Reference structure (from the provided screenshot):
  - A4 Portrait, thin margins.
  - Top: a SINGLE bordered box spanning the full page width containing the
    document title (centered, bold, ~16pt).
  - Below: 3 vertical columns (Right, Middle, Left in RTL reading order).
  - Each column = a stack of "group cards".
  - Each group card = a 2-column nested table:
      * Row 1: merged header cell (GridSpan=2) with light-gray shading (#D9D9D9),
        bold group title centered.
      * Rows 2..N: Name on the RIGHT cell, Extension on the LEFT cell (RTL order).
      * Thin black borders around all cells.
  - The mother table has no outer borders.

Files:

1. `OpenXml/OpenXmlConstants.cs`
   - Namespace URIs.
   - Fixed DXA width constants for A4 at 96 DPI.
   - Font sizes in half-points (uint).

2. `OpenXml/RunFactory.cs`
   - `Run CreateTextRun(string text, string font, double sizePt, bool bold,
                       bool rtl)`
   - MUST set: RunFonts (Ascii, HighAnsi, ComplexScript), FontSize,
     FontSizeComplexScript (as uint), Bold, BoldComplexScript,
     RightToLeftText, Language (bidi="fa-IR").

3. `OpenXml/ParagraphFactory.cs`
   - `Paragraph CreateCentered(string text, ...)`
   - `Paragraph CreateEmpty()` — REQUIRED after every nested table
     (WordprocessingML validity).
   - `Paragraph CreatePageBreak()` — a paragraph containing a `Break` element
     with `Type = BreakValues.Page`.

4. `OpenXml/TableFactory.cs`
   - `Table CreateMotherTable(...)` — 1 row, 3 cells, no borders,
     TableLayout=Fixed, explicit dxa widths summing to page content width.
     Remainder added to LAST cell.
   - `Table CreateGroupTable(PhoneBookGroup, AppSettings, ITextMeasurer)` —
     nested table with GridSpan=2 header, data rows, thin borders via
     TableBorders. Width = column width, all rows fixed height from
     HeightEstimator.

5. `OpenXml/FontEmbedder.cs`
   - Reads Vazirmatn-Regular.ttf and Vazirmatn-Bold.ttf bytes.
   - Registers them as `FontTablePart` embedded fonts (w:embedRegular,
     w:embedBold) with **obfuscated** font obfuscation (Word requires XOR of
     first 32 bytes with GUID — implement it fully).
   - References them from the default `rFonts` via FontRelId.

6. `OpenXml/OpenXmlPhoneBookGenerator.cs`
   - Signature:
     ```csharp
     Task<byte[]> GenerateAsync(
         DocumentHeader header,
         IReadOnlyList<PhoneBookGroup> groups,
         LayoutResult layout,
         AppSettings settings,
         CancellationToken ct = default);
     ```
   - Steps:
     1. Compute mm→DXA: `dxa = round(mm * 56.6929)`.
        Use `checked((short)value)` for Int16Value.
     2. Create WordprocessingDocument in a MemoryStream.
     3. MainDocumentPart → Document → Body.
     4. Page size: A4 (11906 x 16838 twips). Orientation Portrait.
        Margins from `layout.EffectiveSettings`.
     5. Insert the title box ONCE at the top of the document
        (single table, 1 row 1 cell, bordered, centered title run).
     6. For each page in `layout.Pages` (index P):
          - If P > 0, insert a page-break paragraph FIRST:
              `new Paragraph(new Run(new Break { Type = BreakValues.Page }))`
          - Insert the mother table for that page with 3 cells; each cell
            contains the group tables for that page's column, each followed
            by an empty paragraph (WordprocessingML validity).
     7. Set `BiDi` on every paragraph and section properties `bidi`.
     8. Save and return the byte array.

7. `OpenXml/OpenXmlValidatorRunner.cs`
   - Uses `DocumentFormat.OpenXml.Validation.OpenXmlValidator` to validate the
     produced document BEFORE returning. If invalid, throw with the errors.

## END OF PHASE 3
Stop here. Wait for Phase 4.