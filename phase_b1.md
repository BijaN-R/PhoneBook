# PROMPT — Phase 1: Application Layer & Thin Web

You are working on an existing .NET 10 / C# 14 PhoneBook solution. Before changing anything, inspect the existing solution, project references, Razor components, services, domain entities, EF Core infrastructure, Core layout/text code, and export projects.

The goal of this phase is architectural refactoring only. Preserve existing observable behavior unless a change is explicitly required below.

## Main architectural goal

Apply this principle:

“Thin the Web layer, move real application use cases into an Application layer, keep persistence details inside Infrastructure, and change Domain/Core/Export only where their responsibilities are genuinely misplaced.”

Do not turn the project into an over-engineered Clean Architecture implementation.

Do not introduce MediatR, CQRS frameworks, AutoMapper, FluentValidation, generic repository frameworks, event buses, or other abstraction-heavy packages.

Use standard .NET dependency injection and the libraries already present in the solution.

## Required new project

Create:

`PhoneBook.Application`

Target the same framework and language version as the rest of the solution.

References:

- PhoneBook.Domain
- PhoneBook.Core

It must NOT reference:

- PhoneBook.Web
- PhoneBook.Infrastructure
- EF Core
- SQLite
- Razor/ASP.NET presentation types
- concrete Export projects

Infrastructure may reference Application in order to implement persistence abstractions defined there.

Web may reference Application and Infrastructure and remains the composition root.

## Application responsibilities

Move use-case orchestration out of Web.

Create application-level abstractions and services with clear responsibilities.

### Persistence abstractions

Create an `IPhoneBookRepository` abstraction in Application.

It should expose only persistence capabilities required by application services, including:

- loading the document header
- loading all groups, optionally active-only
- loading one group
- loading entries for a group
- inserting/updating/deleting a group
- inserting/updating/deleting an entry
- obtaining data required to determine ordering and priorities
- atomically persisting an entry-order swap
- loading the lightweight active records required by search

Do not place EF-specific concepts such as DbContext, IQueryable, EntityEntry, DbUpdateException, or tracking behavior in this interface.

Create an `IAppSettingsRepository` abstraction with responsibilities for:

- loading application settings
- inserting/updating application settings

### Application services

Create the following services or equivalently named services with the same clear responsibilities:

`PhoneBookQueryService`
- GetDocumentHeaderAsync
- GetGroupsAsync
- GetGroupAsync
- GetEntriesAsync

`GroupManagementService`
- CreateGroupAsync
- UpdateGroupAsync
- DeleteGroupAsync

It owns application rules related to group creation/update such as assigning missing DisplayOrder or Priority.

`EntryManagementService`
- CreateEntryAsync
- UpdateEntryAsync
- DeleteEntryAsync
- MoveEntryAsync

For MoveEntryAsync, Application decides which adjacent entry is the neighbor. Infrastructure only performs the final atomic persistence operation.

`SettingsService`
- GetAsync
- UpdateAsync

Preserve the useful caching behavior of the current AppSettingsService, but keep database access behind `IAppSettingsRepository`.

`PhoneBookSearchService`
- RefreshAsync
- Search

Keep the existing search behavior in this phase. Search quality will be redesigned in a later phase.

The service may maintain its current in-memory index, but it must obtain data through the Application persistence abstraction and must not reference AppDbContext.

`PhoneBookDocumentService`
- prepare the complete read model required by Preview/export: header, active groups, settings and layout result.

This service may use LayoutEngine from Core.

Do not move low-level DOCX/PDF/PNG rendering into Application.

## Infrastructure changes

Move the persistence implementation currently embedded in `PhoneBook.Web.Services.PhoneBookRepository` to Infrastructure.

Create concrete EF implementations such as:

- `EfPhoneBookRepository`
- `EfAppSettingsRepository`

Exact naming may vary if a clearer existing convention exists.

These classes may use:

- IDbContextFactory<AppDbContext>
- EF Core
- SQLite
- transactions
- AsNoTracking
- SaveChangesAsync

The repository implementation must be responsible for persistence mechanics, not UI concerns.

## Web changes

After this phase, Razor components must no longer directly depend on:

- AppDbContext
- IDbContextFactory<AppDbContext>
- concrete EF repositories
- PhoneBook.Web.Services.PhoneBookRepository
- database-specific services

Pages must call Application services.

`Program.cs` remains the composition root and registers Application services and Infrastructure implementations.

Low-level export generators may still be registered in Web because Web is the composition root, but UI pages should obtain their data/layout from `PhoneBookDocumentService` rather than rebuilding the use case themselves.

Remove obsolete Web services once all consumers have been migrated.

Keep only genuinely presentation-specific services in `PhoneBook.Web`.

## Dependency rules

The intended dependency direction is:

Web → Application

Web → Infrastructure only for DI/composition

Infrastructure → Application

Application → Domain/Core

Export projects → Domain/Core as currently appropriate

Domain and Core must not depend on Web or Infrastructure.

Avoid circular project references.

## Important constraints

Do not change authentication in this phase.

Do not implement logging work.

Do not change deployment or migration strategy.

Do not redesign the search algorithm yet.

Do not redesign PDF/DOCX/PNG rendering.

Do not add a new test suite. Existing tests must continue to compile and pass; only make minimal structural test changes if namespaces or project boundaries require them.

Do not rewrite working code merely for style.

## Completion criteria

The phase is complete when:

- `PhoneBook.Application` exists.
- Real application use cases no longer live inside Web.
- Application has no EF Core/SQLite/Web dependency.
- Infrastructure owns concrete database access.
- Razor components consume Application services.
- Existing user-visible behavior remains functionally equivalent.
- The old Web repository/settings/search implementations are removed or reduced to genuine UI-only code.
- The full solution builds successfully.
- Existing tests pass.

