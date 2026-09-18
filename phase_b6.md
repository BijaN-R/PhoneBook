# PROMPT — Phase 6: Operational Admin UX

Continue from all previous phases.

Improve the administration experience without adding a large UI framework or changing the application's overall visual identity.

The goal is practical day-to-day manageability, not a redesign.

Prefer the existing Razor/Blazor stack and CSS.

Do not add a JavaScript SPA framework.

## Admin information architecture

Keep the public phone-book experience simple.

Admin-only areas should be clearly separated.

Organize Settings into logical sections such as:

- نمایش و صفحه‌آرایی
- تنظیمات عمومی
- مدیریت داده‌ها
- عملیات مدیریتی

The JSON backup/import functionality from Phase 4 belongs in `مدیریت داده‌ها`.

## Group management UX

Improve Groups management with:

- clear active/inactive state
- activate/deactivate action
- clear ordering controls
- display of priority and preferred column
- explicit indication of Required groups
- confirmation before destructive deletion

Prefer deactivation/archive over hard deletion for normal administrative use.

Hard deletion may remain available as a secondary action where allowed.

A Required group must not be casually hard-deleted.

Use application rules rather than only disabling a UI button.

## Entry management UX

Improve Entries management with:

- active/inactive filtering
- group filtering where useful
- clear edit/create forms
- clear ordering controls
- activate/deactivate actions
- bulk activate/deactivate for selected entries
- explicit confirmation for destructive actions

Do not add a drag-and-drop dependency.

Existing move-up/move-down behavior is sufficient and should remain keyboard/accessibility friendly.

If useful, add move-to-top/move-to-bottom without third-party libraries.

## Duplicate/data-quality assistance

Create an Application service such as:

`PhoneBookDataQualityService`

It should detect and report likely issues without automatically modifying data.

Checks should include:

- identical normalized name + extension
- repeated extension as a warning
- duplicate/near duplicate names where confidence is high
- entries where both Name and Extension are empty
- invalid ordering inconsistencies if encountered

Important:

An entry with an empty Name but a valid Extension may be legitimate and must not automatically be treated as invalid.

Shared extensions may also be legitimate; display them as warnings rather than blocking saves unless a separate hard business rule exists.

Provide a small admin-facing data-quality panel/report.

Do not implement automatic deduplication.

## Concurrency UX

Integrate the concurrency behavior from Phase 3 properly.

If an admin attempts to save stale data:

- show a clear Persian message
- explain that another change occurred
- provide Reload Latest
- do not automatically overwrite

Do not expose database exception text.

## Import/export UX

Polish the Phase 4 flow:

- select JSON
- validate
- preview counts
- display errors and warnings separately
- require explicit destructive confirmation for Replace
- show completion summary

Do not make import a one-click destructive action.

## Search UX

Use the ranked results produced by Phase 5.

Do not expose internal numeric score unless useful for diagnostics.

Optionally indicate why a result matched only if it improves usability.

Keep the interface uncluttered.

## Admin navigation/security

Admin-only links must continue to be invisible to anonymous users.

Show a clear logged-in administrator state and logout action.

Do not duplicate authorization logic in every UI component where an existing shared policy can be used.

## Feedback

For successful operations provide concise UI feedback for:

- create
- update
- activate/deactivate
- delete
- reorder
- import
- settings save

Use the simplest existing application mechanism.

Do not introduce a large notification/toast library solely for this.

## Forms and validation

Use clear field-level validation messages.

Preserve Persian RTL layout.

Prevent accidental double submission while an async save is in progress.

Disable only the relevant action while saving rather than freezing the entire UI unnecessarily.

## Accessibility/usability

Maintain:

- usable keyboard navigation
- meaningful labels
- visible focus behavior
- reasonable button text
- responsive layouts where practical
- RTL consistency

Do not replace semantic controls with custom div-based widgets unnecessarily.

## Architecture

UI-specific state remains in Web.

Business decisions such as whether an item can be deleted, archived, duplicated or reordered remain in Application/Domain.

Data-quality logic belongs in Application/Core as appropriate, not Razor code.

Persistence remains in Infrastructure.

## Out of scope

No logging project.

No deployment changes.

No general migration strategy redesign.

No broad test initiative.

No SPA framework.

No major visual rebranding.

No automatic deduplication.

## Completion criteria

The phase is complete when an administrator can comfortably manage groups, entries, settings and data transfer without needing database knowledge, while anonymous users retain a clean read-only phone-book experience.
