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

## PHASE 4 DELIVERABLES
Assume Phase 1-3 done. Files under src/PhoneBook.Export.Image/.

1. `Skia/SkiaFontRegistry.cs`
   - Loads Vazirmatn Regular + Bold SKTypeface from disk once, exposes them.
   - Uses `SKFontManager.Default.CreateTypeface(path)`.

2. `Skia/SkiaPageRenderer.cs`
   - Signature (multi-page aware):
     ```csharp
     void Render(
         SKCanvas canvas,
         DocumentHeader header,
         PageLayout page,
         AppSettings settings,
         bool isFirstPage,      // draw title box only when true
         double dpiScale);
     ```
   - Draws:
     * White page background (A4 at dpiScale).
     * Title box (only when `isFirstPage == true`): stroked rectangle +
       centered bold Persian title. Prefer `SKTextBlob` for proper shaping;
       fall back to `canvas.DrawText` with `SKTextAlign.Center`.
     * 3 columns starting from the RIGHT (RTL): column 0 = rightmost.
     * For each group: header rect filled #D9D9D9 + bold title; border rect;
       rows top→bottom; Name right-aligned in the right half; Extension
       LEFT-aligned in the left half (visually left side, digits read LTR).
     * Convert digits with `PersianTextNormalizer.ToPersianDigits` when
       `settings.UsePersianDigits == true`.
   - All coordinates in points (1/72 in).
     Helper: `mmToPt(double mm) => mm * 72.0 / 25.4;`

3. `Skia/SkiaSharpImageGenerator.cs`
   - Signature:
     ```csharp
     Task<IReadOnlyList<byte[]>> GeneratePngPagesAsync(
         DocumentHeader header,
         LayoutResult layout,
         int dpi = 300,
         CancellationToken ct = default);
     ```
   - Returns one PNG per page. Caller (Blazor UI) decides whether to download
     page 1 only, or offer a multi-file / ZIP download.
   - Each PNG is exactly A4 at the requested DPI.
     Page pixel size = (210mm, 297mm) * dpi / 25.4.
   - Uses `SKImageInfo` + `SKSurface.Create`, then
     `SKImage.Encode(SKEncodedImageFormat.Png, 100)`.
   - Count of returned items = `layout.Pages.Count`.

4. `Skia/SkiaSharpPdfGenerator.cs`
   - Signature:
     ```csharp
     Task<byte[]> GeneratePdfAsync(
         DocumentHeader header,
         LayoutResult layout,
         CancellationToken ct = default);
     ```
   - Uses `SKDocument.CreatePdf(stream)`.
   - Internally: for each page in `layout.Pages`, call
     `doc.BeginPage(595.28f, 841.89f)`, render that page
     (pass `isFirstPage: (pageIndex == 0)`), then `doc.EndPage()`.
   - After `doc.Close()`, verify `CountPages(pdfBytes) == layout.Pages.Count`.
     Throw if mismatch.
   - `int CountPages(byte[] pdf)` — simple byte scan for `/Type /Page`
     (case-insensitive), used as verification.

5. `Skia/SkiaExportService.cs`
   - Facade:
     ```csharp
     Task<byte[]> ExportPdfAsync(...);                      // one PDF, N pages
     Task<IReadOnlyList<byte[]>> ExportPngPagesAsync(...);  // one PNG per page
     ```

## END OF PHASE 4
Stop here. Wait for Phase 5.