# PROMPT — Dashboard Search Results Pagination

Continue from the **current repository state** after the completed search improvements and previous phases.

Before changing code, inspect the current implementation of:

- `Dashboard.razor`
- the current Application search service/result model
- relevant Dashboard/search CSS

The repository is the source of truth.

## Goal

Add lightweight pagination to the public Dashboard phone-book results without changing the search algorithm, ranking semantics, search index architecture, persistence layer, or database.

The phone-book dataset is small and search results are already held/ranked in memory, so pagination should remain a **Web/UI concern**.

Do not introduce database/server-side pagination unless inspection of the current repository proves that the search architecture has materially changed and no longer returns the complete ranked result set.

---

## 1. Preserve Phase 5 search behavior

Do not modify:

- normalization
- tokenization
- OSA/fuzzy matching
- scoring
- MatchKind rules
- ranking
- search-index refresh behavior

Do not re-sort search results in Dashboard.

The order returned by the Application search service is authoritative.

Pagination must operate **after search and Dashboard filtering**, preserving that order exactly.

If the current Application search already returns all matching results, do not modify:

- `PhoneBookSearchService`
- search result models
- repository interfaces
- Infrastructure
- database schema
- migrations

Do not add page-number/page-size parameters to the search service merely for this UI feature.

---

## 2. Expected implementation scope

The expected primary changes are:

- `PhoneBook.Web/Components/Pages/Dashboard.razor`
- `PhoneBook.Web/wwwroot/css/app.css`

Only modify additional files if inspection of the current repository proves a small supporting change is genuinely required.

Avoid architectural changes.

Do not create a new pagination service.

Do not create database or Application-layer pagination abstractions.

---

## 3. Remove the current display cap

If Dashboard currently limits results using logic equivalent to:

`Take(20)`

remove that hard-coded presentation limit.

Keep the complete filtered/search result collection available inside Dashboard.

Pagination controls determine which subset is rendered.

Do not truncate the underlying result collection.

---

## 4. Pagination state

Keep pagination state locally in `Dashboard.razor`.

Use state equivalent to:

- `_currentPage`
- `_pageSize`

Default:

`_currentPage = 1`

Default page size:

`20`

Support these page-size choices:

- 20
- 50
- 100

Do not persist page size to the database or AppSettings.

No new service is required.

---

## 5. Pagination calculations

After obtaining the final filtered result collection, calculate:

- `TotalResults`
- `TotalPages`
- current valid page
- results belonging to the current page

Use the logical equivalent of:

`Skip((CurrentPage - 1) * PageSize).Take(PageSize)`

Do not mutate or re-order the source result collection.

Use safe page-count calculation so partial final pages are handled correctly.

When there are zero results:

- TotalPages may be treated internally as 0
- do not show an invalid `صفحه ۱ از ۰`
- show the existing/new empty-state UI instead

---

## 6. Page clamping

The current page must always remain valid.

After any operation that changes the result count:

- if results become empty, reset CurrentPage appropriately
- if CurrentPage exceeds TotalPages, clamp it to the last valid page
- CurrentPage must never become negative or zero while pages exist

Do not leave the Dashboard on an empty page when earlier valid pages exist.

---

## 7. Reset-to-first-page rules

Reset `_currentPage` to 1 when any input that materially changes the result set changes.

At minimum:

- search query changes
- selected group/filter changes
- page size changes

Apply the reset before rendering the new paginated subset.

Do not reset the page because of unrelated UI interactions.

---

## 8. Pagination controls

When more than one page exists, provide controls for:

- first page
- previous page
- next page
- last page

Use concise Persian labels or accessible icon/button combinations consistent with the application's current visual style.

Recommended visible labels:

- `ابتدا`
- `قبلی`
- `بعدی`
- `انتها`

Disable:

- First/Previous on the first page
- Next/Last on the final page

Disabled state must reflect actual HTML disabled semantics, not only CSS styling.

Do not introduce a pagination library.

Numbered page buttons are not required.

Keep the control compact.

---

## 9. Result summary

Display a concise summary near the pagination controls.

At minimum show:

- total matching results
- current page
- total pages when results exist

Use Persian wording consistent with the existing UI, for example:

`۴۷ نتیجه — صفحه ۲ از ۳`

If useful, also show the visible range:

`نمایش ۲۱ تا ۴۰ از ۴۷ نتیجه`

Do not clutter the UI by showing redundant summaries in multiple locations.

---

## 10. Page-size selector

Provide a compact page-size selector with:

- 20
- 50
- 100

Use a native `<select>`.

Use an accessible Persian label such as:

`تعداد در هر صفحه`

Changing page size must:

1. update `_pageSize`
2. reset `_currentPage` to 1
3. recalculate displayed results

Do not add custom JavaScript.

---

## 11. Search/filter interaction

Pagination must work on the final result set after the existing search and Dashboard filters have been applied.

Correct conceptual order:

1. execute Application search
2. apply existing Dashboard-level filters, if any
3. obtain the complete final ordered result collection
4. calculate pagination metadata
5. render only the current page

Do not paginate before applying filters.

Do not re-run or alter the search algorithm because the user navigated between pages.

---

## 12. Empty state

When no results match:

- hide or disable unnecessary pagination controls
- show a clear Persian empty state

Reuse the current empty-state design if one already exists.

Otherwise use wording consistent with:

`موردی مطابق جست‌وجو و فیلترهای انتخاب‌شده پیدا نشد.`

Do not display zero-result pagination such as:

`صفحه ۱ از ۰`

---

## 13. Responsive behavior

Update `app.css` only as needed.

Pagination must work cleanly on desktop and mobile.

On narrow screens:

- controls may wrap
- summary and page-size selector may move to separate lines
- buttons must remain comfortably tappable
- no horizontal overflow should be introduced

Keep styling consistent with the current visual language.

Do not redesign the Dashboard.

Do not introduce a CSS framework.

---

## 14. Accessibility

Use semantic native controls.

Requirements:

- `<button>` for navigation actions
- native `<select>` for page size
- disabled buttons use the `disabled` attribute
- meaningful accessible names
- keyboard navigation works naturally
- visible focus styling remains intact

If arrow icons are used, they must not be the only accessible description.

Pagination should be placed in an appropriate navigation/container region with a meaningful accessible label where practical.

---

## 15. Persian / RTL behavior

Preserve existing RTL behavior.

Ensure First/Previous/Next/Last controls are visually understandable in RTL layout.

Do not rely only on arrow direction to communicate meaning; Persian text labels should make the action clear.

Preserve the project's existing digit-display conventions where practical.

---

## 16. Performance

Do not optimize prematurely.

The source collection is already in memory and small.

Using:

- collection count
- `Skip`
- `Take`

is sufficient.

Do not add:

- caching infrastructure
- virtualized lists
- database pagination
- infinite scrolling
- JavaScript paging
- external UI components

unless the current repository has materially changed in a way that makes the stated approach invalid.

---

## 17. Testing

Do not introduce UI-testing infrastructure solely for pagination.

If pagination calculations remain simple and local to `Dashboard.razor`, no new automated test project/helper is required.

If implementation extracts non-trivial pagination/clamping logic into a reusable pure helper, add focused unit tests for that helper covering at least:

- exact page boundary
- partial final page
- current page above new TotalPages gets clamped
- zero results
- page-size change/reset behavior where applicable

Do not extract a helper merely to make it testable.

Prefer the simplest implementation.

Existing tests must continue to pass.

---

## 18. Implementation discipline

Inspect the current Dashboard first.

Reuse existing:

- search service
- result model
- filtering logic
- loading state
- empty state
- CSS conventions

Do not duplicate them.

Prefer changing only Dashboard and its CSS.

Do not perform unrelated refactoring.

Do not change Phase 5 search behavior while implementing pagination.

Build after the change and run the existing test suite.

---

## Definition of done

This task is complete when:

- Dashboard no longer permanently truncates results to the first 20
- default page size is 20
- users can select 20, 50, or 100 results per page
- First/Previous/Next/Last navigation works correctly
- query/filter/page-size changes reset to page 1
- page number is clamped when result counts shrink
- total result count and current/total page information are visible
- zero-result state does not display invalid pagination
- Phase 5 ranking/order is preserved exactly
- no unnecessary Application/Infrastructure/database changes are introduced
- Dashboard remains responsive, RTL-friendly and keyboard accessible
- the solution builds successfully
- existing tests pass