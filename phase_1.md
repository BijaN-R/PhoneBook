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

## PHASE 1 DELIVERABLES
Create the following files ONLY. Do not write Layout/Export/Web code yet.

1. `dotnet cli` commands to:
   - Create solution PhoneBookApp.sln at repo root.
   - Create the 7 projects under src/:
     PhoneBook.Domain, PhoneBook.Infrastructure, PhoneBook.Core,
     PhoneBook.Export.OpenXml, PhoneBook.Export.Image,
     PhoneBook.Web (blazor template), PhoneBook.Tests.
   - Add project references (Web → all; Tests → Core + Export.* + Infrastructure;
     Infrastructure → Domain; Core → Domain; Export.* → Core + Domain).
   - Add NuGet packages with EXACT latest-stable versions for net10.0:
     * Infrastructure: Microsoft.EntityFrameworkCore.Sqlite,
                       Microsoft.EntityFrameworkCore.Design
     * OpenXml: DocumentFormat.OpenXml (3.x)
     * Image: SkiaSharp, SkiaSharp.NativeAssets.Linux, SkiaSharp.NativeAssets.Win32
     * Tests: xunit, xunit.runner.visualstudio, Microsoft.NET.Test.Sdk,
              FluentAssertions

2. All 7 `.csproj` files, complete, net10.0, nullable enabled.
   - PhoneBook.Web uses Microsoft.NET.Sdk.Web; all others use Microsoft.NET.Sdk.

3. Domain entities (PhoneBook.Domain/Entities/):
   - DocumentHeader.cs
   - PhoneBookGroup.cs   (Id:int, Title:string, Priority:int,
                          PreferredColumn:ColumnPosition?,
                          DisplayOrder:int, Required:bool=true,
                          KeepTogether:bool=true, IsActive:bool=true,
                          Entries:ICollection<PhoneBookEntry>)
   - PhoneBookEntry.cs   (Id:int, GroupId:int, Group nav, Name:string,
                          Extension:string?, DisplayOrder:int, IsActive:bool=true)
   - AppSettings.cs      (Id:int, PageWidthMm:double=210, PageHeightMm:double=297,
                          MarginTopMm:double=10, MarginBottomMm:double=10,
                          MarginLeftMm:double=8, MarginRightMm:double=8,
                          GroupGapMm:double=2.5, CellPaddingMm:double=1.2,
                          PrimaryFontFamily:string="Vazirmatn",
                          UsePersianDigits:bool=true, MinFontSizePt:double=7,
                          DefaultFontSizePt:double=9, HeaderFontSizePt:double=16,
                          GroupHeaderFontSizePt:double=10, PriorityTopLimit:int=3)

4. Domain enum: `ColumnPosition { Auto = -1, Right = 0, Middle = 1, Left = 2 }`.

5. `AppDbContext.cs`:
   - DbSets for all 4 entities.
   - OnModelCreating: apply configurations (keys, indexes, required, max lengths).
   - HasData seeding with the FULL dataset (see below).

6. `AppDbContextFactory.cs` implementing IDesignTimeDbContextFactory for `dotnet ef` CLI.

7. The `dotnet ef migrations add InitialCreate` command to run manually.

## SEED DATA (exact — do not paraphrase)

DocumentHeader:
  Title = "داخلی پرسنل شرکت رهیاب رایانه گستر"
  Subtitle = null
  UpdatedAt = 2026-01-01

Groups (in this display order — right-to-left visual = Group 1 is rightmost col by default):
  Group 1: "فروش",                Priority=1, PreferredColumn=Right,  DisplayOrder=1
  Group 2: "نرم‌افزار پیام کوتاه",   Priority=4, PreferredColumn=Right,  DisplayOrder=2
  Group 3: "پشتیبانی پیام کوتاه",   Priority=7, PreferredColumn=Right,  DisplayOrder=3
  Group 4: "آبدارخانه",            Priority=9, PreferredColumn=Right,  DisplayOrder=4
  Group 5: "لجستیک",              Priority=2, PreferredColumn=Middle, DisplayOrder=5
  Group 6: "سامانه‌های عمومی",      Priority=10,PreferredColumn=Middle, DisplayOrder=6
  Group 7: "تست و تضمین کیفیت",    Priority=11,PreferredColumn=Middle, DisplayOrder=7
  Group 8: "طراحی محصول",          Priority=12,PreferredColumn=Middle, DisplayOrder=8
  Group 9: "امور قراردادها",        Priority=13,PreferredColumn=Middle, DisplayOrder=9
  Group 10:"دبیرخانه",             Priority=14,PreferredColumn=Middle, DisplayOrder=10
  Group 11:"حراست",                Priority=15,PreferredColumn=Middle, DisplayOrder=11
  Group 12:"اسکرام مستر",          Priority=16,PreferredColumn=Middle, DisplayOrder=12
  Group 13:"مدیریت",               Priority=3, PreferredColumn=Left,   DisplayOrder=13
  Group 14:"واحد مالی",            Priority=5, PreferredColumn=Left,   DisplayOrder=14
  Group 15:"واحد IT",              Priority=6, PreferredColumn=Left,   DisplayOrder=15
  Group 16:"واحد منابع انسانی",      Priority=8, PreferredColumn=Left,   DisplayOrder=16
  Group 17:"اتاق مشاوران",          Priority=17,PreferredColumn=Left,   DisplayOrder=17
  Group 18:"اتاق کنفرانس طبقه ۶",   Priority=18,PreferredColumn=Left,   DisplayOrder=18
  Group 19:"اتاق جلسات طبقه ۳",     Priority=19,PreferredColumn=Left,   DisplayOrder=19

Entries (Group / Name / Extension / DisplayOrder) — use PERSIAN digits for extensions as shown:

فروش:
  1 آقای جعفری / ۱۸۸
  2 خانم جهان‌نما / ۱۹۶
  3 خانم تقوائی / ۱۹۳
  4 خانم شمس / ۱۹۵
  5 خانم خیری / ۱۹۴
  6 خانم اردستانی / ۱۹۸

نرم‌افزار پیام کوتاه:
  1 آقای علی نوربخش / ۱۱۶
  2 خانم سمانه مومن / ۱۱۵
  3 آقای احسان جعفری / ۱۲۲
  4 آقای علی گودرزی / ۱۱۸
  5 آقای مهرابی / ۱۲۴
  6 آقای افشار / ۱۱۹
  7 آقای صالحان / ۱۲۳
  8 آقای عبدالهی / ۱۲۵
  9 خانم جعفر کیاه / ۱۲۰

پشتیبانی پیام کوتاه:
  1 آقای نوید نصیری / ۱۵۹
  2 آقای مهدی خانی / ۱۶۱
  3 آقای علی کامجو / ۱۸۱
  4 آقای محمدجواد کریمی راد / ۱۵۶
  5 آقای نصرت رنجبر / ۱۶۷
  6 آقای بهبهانی / ۱۵۵
  7 خانم مسناآبادی / ۱۶۰
  8 آقای سامانی / ۱۸۳
  9 خانم میرزا زاده / ۱۸۵

آبدارخانه:
  1 طبقه اول – علیرضا فتحی / ۱۲۸
  2 طبقه دوم – آقای بهروز شیرآوند / ۲۴۱
  3 طبقه سوم – آقای حسن شیرآوند / ۳۱۷
  4 Name = "" / ۵۹۰
  5 طبقه ششم – آقای آیت شیراوند / ۶۱۳

لجستیک:
  1 خانم قائم مقامی / ۲۰۴
  2 خانم حامدی / ۲۴۳
  3 خانم سلیمی / ۲۴۲
  4 آقای مزدارانی / ۲۳۲
  5 خانم نصیری زاده / ۲۳۳
  6 آقای روحی / ۲۲۱
  7 خانم خسروجردی / ۲۱۰
  8 آقای نوروزی / ۲۱۹
  9 بیژن راجی / ۲۲۲
  10 خانم خزچین / ۲۲۸
  11 آقای سعیدی نژاد / ۲۳۱

سامانه‌های عمومی:   1 Name = "" / ۲۲۶
تست و تضمین کیفیت:  1 آقای اسکندری / ۱۳۴
طراحی محصول:        1 خانم نوربخش / ۱۶۳
                    2 خانم سلیمان پوریان / ۱۹۷
امور قراردادها:     1 آقای عسگری / ۵۸۷
دبیرخانه:           1 خانم صفری / ۵۳۷
حراست:              1 آقای استقامتی / ۵۸۴
                    2 نگهبان / ۱۰۰
اسکرام مستر:        1 Name = "" / ۱۲۱

مدیریت:
  1 خانم شاه محمدی / ۶۰۱
  2 آقای دکتر شعاعی / ۶۰۷ - ۶۰۶
  3 آقای باغستانی / ۶۱۰
  4 آقای باقری / ۶۰۳
  5 آقای دکتر فتحعلیزاده / ۶۱۱

واحد مالی:
  1 آقای قلانی / ۳۰۸
  2 خانم خردوار / ۳۰۷
  3 آقای داود آبادی / ۳۰۵
  4 خانم ذوالفقاری / ۳۰۶
  5 خانم طهماسبی / ۳۰۴
  6 آقای حبیبی / ۳۱۶
  7 آقای احمدی زاده / ۳۲۱
  8 آقای لطفی / ۳۰۹
  9 خانم علی نقیان / ۳۱۹

واحد IT:
  1 آقای غریبی / ۴۰۲
  2 خانم زندیه / ۴۰۳
  3 آقای فیروزمنش / ۴۰۱
  4 آقای بیداران / ۴۰۵
  5 آقای ستاری کیا / ۴۰۶
  6 آقای پور صدرا / ۴۰۴
  7 خانم نژاد عبداله / null

واحد منابع انسانی:
  1 آقای خورش / ۵۷۱
  2 خانم گرشاسبی / ۵۷۳
  3 آقای عباسی / ۵۷۲
  4 خانم خزائلی / ۵۷۷
  5 خانم رحیمی / ۵۳۸

اتاق مشاوران:        1 Name = "" / ۶۰۴
اتاق کنفرانس طبقه ۶:  1 Name = "" / ۶۰۹
اتاق جلسات طبقه ۳:    1 Name = "" / ۳۱۱

Note on empty-name entries: In the DB store Name = string.Empty ("").
Do NOT store the group title as the name.

## END OF PHASE 1
Stop here. Wait for Phase 2.