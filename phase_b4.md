# PROMPT — Phase 4: Admin JSON Backup, Restore, Import & Export

Continue from Phases 1–3.

Implement a portable administrator-only phone-book data transfer feature.

Use JSON as the canonical data exchange format.

Do not implement CSV or YAML in this phase.

Use `System.Text.Json`. Do not add a third-party serializer.

## Design principle

Treat JSON export as a logical backup of phone-book application data.

Treat validated replace-import as logical restore.

This logical backup intentionally excludes ASP.NET Identity users, roles, password hashes, authentication cookies and other security credentials.

Administrator accounts are security configuration, not phone-book business data.

A raw SQLite database backup is NOT part of this phase.

## JSON document format

Create a versioned transfer document with a root structure conceptually containing:

- schemaVersion
- exportedAtUtc
- application identifier/version if available
- documentHeader
- appSettings
- groups

Each group contains its own entries.

Do not make database primary keys authoritative in the exchange format.

The document must preserve business data needed to reconstruct the phone book, including:

Group:
- title
- priority
- preferredColumn
- displayOrder
- required
- keepTogether
- isActive

Entry:
- name
- extension
- displayOrder
- isActive

Settings:
- all existing layout/settings values required to reproduce the current output

Header:
- title
- subtitle
- updated date/value

Revision concurrency values must not be imported as authoritative state. Imported records receive fresh revisions.

## Application models

Create transfer models separated from Domain entities, for example:

- PhoneBookTransferDocument
- PhoneBookTransferGroup
- PhoneBookTransferEntry
- PhoneBookTransferSettings
- PhoneBookTransferHeader

Create result/preview models such as:

- PhoneBookImportPreview
- PhoneBookImportIssue
- PhoneBookImportSummary

An import issue must distinguish at minimum:

- Error
- Warning

## Application service

Create an application service such as:

`PhoneBookDataTransferService`

Required operations:

`ExportAsync`
- loads a consistent snapshot of phone-book data
- creates the transfer document
- returns data suitable for download

`ValidateImportAsync`
- parses and validates an uploaded JSON document
- performs no database mutation
- returns a preview/validation result

`ApplyImportAsync`
- receives already validated transfer data
- replaces phone-book business data in one transaction
- leaves Identity/security data untouched

For this phase, implement only a clear `Replace` import mode.

Do not implement fuzzy merge/upsert behavior. Matching imported people against existing people creates ambiguity and should be designed separately if ever needed.

## Serialization

Place JSON serialization/deserialization behind a technical abstraction or Infrastructure implementation such as:

`JsonPhoneBookTransferSerializer`

Use UTF-8.

Use camelCase JSON naming.

Set a reasonable maximum upload size, approximately 5 MB unless the current data volume requires slightly more.

Reject unsupported schema versions.

The current format should use `schemaVersion = 1`.

Use a strict-enough deserialization policy to catch malformed transfer files rather than silently accepting structurally invalid documents.

## Validation

Validation must happen before mutation.

Validate at minimum:

- supported schemaVersion
- required root sections
- field length limits consistent with EF model rules
- required group titles
- valid Priority values
- valid DisplayOrder values
- unique group ordering where required
- unique group priorities where required
- valid enum values
- valid settings ranges
- entries must contain at least a name or extension
- no duplicate DisplayOrder within the same group
- structural consistency

Duplicate extensions should normally produce a warning, not an error.

Potentially duplicate people, such as identical normalized name + extension, should also produce warnings.

## Import preview

Before ApplyImportAsync is allowed, the Settings UI must show a preview including:

- number of groups
- number of entries
- active/inactive counts where useful
- validation errors
- warnings
- confirmation that the operation will replace current phone-book business data

Do not mutate the database during preview.

Require an explicit final administrator confirmation.

## Atomic restore

ApplyImportAsync must be transactional.

Either the entire replacement succeeds or the original phone-book business data remains intact.

Do not leave half-imported groups/settings if a failure occurs.

Because concurrency protection exists from Phase 3, use an application-level data-management lock or equivalent simple mechanism so normal admin writes cannot run midway through a restore.

Do not create a distributed lock system; this is a single SQLite application.

## Admin UI

Place this feature inside Settings under a clear section such as:

`مدیریت داده‌ها`

The section is AdminOnly.

Provide:

- Export/Backup JSON
- Import/Restore JSON
- file selection
- Validate/Preview
- confirmation
- final result summary

Normal users must never see these controls.

Exported filenames should contain a recognizable name and date, for example a phonebook backup naming convention.

## Existing public exports

Do not change the normal DOCX/PDF/PNG export behavior.

Those formats remain end-user presentation exports and are separate from administrator data backup/import.

## Out of scope

No CSV.

No YAML.

No raw SQLite backup.

No Identity user backup.

No merge import.

No deployment automation.

No logging project.

No broad new tests.

## Completion criteria

The phase is complete when:

- an Admin can export the entire phone-book business configuration to one versioned JSON file.
- that JSON preserves groups, entries, settings and header.
- an Admin can upload the file and preview it without changing data.
- invalid documents cannot be applied.
- warnings are visible.
- confirmed import replaces phone-book business data atomically.
- admin accounts are unaffected.
- anonymous users cannot see or call the feature.
- public DOCX/PDF/PNG exports continue to work.