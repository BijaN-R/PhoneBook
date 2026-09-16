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

## PHASE 6 DELIVERABLES
Assume Phase 1-5 done. Files under src/PhoneBook.Tests/.

1. `PersianTextNormalizerTests.cs`
   - English↔Persian digit conversion round-trip.
   - ی/ي, ک/ك unification.
   - ZWNJ removal.
   - `NormalizeForSearch` idempotent.

2. `PersianFuzzyMatcherTests.cs`
   - Exact match, prefix, substring, distance=1, distance=2, distance=3
     (no match).
   - Empty query matches all.
   - Persian query against Persian target; mixed digits.

3. `HeightEstimatorTests.cs`
   - Row height matches padding + font size expectations.
   - Group height with N rows = header + N * rowHeight.

4. `LayoutEngineTests.cs`
   - Priority 1, 2, 3 land on top of page-1 columns 0, 1, 2.
   - Priority 4 lands at row 2 of exactly one page-1 column.
   - All groups fit on 1 page for the seed dataset with default AppSettings.
   - PreferredColumn respected when set (cost function test).
   - Cost function: variance minimization sanity check with 2 handcrafted
     group sets.

   Multi-page tests:

	[Fact] Layout_With_Huge_Dataset_Produces_Multiple_Pages()
	{
		// Assert: result.Pages.Count >= 2
		// Assert: Priority 1..4 on page 1
		// Assert: overflow content is on page 2+ in DisplayOrder
	}

	[Fact] Layout_With_Extreme_Dataset_Produces_Many_Pages()
	{
		// 500 groups × 20 rows
		// Assert: result.Pages.Count >= 3
		// Assert: result.LayoutFailed == false
		// Assert: no page has more than printable height
	}

   [Fact]
   public void Layout_Order_Is_Preserved_Across_Pages()
   {
		// Groups with DisplayOrder 1..N appear in DisplayOrder across
		// (page-1 columns 0,1,2), then (page-2 columns 0,1,2), ..., (page-K columns 0,1,2).
   }


5. `OpenXmlGeneratorTests.cs` (integration, writes to temp file)
   - Generates DOCX from seed data → OpenXmlValidator returns zero errors.
   - Contains exactly 1 mother table (for 1-page layout).
   - Contains expected title text.
   - Nested table count equals group count.

   Multi-page DOCX:

   [Fact]
   public void Docx_With_N_Page_Layout_Contains_N_Minus_1_PageBreaks()
   {
       // Generate DOCX from a layout with 2 pages.
       // Count <w:br w:type="page"/> elements → expect exactly 1.
       // Validate with OpenXmlValidator → zero errors.
   }


6. `SkiaExportTests.cs`
   - PNG signature (89 50 4E 47) at start of each returned array.
   - PDF signature "%PDF" and CountPages == layout.Pages.Count.
   - Image dimensions for 300 DPI equal
     round(210/25.4*300) x round(297/25.4*300).

   Multi-page Skia:

   [Fact]
   public void Pdf_With_Multi_Page_Layout_Produces_Correct_Page_Count()
   {
       // Assert: CountPages(pdfBytes) == 2.
   }

   [Fact]
   public void Png_Pages_With_Two_Page_Layout_Returns_Two_Images()
   {
       // Assert: result.Count == 2
       // Assert: both start with the PNG signature.
   }


7. `TestDataSeeder.cs` — helper returning the seed dataset as in-memory
   entities (reuse the same data as Phase 1 but without EF).

## END OF PHASE 6
Provide a final short summary of the 6 phases and a one-line
`dotnet run --project src/PhoneBook.Web` confirmation.