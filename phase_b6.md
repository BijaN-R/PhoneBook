# PROMPT — Phase 6: Operational Admin UX

Continue from the **current repository state** after all previous phases and later administrator-management work.

Before changing code, inspect the current implementation. The repository is the source of truth; do not assume older phase prompts exactly match the current code.

The goal is to improve day-to-day administration of **phone-book data** without redesigning the application, replacing the existing architecture, or reimplementing completed features.

Use the existing Razor/Blazor stack and CSS.

Do not add a SPA framework, large UI library, toast/notification library, drag-and-drop dependency, or other unnecessary package.

---

## 1. Preserve completed Identity/User management

The `/admin/users` feature is already complete and includes:

- user listing and creation
- Admin/User role assignment
- search and role filtering
- active/disabled state using Identity lockout
- enable/disable
- password reset
- deletion with confirmation
- protection against deleting/disabling the signed-in administrator
- protection against deleting, disabling, or demoting the last active administrator
- responsive management UI

Treat this functionality as **out of scope**.

Do not recreate, redesign, move, or broadly refactor:

- `Users.razor`
- `AdminBootstrapper`
- ASP.NET Core Identity setup
- login/logout
- roles
- password/account lifecycle behavior

Preserve the existing Users navigation and authorization behavior.

Only make minimal changes there if required by shared navigation/layout changes.

In this phase, terms such as activate/deactivate, delete, bulk operations, ordering, and data quality refer to **PhoneBook groups/entries**, not Identity users.

---

## 2. Implementation discipline

Before creating a new service, model, helper, component, or CSS pattern, inspect the existing code and extend/reuse existing responsibilities where appropriate.

Avoid:

- duplicate abstractions
- unrelated refactoring
- broad renaming
- formatting-only rewrites
- speculative architecture changes

Prefer the smallest set of changes needed.

UI state stays in Razor components.

Do not put phone-book business rules, EF Core access, DbContext instances, or persistence logic into Razor components.

---

## 3. Primary implementation areas

Primary existing pages:

- `Groups.razor`
- `Entries.razor`
- `Settings.razor`
- `MainLayout.razor` only where necessary for PhoneBook admin navigation

Do not create new top-level admin pages unless strictly necessary.

Do not move data-quality functionality to Dashboard; keep it inside Settings.

Treat existing `phase_*.md` / `phase_b*.md` files as reference/history only and do not edit them.

---

## 4. Settings structure

Keep one Settings route/page.

Organize it into four in-page Blazor tabs:

1. `نمایش و صفحه‌آرایی`
2. `تنظیمات عمومی`
3. `مدیریت داده‌ها`
4. `عملیات مدیریتی`

Use component-local state; do not add a JS tabs package.

Use semantic accessible controls and selected-state/ARIA information.

Place:

- existing layout/display settings → `نمایش و صفحه‌آرایی`
- general settings → `تنظیمات عمومی`
- Phase 4 JSON backup/import → `مدیریت داده‌ها`
- data-quality analysis → `عملیات مدیریتی`

---

## 5. Operation feedback

Use inline page-local feedback, not a global toast system.

Create or reuse a small component such as:

`OperationFeedback`

Support:

- success
- warning/information
- error

Use:

- `role="status"` for normal feedback
- `role="alert"` for blocking errors where appropriate

Reuse it across Groups, Entries, Settings and import operations.

---

## 6. Group management

Improve `Groups.razor` to clearly show:

- title
- active/inactive state
- priority
- preferred column
- Required status
- display order

Support:

- create/edit
- activate/deactivate
- move up/down
- move to top/bottom
- hard delete when allowed

Do not add drag-and-drop.

### Ordering safety

`PhoneBookGroup.DisplayOrder` is uniquely constrained.

Group reorder operations must be transactional and collision-safe.

Do not perform a naive two-value swap.

Use temporary out-of-range values or an equivalent safe transactional strategy before assigning final order values.

For move-to-top/bottom, safely shift all affected groups in one transaction.

Do not rely on EF update ordering to avoid unique-index collisions.

Application decides the requested move; Infrastructure performs persistence mechanics.

### Priority

`Priority` is also unique.

Do not change Priority when DisplayOrder changes.

If the current UI already allows explicit Priority editing, preserve it.

If a requested Priority is already used:

- reject it clearly
- do not silently swap
- do not auto-renumber unrelated groups

Do not interpret `PriorityTopLimit` as the maximum allowed Priority.

---

## 7. Group deletion rules

Prefer deactivation over hard deletion.

Enforce these rules in Application, not only in UI:

### Required group

If:

`Required == true`

hard delete is forbidden.

To become deletable, the admin must explicitly change it to:

`Required == false`

### Non-required group

Hard delete is allowed only when the group contains **no entries**, including inactive entries.

Do not cascade-delete entries through normal administrator group deletion.

Require explicit confirmation.

Use Persian wording consistent with:

`آیا از حذف «{name}» مطمئن هستید؟ این عملیات قابل بازگشت نیست.`

---

## 8. Entry management

Improve `Entries.razor` with:

- group filtering
- active/inactive filtering
- create/edit forms
- activate/deactivate
- move up/down
- move to top/bottom
- bulk activate/deactivate
- destructive deletion with confirmation

Preserve the existing collision-safe entry ordering behavior.

Move-to-top/bottom is explicitly in scope.

Do not add drag-and-drop.

---

## 9. Bulk selection and operations

Selection state belongs inside `Entries.razor`, for example an in-memory set of selected Entry IDs.

Use native checkboxes.

Each row checkbox must have a meaningful accessible label.

Provide a header select-all checkbox.

Select-all affects only **currently visible entries under the active filters**.

When filters change, remove selections no longer visible.

Bulk activate/deactivate must:

- run as one logical operation
- execute in one transaction
- be all-or-nothing
- apply concurrency checks to all selected records
- refresh search index only after successful commit

If any selected entry:

- no longer exists
- has stale Revision
- cannot be modified

fail the entire operation.

Do not silently partially succeed.

Expose the operation through Application, e.g. one bulk state-change method or equivalent.

---

## 10. Concurrency UX

Reuse the Phase 3 concurrency mechanism:

- numeric `Revision`
- Revision configured as EF concurrency token
- update/delete commands carry the originally loaded revision
- stale writes surface as `ConcurrencyConflictException` or the equivalent existing application-level conflict type

Verify that `Revision` exists on `PhoneBookGroup`, `PhoneBookEntry`, and `AppSettings`, is configured in `AppDbContext` as an EF Core concurrency token, and is present in the current database schema/migrations.

If the current implementation already satisfies this, reuse it unchanged.

If any part is missing, add only the minimum concurrency configuration required by this phase. A focused migration that only adds/fixes the required `Revision` columns or concurrency metadata is allowed and is not considered a migration/deployment strategy redesign.

Do not redesign the migration strategy, recreate existing concurrency infrastructure, or introduce a second concurrency mechanism.

Do not invent another concurrency mechanism.

When a stale edit/delete/reorder/bulk operation occurs:

- do not overwrite newer data
- do not expose EF/SQLite errors
- keep the administrator in the relevant context
- show a Persian conflict message such as:

`این مورد توسط کاربر دیگری تغییر کرده است. لطفاً آخرین اطلاعات را بارگذاری کنید.`

Provide:

`بارگذاری مجدد`

For editable forms, Reload Latest must:

1. warn if unsaved changes exist
2. after confirmation, discard them
3. refetch the entity
4. repopulate the form with current values and current Revision

Do not implement automatic merge.

---

## 11. Data-quality analysis

Create or extend:

`IPhoneBookDataQualityService`

with an implementation such as:

`PhoneBookDataQualityService`

Expose a read-only operation equivalent to:

`AnalyzeAsync`

It must never modify data.

Use a result model equivalent to:

`DataQualityReport`
- `IReadOnlyList<DataQualityIssue> Issues`

Each issue must contain:

- Severity
- Category
- GroupId when applicable
- EntryId when applicable
- Persian Message

Use at least:

- Warning
- Error

### Required checks

#### Error

Entry where both normalized Name and Extension are empty.

An empty Name with a valid Extension is legitimate.

#### Warning — exact duplicate person

Within the same group:

- same normalized Name
- same normalized non-authoritative Extension value

between different entries.

#### Warning — repeated extension

Same non-empty normalized Extension appears on multiple entries.

This is warning-only because shared extensions may be legitimate.

#### Warning — near duplicate name

Within the same group only.

Use Phase 5 Core normalization and OSA distance.

Flag only when:

- normalized names differ
- both names length >= 4
- OSA distance == 1

Do not use distance 2.

Do not use the full search ranking algorithm.

Do not automatically merge duplicates.

#### Ordering issues

Report:

- Group DisplayOrder <= 0
- Entry DisplayOrder <= 0
- Priority <= 0
- duplicate logical ordering values if inconsistent/imported data contains them

Do not treat order gaps as corruption.

Do not treat `Priority > PriorityTopLimit` as invalid.

### UI

Place this under:

`Settings → عملیات مدیریتی`

Run analysis only when the administrator requests it.

Provide:

`بررسی کیفیت داده‌ها`

Show severity, issue count, concise message, and relevant group/entry context.

Do not create a separate top-level page.

---

## 12. Import / export UX

Preserve the working Phase 4 transfer services, serializers, endpoints and models.

Do not rewrite them merely for UX.

Under:

`Settings → مدیریت داده‌ها`

implement this flow:

1. choose JSON file
2. validate
3. show preview
4. display Errors and Warnings separately
5. explicit Replace confirmation
6. apply import
7. show completion result

Reuse existing models if they already represent this.

Otherwise use an equivalent result shape containing:

- Errors
- Warnings
- PreviewCounts

Rules:

- Errors block import
- Warnings do not block import
- Preview must not mutate data
- Warnings must be visible before destructive confirmation

Use destructive wording consistent with:

`این عملیات تمام داده‌های فعلی دفترچه تلفن را با محتوای فایل جایگزین می‌کند و قابل بازگشت نیست.`

After successful import:

- show resulting counts
- refresh dependent search/index state once after commit
- clear obsolete preview/selection state

Do not change public DOCX/PDF/PNG exports.

---

## 13. Search UX

Use the ranked Application search from Phase 5.

Do not modify Phase 5 matching/ranking logic.

Do not display the internal numeric Score.

Optionally show a small Persian MatchKind hint such as:

- `تطابق دقیق`
- `تطابق تقریبی`

only if it improves clarity; otherwise omit it.

Do not add a diagnostics column.

---

## 14. Navigation and authorization

Preserve existing:

- authentication
- administrator state
- logout
- Users navigation
- Admin role/policy behavior

Do not rebuild them.

Any new/reorganized **PhoneBook admin navigation** must follow the existing authorization-aware pattern.

Anonymous users must not see administrative links.

Authorization must remain enforced on destination components/use cases, not only by hiding navigation.

---

## 15. Loading, empty and submit states

For async loads, show a lightweight loading indicator.

When a filtered/list view is empty, show a clear Persian empty state such as:

`موردی با فیلترهای انتخاب‌شده پیدا نشد.`

During async mutations/import/bulk actions:

- prevent double submission
- disable only relevant controls
- do not unnecessarily freeze the entire page

Do not create a global loading-state system.

---

## 16. Forms, messages and accessibility

Use clear field-level validation.

Application/Domain remains the source of truth for business validation.

Preserve Persian RTL behavior.

Require confirmation for destructive actions.

Use concise Persian wording consistent with:

Concurrency:
`این مورد توسط کاربر دیگری تغییر کرده است. لطفاً آخرین اطلاعات را بارگذاری کنید.`

Delete:
`آیا از حذف «{name}» مطمئن هستید؟ این عملیات قابل بازگشت نیست.`

Import:
`این عملیات تمام داده‌های فعلی دفترچه تلفن را جایگزین می‌کند و قابل بازگشت نیست.`

Maintain:

- keyboard usability
- semantic controls
- native checkboxes
- meaningful labels
- visible focus
- RTL consistency
- responsive behavior

Avoid custom clickable divs when native controls are sufficient.

---

## 17. Architecture boundaries

### Web

Owns UI-only state:

- active tab
- filters
- selections
- confirmation state
- loading state
- feedback rendering

### Application

Owns:

- deletion rules
- Required-group rules
- activation/deactivation
- bulk operation semantics
- reordering use cases
- concurrency outcomes
- data-quality analysis
- business validation

### Infrastructure

Owns:

- EF Core
- SQLite
- transactions
- unique-order collision handling
- concurrency persistence mechanics

### Core

Owns reusable technical algorithms, including Persian normalization and OSA distance.

Do not keep long-lived DbContext instances in services/components.

---

## 18. Focused tests

Do not start a broad testing initiative.

Add/update only focused tests required for new business-sensitive behavior.

Cover at minimum:

- Required group cannot be hard-deleted
- non-required group with entries cannot be hard-deleted
- group reorder does not violate unique DisplayOrder
- duplicate Priority is rejected
- bulk activate/deactivate is all-or-nothing
- bulk concurrency conflict causes no partial commit
- exact duplicate detection
- repeated-extension warning
- OSA distance-1 near-name warning
- distance-2 names are not reported as near duplicates

Use existing testing conventions.

Do not add UI automation infrastructure solely for this phase.

Existing tests must continue to pass.

---

## 19. Execution order

Implement incrementally:

1. inspect current Phase 3/4/5 code and current pages/services
2. Application business rules
3. Infrastructure transactional/order changes
4. focused backend tests
5. reusable feedback component
6. Groups UX
7. Entries UX and bulk operations
8. Settings tabs
9. data-quality UI
10. import UX polish
11. final CSS/accessibility polish

Build after each major backend/application step and fix compile errors before continuing.

Do not produce a large speculative rewrite across all layers before validating earlier steps.

Before completion:

- build the full solution
- run the full existing test suite
- fix regressions introduced by this phase

---

## 20. Out of scope

Do not implement or redesign:

- Identity/user management
- authentication architecture
- logging
- deployment/migration strategy
- automatic deduplication
- automatic conflict merging
- drag-and-drop
- SPA framework
- global toast framework
- major visual rebranding
- new search algorithm
- DOCX/PDF/PNG generation
- broad test infrastructure

---

## Definition of done

Phase 6 is complete when:

- Groups and Entries have safe, practical administrative UX
- reorder operations cannot violate unique DB constraints
- Required/non-empty group deletion rules are enforced by Application
- bulk entry state changes are transactional and concurrency-safe
- stale writes never overwrite newer data
- Settings is organized into the specified four tabs
- JSON import has validation, preview, warnings/errors, confirmation and completion feedback
- data-quality analysis is on-demand, deterministic and non-destructive
- loading, empty, feedback and double-submit states are handled
- existing Identity/User-management remains intact
- existing Phase 4 import/export and Phase 5 search logic are reused rather than rewritten
- the solution builds successfully
- all existing and focused new tests pass