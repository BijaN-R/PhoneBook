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

## PHASE 5 DELIVERABLES
Assume Phase 1-4 done. Files under src/PhoneBook.Web/.

1. `Program.cs`
   - AddRazorComponents().AddInteractiveServerComponents().
   - AddDbContextFactory<AppDbContext>(opt => opt.UseSqlite(connString)).
   - Register: AppSettingsService (singleton, loads from DB on first access),
     LayoutEngine, ITextMeasurer → SkiaTextMeasurer, FontProvider,
     OpenXmlPhoneBookGenerator, SkiaSharpImageGenerator,
     SkiaSharpPdfGenerator, SkiaExportService, PhoneBookRepository,
     LiveSearchService.
   - Static files middleware.
   - `app.MapRazorComponents<App>().AddInteractiveServerRenderMode();`
   - DO NOT auto-migrate. Fail fast if DB missing with a clear message.

2. `App.razor` — RTL root document with
   `<html dir="rtl" lang="fa">`, references Vazirmatn via @font-face from
   wwwroot/fonts.

3. `Routes.razor`, `_Imports.razor`, `MainLayout.razor` (simple top nav with:
   Dashboard / Groups / Entries / Settings / Preview).

4. `Services/AppSettingsService.cs` — cached settings, `UpdateAsync(AppSettings)`.

5. `Services/PhoneBookRepository.cs` — CRUD for groups & entries, all async,
   all using IDbContextFactory.

6. `Services/LiveSearchService.cs`
   - Loads all active entries + groups into memory once at startup.
   - `IEnumerable<SearchHit> Search(string query)` — uses PersianFuzzyMatcher.
   - `record SearchHit(int GroupId, string GroupTitle, string Name,
                       string? Extension)`.
   - Thread-safe refresh via lock (single-user, so trivial).

7. Components:
   - `Components/Pages/Dashboard.razor`
       * Search input with `@bind:event="oninput"` and `@bind:after="OnSearch"`
         → filters via LiveSearchService.
		 Search input uses a 150ms debounce via CancellationTokenSource.
		 Cancel any in-flight search before starting a new one. This is a UI
		 concern (avoid SignalR flooding), not a search-performance concern.
       * Group filter dropdown ("همه گروه‌ها" + list).
       * Result list with live highlighting of matched substring.
       * Export buttons:
           - **DOCX** → single file, all pages.
           - **PDF** → single file, all pages.
           - **PNG**:
               * If `layout.Pages.Count == 1`: single button
                 "دانلود PNG".
               * If `layout.Pages.Count >= 2`: render a dropdown menu with one entry
                 per page ("دانلود PNG – صفحه K" for K in 1..N), plus a final entry
                 "دانلود PNG – همه صفحات (ZIP)".
                 (built in memory with `System.IO.Compression.ZipArchive`).
         Use `NavigationManager.NavigateTo(dataUri, forceLoad: true)` or
         JS interop download for each file.
   - `Components/Pages/Groups.razor` — table CRUD for groups with inline edit.
   - `Components/Pages/Entries.razor` — pick group, list entries,
     add/edit/delete, reorder via up/down buttons (no JS lib).
   - `Components/Pages/Settings.razor` — edit AppSettings fields.
   - `Components/Pages/Preview.razor` — runs LayoutEngine in-memory and
     renders ALL pages stacked vertically with a label above each one
     ("صفحه ۱"، "صفحه ۲" ...). Each page container visually mirrors the
     exported DOCX/PNG. Uses inline `<div>` + plain CSS (no Tailwind build
     step required). If Pages.Count > 3, add a page-jump sidebar for quick
     navigation.

8. `Components/Shared/GroupCard.razor` — reusable group card used both in
   Preview and (conceptually) in exports.

9. `wwwroot/css/app.css` — RTL styles, grid layout for 3 columns,
   group card styling matching #D9D9D9 header, page separator styling for
   Preview.

10. `appsettings.json`:
    ```json
    {
      "ConnectionStrings": { "Default": "Data Source=phonebook.db" },
      "Fonts": { "BasePath": "wwwroot/fonts" }
    }
    ```

## END OF PHASE 5
Stop here. Wait for Phase 6.