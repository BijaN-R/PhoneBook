# PROMPT — Phase 2: Admin Authentication & Authorization

Continue from the architecture produced in Phase 1.

The application must remain publicly readable: ordinary users do not need an account to search/view the phone book or download the normal DOCX/PDF/PNG outputs.

Only administrators may access or perform management operations.

## Security model

Use ASP.NET Core Identity with cookie authentication.

Do NOT create a custom password storage or hashing system.

Use the same SQLite database unless there is a strong technical reason not to.

Use the ASP.NET Core Identity EF Core integration package compatible with the project's .NET/EF Core 10 package line.

Do not introduce JWT authentication. This is a server-rendered/internal Blazor application and cookie authentication is sufficient.

## Identity persistence

Add an Identity user type named approximately:

`ApplicationUser`

Keep Identity-specific types outside PhoneBook.Domain. Identity is infrastructure/security concern, not phone-book domain data.

Adapt AppDbContext to support ASP.NET Core Identity persistence while preserving the existing phone-book tables.

Create the necessary schema migration for Identity tables, but do not redesign deployment/migration automation in this phase.

## Roles and policies

Create exactly one required application role:

`Admin`

Create a named authorization policy:

`AdminOnly`

The policy must require the Admin role.

Use the policy consistently instead of scattering hard-coded authorization rules through components.

## Administrator bootstrap

There must be no public registration page.

Create an administrator bootstrap mechanism, for example an `AdminBootstrapper`.

Responsibilities:

- Ensure the Admin role exists.
- If there is no administrator account yet, optionally create the first administrator from configuration supplied through environment variables or .NET User Secrets.
- Never place an administrator password in source code, migrations, seed data, or committed appsettings files.
- After an administrator already exists, bootstrap credentials must not overwrite its password.

Use Identity's password hashing and password validation.

## Required authentication UI

Provide:

- Admin login page
- Logout action
- Access-denied handling

Do not provide:

- public signup
- self-service role assignment
- anonymous password reset workflow unless it is clearly required by existing infrastructure

Keep the authentication UI visually consistent with the current Persian/RTL application.

## Route protection

The following management areas must require the `AdminOnly` policy:

- Groups management
- Entries management
- Settings
- future import/export and backup/restore admin functions

If any create/edit/delete controls exist elsewhere, those actions must also require Admin authorization.

Normal users must not merely receive disabled controls: unauthorized users must be unable to execute the protected operation even by navigating directly to a URL.

Use route/component authorization such as Authorize attributes/policies and an authorization-aware router.

## Navigation

Use authorization-aware rendering so anonymous users do not see links to:

- Groups management
- Entries management
- Settings
- other admin-only areas

Provide an unobtrusive admin login entry point.

When logged in as Admin, show the management navigation and logout option.

Remember that hiding navigation is only a UX measure; destination components/actions must be independently protected.

## Public features

Keep these anonymous/public unless the current code reveals a security-sensitive mutation inside them:

- Dashboard
- phone-book search
- Preview
- DOCX export
- PDF export
- PNG/image export

If a public component contains both read and mutation behavior, separate those responsibilities rather than making the entire public feature admin-only.

## Cookie configuration

Use standard secure cookie practices:

- HttpOnly
- Secure when served over HTTPS
- appropriate SameSite setting
- sensible expiration
- sliding expiration if appropriate

Enable normal Identity lockout protections for repeated failed login attempts.

Do not add MFA in this phase.

## Architectural requirement

Identity/security implementation must not cause PhoneBook.Domain or Core to depend on ASP.NET Core Identity.

Application phone-book use cases should remain independent from authentication implementation.

Authorization belongs at the Web/application boundary.

## Out of scope

Do not implement audit logging.

Do not implement general logging changes.

Do not implement import/export in this phase.

Do not redesign migrations/deployment.

Do not add broad new test coverage.

Existing behavior for anonymous users must remain available.

## Completion criteria

The phase is complete when:

- anonymous visitors can use all normal read/search/export functionality.
- anonymous visitors cannot see admin navigation.
- direct navigation to admin components requires login.
- logged-in non-admin users cannot access AdminOnly components.
- administrators can access Groups, Entries and Settings.
- no public registration exists.
- no password is committed to source/appsettings/migrations.
- Identity data is persisted in SQLite.
- the solution builds and existing tests continue to pass.