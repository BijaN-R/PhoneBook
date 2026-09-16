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

## PHASE 2 DELIVERABLES
Assume Phase 1 is done. Files go under src/PhoneBook.Core/.

1. `Text/PersianTextNormalizer.cs`
   - `Normalize(string input): string` → standardize ی/ي, ک/ك, ه/ۀ, remove ZWNJ,
     collapse whitespace.
   - `ToPersianDigits(string input): string`
   - `ToEnglishDigits(string input): string`  (handles ۰-۹ and ٠-٩)
   - `NormalizeForSearch(string input): string` → Normalize + ToEnglishDigits
     + lowercase.

2. `Text/PersianFuzzyMatcher.cs`
   - `int LevenshteinDistance(string a, string b): int`
     (with O(min(a,b)) memory optimization).
   - `bool IsMatch(string normalizedQuery, string normalizedTarget): bool`
       → true if target.StartsWith(query)
              || target.Contains(query)
              || LevenshteinDistance(query, target) <= 2.
   - Handle empty query → returns true (matches everything).

3. `Layout/ITextMeasurer.cs`

   public interface ITextMeasurer
   {
       double MeasureTextWidthMm(string text, string fontFamily, double fontSizePt);
       double MeasureTextHeightMm(string fontFamily, double fontSizePt);
   }


4. `Layout/SkiaTextMeasurer.cs`
   - Uses SkiaSharp `SKTypeface` + `SKFont` for accurate measurement.
   - Font files loaded via `FontProvider`.

5. `Layout/FontProvider.cs`
   - `SKTypeface GetRegular()`
   - `SKTypeface GetBold()`
   - `string GetFontPath(string family)`
   - Constructor takes a base fonts directory path.

6. `Layout/HeightEstimator.cs`
   - `double EstimateGroupHeightMm(PhoneBookGroup group, AppSettings settings,
                                  ITextMeasurer measurer)`
       = GroupHeaderHeight + (RowCount * RowHeight) + internal borders.
   - `double EstimateRowHeightMm(AppSettings settings, ITextMeasurer measurer)`.
   - All in mm, computed from font size + cell padding.

7. `Layout/LayoutModels.cs` — with multi-page support:

   public sealed record PlacedGroup(
       PhoneBookGroup Group,
       double HeightMm,
       int ColumnIndex,
       int RowIndex);

   public sealed record PageLayout(
       IReadOnlyList<IReadOnlyList<PlacedGroup>> Columns,   // exactly 3 columns
       double[] ColumnHeightsMm);                           // length = 3

	public sealed record LayoutResult(
		IReadOnlyList<PageLayout> Pages,   // 1..N pages
		AppSettings EffectiveSettings,
		bool LayoutFailed,                 // true only if a single group exceeds
										   // a full page height, or if the packer
										   // timed out with unresolved overflow
		string? FailureReason);


8. `Layout/LayoutEngine.cs` — THE CORE.

   Algorithm outline:

   a. Validate that the count of groups with `Priority in [1..PriorityTopLimit]`
      (i.e., 1..3) is <= PriorityTopLimit. If violated, throw a clear
      InvalidOperationException.

   b. Place Priority 1, 2, 3 at the top of page-1 columns Right(0), Middle(1),
      Left(2) respectively.

   c. Place Priority 4 at the SECOND row of ONE page-1 column — chosen by
      trying all 3 candidate columns and taking the one with the lowest cost.

   d. Pass 1 — attempt to fit everything on ONE page by progressively
      reducing layout slack, in this priority order (mildest first):
        (1) group gaps, (2) cell padding + row heights, (3) margins,
        (4) font size down to MinFontSizePt.
      Apply one step at a time and re-run the packer after each. If the
      minimum configuration still overflows, ABORT Pass 1 and go to Pass 2.
      Choose conservative multiplier factors yourself; never violate the
      floors defined in AppSettings.
   
      Packer requirements (used in both Pass 1 and Pass 2):
      - Choose a bin-packing heuristic (Beam Search, Best-Fit-Decreasing, or
        hybrid) with a hard 2-second upper bound and best-so-far fallback.
      - Optimize, in priority order: (1) minimize overflow past printable
        height, (2) minimize height variance across the 3 columns,
        (3) respect PreferredColumn when set, (4) minimize empty-space
        imbalance.
      - `KeepTogether = true` groups are atomic and must never be split
        across columns or pages.

   e. Pass 2 — multi-page layout (only if Pass 1 aborted):
      - RESTORE the ORIGINAL AppSettings (do NOT keep shrunken values; page-2
        availability removes the compression constraint).
      - Build the layout as follows:
         * Priority 1, 2, 3 → top of page-1 columns Right(0), Middle(1), Left(2).
         * Priority 4 → row 2 of the page-1 column with the lowest current
           height.
         * For every remaining group, in DisplayOrder ascending order:
             - Try to place it into the SHORTEST column of the CURRENT page
               (ties broken by column index ascending: 0, then 1, then 2).
             - If it fits (columnHeight + groupHeight <= printableHeight):
               place it there and continue.
             - Otherwise, try the next column of the current page
               (0 → 1 → 2).
             - If NO column of the current page can hold it, start a NEW page
               (append a fresh PageLayout with 3 empty columns) and place the
               group at column 0 of the new page.
		* Continue until all groups are placed. There is no page-count ceiling.
		  If a single group's height exceeds the printable height of one page,
		  set LayoutFailed = true and FailureReason = "Group '<title>' exceeds
		  one full page and cannot be split (KeepTogether = true)."
      - EffectiveSettings on the returned LayoutResult = the ORIGINAL
        AppSettings (NOT the shrunken ones from Pass 1).


9. `Layout/LayoutDiagnostics.cs`
   - `string Dump(LayoutResult result): string` — human-readable report:
       * For each page, print a header "=== PAGE N ===", then the 3 columns
         with their heights and placed group titles.
       * Print EffectiveSettings.
       * Print final LayoutFailed and FailureReason.

## END OF PHASE 2
Stop here. Wait for Phase 3.