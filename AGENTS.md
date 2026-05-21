# Copilot Instructions

This repository contains a professional enterprise application built with C#, .NET, Blazor Web App and SQL Server.

These instructions must be followed whenever code is generated, modified, reviewed or refactored in this project.

The goal is not to create a weak demo application. The goal is to create a professional, secure, intuitive, maintainable and scalable foundation for a real business application.

---

## 1. Mandatory stack

Use:

- .NET 10 LTS
- Blazor Web App
- Interactive Server render mode
- SQL Server
- Entity Framework Core
- ASP.NET Core Identity
- Native Blazor components
- Custom Razor components
- HTML and CSS
- Bootstrap only if it already exists in the Blazor template and only as auxiliary CSS
- Automatic localization based on the browser language
- Responsive UI that works correctly on mobile phones, tablets, laptops and desktops
- Automated tests for all implemented features

Do not use:

- Syncfusion
- MudBlazor
- Radzen
- Telerik
- DevExpress
- Blazorise
- AntDesign
- Any external UI component library
- Multi-tenant architecture
- TenantId
- ITenantProvider
- Tenant isolation
- Database-per-tenant architecture

The application must use one normal SQL Server database.

---

## 2. Product and UX principle

Keep this principle in mind at all times:

> For the application to be excellent, it must not need to be explained. The application itself must speak for itself.

This means:

- The UI must be intuitive.
- The user must understand what to do without reading a manual.
- Main actions must be obvious.
- Navigation must be clear.
- Labels must be meaningful.
- Buttons must describe the action they perform.
- Error messages must explain what happened and how to fix it.
- Empty states must guide the user to the next step.
- Loading states must make it clear that something is happening.
- Success messages must confirm what was done.
- Dangerous actions must be clearly identified and require confirmation.
- Forms must be simple, logical and grouped by meaning.
- The application must avoid unnecessary complexity.
- The interface must feel natural on mobile, tablet, laptop and desktop.
- The application must not rely on long explanations to be usable.
- Every screen must have a clear purpose.
- Every page must answer:
  - Where am I?
  - What can I do here?
  - What should I do next?
  - What happened after I performed an action?

UX rules:

- Prefer clarity over cleverness.
- Prefer simple flows over complex flows.
- Prefer explicit labels over ambiguous icons.
- Icons may be used, but important actions must also have text when needed.
- Avoid hidden functionality.
- Avoid confusing technical language in the UI.
- Use business-friendly language.
- Keep visual hierarchy clear.
- Primary actions must be visually distinguishable.
- Secondary actions must not compete with primary actions.
- Destructive actions must be visually distinct and confirmed.
- A user should be able to use the application without training for common tasks.

The application must feel self-explanatory, professional and trustworthy.

---

## 3. Database

Use SQL Server instance:

```text
Server=.\SQLEXPRESS
```

Database naming rule:

- The database name must always be the application name followed by `DB`.
- Example: if the application is called `Workvert`, the database must be called `WorkvertDB`.
- Do not ask for a separate database name unless explicitly requested.

Development connection string format:

```text
Server=.\\SQLEXPRESS;Database=[NOME_DA_APP]DB;Trusted_Connection=True;TrustServerCertificate=True;MultipleActiveResultSets=true;
```

Rules:

- Use Entity Framework Core migrations.
- Apply migrations automatically on application startup.
- Use `Database.MigrateAsync()`.
- Never use `EnsureCreated()`.
- Never mix `EnsureCreated()` with migrations.
- If migrations fail, log a critical error and stop application startup.
- Seed initial roles and an initial admin user.
- Admin credentials must come from configuration, User Secrets or environment variables.
- Never hardcode passwords.
- Never hardcode production connection strings.
- Never hardcode tokens or secrets.
- In development, connection strings may be stored in `appsettings.Development.json`.
- In production, use environment variables, User Secrets, Azure Key Vault or another secure secret provider.
- Automatic migrations must be possible to enable or disable through configuration in production.

Expected startup migration behavior:

```csharp
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    var logger = services.GetRequiredService<ILogger<Program>>();

    try
    {
        var dbContext = services.GetRequiredService<ApplicationDbContext>();

        await dbContext.Database.MigrateAsync();

        await DatabaseSeeder.SeedAsync(services);

        logger.LogInformation("Database migrations applied successfully.");
    }
    catch (Exception ex)
    {
        logger.LogCritical(ex, "An error occurred while applying database migrations.");
        throw;
    }
}
```

---

## 4. Architecture

Use a clean, maintainable and professional architecture.

Preferred approach:

```text
Light Clean Architecture + feature-based organization
```

Recommended solution structure:

```text
src/
  [NOME_DA_APP].Web/
  [NOME_DA_APP].Application/
  [NOME_DA_APP].Domain/
  [NOME_DA_APP].Infrastructure/
  [NOME_DA_APP].Shared/

tests/
  [NOME_DA_APP].Domain.Tests/
  [NOME_DA_APP].Application.Tests/
  [NOME_DA_APP].Infrastructure.Tests/
  [NOME_DA_APP].Web.Tests/
  [NOME_DA_APP].IntegrationTests/
  [NOME_DA_APP].E2ETests/
```

Reference architecture:

- Folder structure, project organization, layer separation, naming and architecture decisions must use the project located at `C:\Work\Blazor\SBI` as the base reference whenever applicable.
- Before creating new folders, services, components, pages or data layers, compare the intended structure with that reference project and keep this solution aligned with the same organization.
- Use the reference project as an architectural and structural guide. Do not copy code blindly; adapt only patterns that fit this application's business context and security requirements.

### [NOME_DA_APP].Web

Responsibilities:

- Blazor Web App
- Razor Components
- Layouts
- Pages
- Custom UI components
- UI authorization
- Application configuration
- `Program.cs`
- Middleware
- Error boundaries
- Responsive layout and device adaptation
- Localization resources for UI text

Rules:

- Do not place business logic in Razor components.
- Razor components must call application services.
- Do not access `DbContext` directly from Razor components.
- Do not expose EF Core entities directly to the UI.

### [NOME_DA_APP].Application

Responsibilities:

- Use cases
- Application services
- DTOs
- ViewModels
- Validations
- Interfaces when needed
- Application rules
- Pagination, filters and result objects
- Localized validation messages where applicable

Rules:

- Must not depend on the Web layer.
- Must not contain Blazor-specific code.
- Must not return EF Core entities to the UI.

### [NOME_DA_APP].Domain

Responsibilities:

- Entities
- Value Objects
- Enums
- Domain rules
- Domain interfaces, only when useful

Rules:

- No dependency on EF Core.
- No dependency on Blazor.
- No dependency on Infrastructure.
- No dependency on Web.

### [NOME_DA_APP].Infrastructure

Responsibilities:

- EF Core
- `ApplicationDbContext`
- Fluent API configurations
- Identity persistence
- Repository implementations, only when useful
- External service implementations
- Persistence
- Audit logging
- Initial seed

### [NOME_DA_APP].Shared

Responsibilities:

- Simple shared types
- Constants
- Generic helpers
- Result objects
- Shared models that do not belong exclusively to one layer

---

## 5. Architecture rules

Follow these rules:

- Do not put business logic in Razor components.
- Razor components must call application services.
- Do not access `DbContext` directly from Razor components.
- Do not expose EF Core entities directly to the UI.
- Use DTOs and ViewModels.
- Separate commands and queries when it improves clarity.
- Use Dependency Injection correctly.
- Avoid unnecessary abstractions.
- Do not create interfaces just for the sake of creating interfaces.
- Create interfaces only when they provide real value for testing, infrastructure replacement or decoupling.
- Keep code simple, readable and prepared for maintenance.
- Do not expose `IQueryable` to the UI.
- Do not return EF Core entities directly to Blazor components.

---

## 6. Security

Security is mandatory from the beginning.

### Authentication

- Use ASP.NET Core Identity.
- Never store passwords manually.
- Use the secure password hashing provided by Identity.
- Implement secure login and logout.
- Configure password policy.
- Configure account lockout after failed login attempts.
- Prepare the architecture for future 2FA support.
- Seed initial roles.
- Seed an initial administrator only if it does not already exist.

### Authorization

Use roles and policies.

Create at least these roles:

- Admin
- Manager
- User

Rules:

- Protect pages with authorization.
- Use `[Authorize]` where applicable.
- Verify permissions in application services.
- Do not rely only on UI-level checks.
- `AuthorizeView` may hide buttons, but server-side checks are mandatory.
- Validate permissions before executing critical operations.

### XSS protection

- Protect against XSS.
- Do not render user-provided HTML unless sanitized.
- Avoid `MarkupString` for user content.
- Use Razor default encoding.
- Treat all user input as untrusted.

### CSRF and request protection

- Protect against CSRF/forgery where endpoints or server-side forms are applicable.
- Use HTTPS.
- Use HSTS in production.
- Configure secure cookies:
  - `HttpOnly`
  - `Secure`
  - appropriate `SameSite`

### Security headers

Add security headers where appropriate:

- `Content-Security-Policy`
- `X-Content-Type-Options`
- `Referrer-Policy`
- `Permissions-Policy`
- `X-Frame-Options` or `frame-ancestors`

### CORS

If an API exists:

- CORS must be restrictive.
- Do not allow open CORS with `"*"` unless there is a clearly justified public endpoint.
- Never allow credentials with unrestricted origins.

### Rate limiting

Use rate limiting where appropriate, especially for sensitive endpoints such as:

- Login
- Password reset
- Public API endpoints
- Expensive operations

### SQL Injection

- Use EF Core correctly.
- Do not concatenate raw SQL.
- If raw SQL is required, use parameters.
- Never pass unsanitized user input into SQL strings.

---

## 7. Secrets

Rules:

- Never commit real secrets.
- Never place passwords in code.
- Never place tokens in code.
- Never place production connection strings in the repository.
- Use User Secrets in development.
- Use environment variables or a secure secret provider in production.
- `appsettings.json` must not contain real production secrets.

---

## 8. Logging and error handling

### Logging

Use:

- `ILogger`
- Structured logging

Rules:

- Do not log passwords.
- Do not log tokens.
- Do not log unnecessary sensitive data.
- Use correlation/request id when possible.
- Log relevant business and security events.

Log at least:

- Login
- Logout
- Failed login
- Created records
- Updated records
- Deleted records
- Permission changes
- Critical errors
- Applied migrations

### Error handling

Rules:

- Use global exception handling.
- Do not show stack traces to users in production.
- Show friendly messages in the UI.
- Log full technical errors internally.
- Use `ErrorBoundary` in important Blazor component areas.
- Create a generic error page.
- In development, technical details may be visible.
- In production, technical details must be hidden.

---

## 9. Audit

Create audit logs for critical actions.

Create an `AuditLog` entity.

Audit at least:

- Login
- Logout
- Failed login
- Create
- Update
- Delete
- Permission changes
- Role changes

Audit fields should include:

- User
- Operation type
- Affected entity
- Affected entity id
- Date/time
- Relevant values, without sensitive data
- IP address, if available
- User agent, if available

Main entities should include:

- `CreatedAt`
- `CreatedBy`
- `UpdatedAt`
- `UpdatedBy`
- `IsDeleted`
- `DeletedAt`
- `DeletedBy`
- `RowVersion`

---

## 10. Entity Framework Core

Rules:

- Use migrations.
- Use Fluent API for entity configuration.
- Do not use lazy loading by default.
- Use `AsNoTracking()` for read-only queries.
- Use `Select` projections to DTOs.
- Avoid N+1 queries.
- Use server-side pagination.
- Use server-side filtering.
- Use server-side sorting.
- Create indexes where appropriate.
- Use `RowVersion` or another concurrency token for important editable entities.
- Use soft delete for main entities.
- Use query filters for soft delete where appropriate.
- Use transactions when one operation changes multiple related entities.
- Do not expose `IQueryable` to the UI.
- Do not return EF Core entities directly to Blazor components.

Create a base entity with:

- `Id`
- `CreatedAt`
- `CreatedBy`
- `UpdatedAt`
- `UpdatedBy`
- `IsDeleted`
- `DeletedAt`
- `DeletedBy`
- `RowVersion`

Create at least the `Cliente` entity with:

- `Id`
- `Nome`
- `NIF`
- `Email`
- `Telefone`
- `Morada`
- `CodigoPostal`
- `Localidade`
- `Observacoes`
- `Ativo`
- `CreatedAt`
- `CreatedBy`
- `UpdatedAt`
- `UpdatedBy`
- `IsDeleted`
- `DeletedAt`
- `DeletedBy`
- `RowVersion`

---

## 11. Localization and language

The application must support automatic language selection based on the user's current browser language.

Rules:

- Use ASP.NET Core localization.
- Use `RequestLocalizationMiddleware`.
- Detect the preferred language from the browser `Accept-Language` header.
- The application must automatically use the browser language when it is supported.
- If the browser language is not supported, use the default fallback culture.
- The default fallback culture must be `pt-PT`, unless explicitly changed.
- Supported cultures must be configurable.
- At minimum, prepare the application for:
  - `pt-PT`
  - `en-US`
- Do not hardcode visible UI text directly in Razor components.
- Use resource files, such as `.resx`, for UI texts.
- Use `IStringLocalizer` or an equivalent ASP.NET Core localization mechanism.
- Localize:
  - Menus
  - Buttons
  - Page titles
  - Form labels
  - Table headers
  - Empty states
  - Loading messages
  - Error messages
  - Success messages
  - Validation messages
  - Identity/authentication messages where applicable
- Format dates, numbers and currency according to the current culture.
- Store dates in UTC where appropriate.
- Do not store localized display text in the database when stable keys/enums should be used instead.
- Business logic must not depend on translated display strings.
- If a manual language selector is added in the future, the selected language may be persisted in a cookie.
- Browser language detection must remain the default behavior when no manual language preference exists.
- Localization must work in Blazor components and application validation messages.

Expected startup behavior:

```csharp
var supportedCultures = new[] { "pt-PT", "en-US" };

var localizationOptions = new RequestLocalizationOptions()
    .SetDefaultCulture("pt-PT")
    .AddSupportedCultures(supportedCultures)
    .AddSupportedUICultures(supportedCultures);

app.UseRequestLocalization(localizationOptions);
```

Required service registration example:

```csharp
builder.Services.AddLocalization(options =>
{
    options.ResourcesPath = "Resources";
});
```

Recommended resource structure:

```text
src/[NOME_DA_APP].Web/Resources/
  Components/
  Pages/
  Shared/

src/[NOME_DA_APP].Application/Resources/
  ValidationMessages.pt-PT.resx
  ValidationMessages.en-US.resx
```

Important:

- The app must adapt automatically to the browser language.
- Do not require the user to choose a language on first access.
- Do not create a database setting for language unless explicitly requested.

---

## 12. Responsive design and device support

The application must work correctly on:

- Mobile phones
- Tablets
- Laptops
- Desktop screens
- Touch devices
- Keyboard and mouse devices

Responsive behavior is mandatory, not optional.

Rules:

- Use a mobile-first approach.
- Use responsive CSS.
- Use flexible layouts.
- Avoid fixed widths that break on smaller screens.
- Use `max-width`, `min-width`, `flex`, `grid` and responsive units where appropriate.
- Tables must be usable on mobile.
- Forms must be usable on mobile.
- Dialogs/modals must be usable on mobile.
- Navigation must be usable on mobile.
- Sidebar must collapse or adapt on smaller screens.
- Topbar must remain usable on small screens.
- Buttons and inputs must have touch-friendly sizes.
- Avoid hover-only interactions.
- Important actions must be accessible on touch devices.
- Text must remain readable on small screens.
- Content must not overflow horizontally.
- Forms must avoid excessive scrolling when possible.
- Long pages must have clear sectioning.
- Error messages must be visible near the relevant fields.
- Loading and empty states must adapt to small screens.
- The app must support modern browsers on mobile, tablet and desktop.

Minimum responsive breakpoints should be considered for:

- Small mobile screens
- Large mobile screens
- Tablets
- Laptops
- Desktop screens

Testing requirement:

- Responsive behavior must be tested manually and, where practical, with automated E2E tests.
- Important pages must be tested at mobile, tablet and desktop viewport sizes.
- At minimum, test:
  - Login
  - Dashboard
  - Clientes list
  - Cliente create/edit form
  - Cliente details
  - Modal/confirm dialog
  - Navigation/sidebar
  - Error page
  - Access denied page

UX expectation:

- The application must not feel like a desktop-only system squeezed into a mobile screen.
- It must feel intentionally designed for each device size.

---

## 13. Accessibility

Accessibility is mandatory.

The application must be usable by all users, including users who navigate with a keyboard, users with screen readers, users with low vision, users with motor limitations and users on different device sizes.

Rules:

- Follow WCAG principles where practical.
- Use semantic HTML whenever possible.
- All interactive elements must be reachable by keyboard.
- Do not remove visible focus indicators.
- Focus order must be logical.
- Buttons must be real `<button>` elements.
- Links must be real `<a>` elements.
- Form inputs must have associated labels.
- Use `aria-label`, `aria-describedby`, `aria-expanded`, `aria-controls` and `aria-live` where appropriate.
- Do not overuse ARIA when semantic HTML is enough.
- Error messages must be clearly associated with the relevant fields.
- Validation summaries must be understandable by screen readers.
- Loading states must be understandable for assistive technologies.
- Success and error alerts should use appropriate live regions when needed.
- Color must not be the only way to communicate meaning.
- Text contrast must be readable.
- Font sizes must remain readable on small screens.
- Click/tap targets must be large enough for touch devices.
- Modals and confirmation dialogs must manage focus correctly.
- When a modal opens, focus must move into it.
- When a modal closes, focus should return to the element that opened it when practical.
- Tables must use correct table semantics.
- Data tables must have clear headers.
- Icons used for actions must have accessible names.
- Images must have meaningful alt text or empty alt text when decorative.
- The app must be usable without a mouse.
- Avoid hover-only interactions.
- Avoid time-limited interactions unless necessary.
- Important flows must be tested using keyboard only.
- Important flows should be tested with screen reader-friendly markup in mind.

Accessibility testing must include at least:

- Login
- Dashboard
- Navigation/sidebar
- Clientes list
- Cliente create/edit form
- Cliente details
- Modal/confirm dialog
- Error page
- Access denied page

Accessibility is not optional. A feature is not complete if it is not accessible.

---

## 14. Privacy and GDPR

The application must respect privacy and data protection principles from the beginning.

Rules:

- Collect only the data that is necessary for the business purpose.
- Do not expose personal data unnecessarily.
- Do not log passwords, tokens, secrets or unnecessary personal data.
- Protect personal data in DTOs, ViewModels, logs, audit entries and UI responses.
- Use authorization checks before showing personal or business-sensitive data.
- Avoid returning sensitive fields to the UI unless strictly required.
- Sensitive operations must be audited.
- Audit logs must not contain passwords, tokens or unnecessary sensitive data.
- Consider data retention rules for personal data.
- Consider export, deletion and anonymization requirements where applicable.
- Prefer soft delete for important business entities.
- Permanent deletion must be explicit, restricted and auditable.
- Do not store more personal information than required.
- Do not store localized display text or personal preferences unless needed.
- Do not use production personal data in development or tests unless anonymized.
- Test data must not contain real personal data.
- Database backups must be protected.
- Access to production data must be restricted.
- Users must only see data they are authorized to see.
- Error messages must not leak personal data or internal technical details.
- Logs must be useful for diagnosis without exposing private information.
- Any future integrations with third-party services must consider data sharing and privacy impact.

GDPR-conscious behavior:

- Be able to identify where personal data is stored.
- Be able to correct personal data when required.
- Be able to delete or anonymize personal data when legally appropriate.
- Be able to export relevant personal data when required.
- Keep auditability for critical operations without storing excessive sensitive data.
- Apply privacy by design and privacy by default.

The application must be secure, privacy-aware and respectful of user data.

---

## 15. Blazor UI

Use only:

- Razor Components
- Native Blazor components
- HTML
- CSS
- `EditForm`
- `InputText`
- `InputTextArea`
- `InputSelect`
- `InputDate`
- `InputNumber`
- `ValidationMessage`
- `ValidationSummary`
- `DataAnnotationsValidator`
- Custom Razor components

Do not use external UI component libraries.

Create reusable components in:

```text
src/[NOME_DA_APP].Web/Components/App
```

Required components:

- `AppButton`
- `AppTextBox`
- `AppTextArea`
- `AppSelect`
- `AppDatePicker`
- `AppNumberInput`
- `AppCheckbox`
- `AppTable`
- `AppPagination`
- `AppModal`
- `AppCard`
- `AppPageHeader`
- `AppSearchBox`
- `AppConfirmDialog`
- `AppLoading`
- `AppEmptyState`
- `AppAlert`
- `AppValidationSummary`
- `AppBreadcrumbs`

Rules:

- Components must be simple.
- Components must be reusable.
- Components must support an additional CSS class parameter when appropriate.
- Components must support `Disabled` when applicable.
- Components must support `Loading` when applicable.
- Components must support `ChildContent` when applicable.
- Components must use proper labels.
- Components must use `aria-label` and `aria-describedby` when applicable.
- Components must respect accessibility.
- Components must not contain business logic.
- Components must be easy to replace in the future.
- Use CSS isolation where appropriate.
- Components must be responsive.
- Components must be usable with touch.
- Components must not assume a desktop-only layout.

---

## 16. Layout

Create an enterprise layout with:

- Sidebar
- Topbar
- Breadcrumbs
- Content area
- Responsive menu
- Dashboard page
- Generic error page
- Access denied page
- Loading state
- Empty state

Layout rules:

- The sidebar must collapse/adapt on mobile and tablet.
- The topbar must remain usable on small screens.
- Breadcrumbs must not break the layout on small screens.
- Main content must not overflow horizontally.
- Layout must work in portrait and landscape orientation.
- Navigation must be self-explanatory.

---

## 17. Initial features

Create real functionality for the following areas.

### Authentication

- Login
- Logout
- Access denied page
- Administrator seed
- Basic role management

### Clientes

Create a complete CRUD for `Cliente`:

- List clients
- Create client
- Edit client
- View client details
- Delete client using soft delete
- Confirmation dialog before delete
- Server-side search
- Server-side pagination
- Server-side sorting
- Server-side filters
- Form validation
- Success and error messages
- Audit changes

The `Clientes` list must use a custom table component with:

- Configurable columns
- Pagination
- Search
- Sorting
- Loading state
- Empty state
- Action buttons
- No dependency on external libraries
- Responsive behavior on mobile, tablet, laptop and desktop

---

## 18. Validation

Server-side validation is mandatory.

Use DataAnnotations by default.

Optionally prepare the architecture for FluentValidation in the future, but do not make it mandatory unless necessary.

Rules:

- Validate DTOs and ViewModels.
- Validate in application services.
- Do not trust browser validation only.
- Show validation errors in the UI.
- Prevent saving invalid models.
- Validate email fields.
- Validate maximum string lengths.
- Validate required fields.
- Create unique indexes where the business rule requires uniqueness, such as `Email` or `NIF`.
- Validation messages must be clear, localized and useful.
- Validation messages must help the user fix the problem without needing external explanation.

---

## 19. Performance

Rules:

- Use server-side pagination.
- Use server-side filtering.
- Use server-side sorting.
- Do not load complete tables into memory.
- Use `Select` projections to DTOs.
- Use `AsNoTracking()` for read-only queries.
- Avoid N+1 queries.
- Use indexes.
- Use `CancellationToken` in async operations.
- Use `async` and `await` correctly.
- Never use `.Result`.
- Never use `.Wait()`.
- Avoid heavy logic inside Razor components.
- Use `@key` in lists where appropriate.
- Avoid unnecessary renders.
- Split large components into smaller components.
- Use caching only where it makes sense and does not compromise security.
- Keep pages performant on mobile devices.
- Avoid excessive client-side payloads.
- Avoid unnecessary JavaScript.

---

## 20. Code quality

Follow:

- Clean Code
- SOLID where it makes sense
- DRY, but without premature abstractions
- KISS
- Clear names
- Small methods
- Focused classes
- Simple Razor components
- Separation between UI and business logic
- Separation between Application and Infrastructure
- Strong typing
- Nullable reference types enabled
- Treat warnings seriously

Rules:

- Do not generate dead code.
- Do not generate pseudocode.
- Do not leave important TODOs unresolved.
- Do not add comments unless they add real value.
- Code must compile.

Use simple patterns:

- `Result<T>` for service responses
- `PagedResult<T>` for lists
- `QueryParameters` for pagination and filters
- Application services for use cases
- DTOs for input and output
- Entities only in Domain/Infrastructure

---

## 21. Definition of done

No feature should be considered complete unless it includes tests.

A feature is only complete when:

- The code compiles.
- Unit tests pass.
- Integration tests pass where applicable.
- Blazor component tests pass where applicable.
- E2E tests pass for critical flows where applicable.
- Responsive behavior was considered and tested where applicable.
- Localization behavior was considered and tested where applicable.
- Security/authorization behavior is tested where applicable.
- Validation rules are tested.
- Error cases are tested.
- The happy path and relevant failure paths are tested.
- Existing tests remain green.
- No critical warnings are introduced.
- The feature is understandable without additional explanation.
- The UI makes the next action clear to the user.

---

## 22. Tests

Automated tests are mandatory.

The application must include tests to guarantee that all implemented functionalities are working correctly and continue working after future changes.

Testing must be part of the normal development workflow, not an optional task.

### Test execution scope

Do not run the full test suite automatically after every small user-requested change.

Use this rule:

- For documentation-only changes, do not run application tests.
- For small CSS, layout, copy or visual-only changes, prefer a focused browser/manual verification of the affected page instead of running all tests.
- For narrow code changes, run only the relevant targeted test class or test method using `dotnet test --filter ...`.
- For new business logic, security-sensitive flows, billing, authentication, authorization, database changes, migrations, shared services or broad refactors, run the relevant targeted tests and then the full test suite when appropriate.
- Run the full test suite only when the change has broad impact, when multiple unrelated areas were touched, before considering a larger feature complete, or when the user explicitly asks for it.
- If tests are skipped or only targeted tests are run, state that clearly in the final response with the reason.

Testing libraries are allowed. The restriction on external UI component libraries applies only to the application UI.

Use test libraries appropriate for .NET and Blazor, such as:

- xUnit, NUnit or MSTest
- FluentAssertions or equivalent assertion helpers
- bUnit for Blazor component tests
- WebApplicationFactory for integration tests
- Playwright for end-to-end browser tests, when useful
- EF Core SQLite in-memory mode or SQL Server test database for persistence tests

### Minimum required test types

Create tests for:

- Domain rules
- Entity behavior
- `Cliente` validation
- Application services
- Query handlers/use cases
- Command handlers/use cases
- Pagination
- Filtering
- Sorting
- Soft delete
- Audit behavior
- Authorization and permission checks
- Authentication flows where practical
- Identity role checks where practical
- EF Core mappings where useful
- Database migrations where practical
- Initial seed of roles and admin user
- Automatic migration execution behavior where practical
- Blazor UI components
- Form validation
- Error handling
- Localization and browser culture behavior
- Responsive behavior for critical pages
- Security-sensitive operations

### Feature coverage rule

For every feature, create tests for:

- Successful scenario
- Invalid input
- Unauthorized access
- Forbidden access when the user is authenticated but lacks permission
- Not found scenarios
- Duplicate data scenarios where applicable
- Validation errors
- Persistence errors where practical
- Audit log creation where applicable
- Soft delete behavior where applicable
- Concurrency conflicts where applicable
- Localization behavior where applicable
- Responsive behavior where applicable

### Clientes feature tests

The `Clientes` module must include tests for:

- Listing clients
- Creating a valid client
- Rejecting invalid client data
- Editing a client
- Viewing client details
- Soft deleting a client
- Preventing access to deleted clients where appropriate
- Searching clients server-side
- Filtering clients server-side
- Sorting clients server-side
- Paginating clients server-side
- Returning `PagedResult<T>` correctly
- Preventing unauthorized create/update/delete operations
- Creating audit logs for create/update/delete
- Handling duplicate `Email` or `NIF` if the business rules require uniqueness
- Handling concurrency conflicts using `RowVersion`

### Blazor component tests

Use bUnit or equivalent for important Blazor components.

Test at least:

- `AppButton`
- `AppTextBox`
- `AppTextArea`
- `AppSelect`
- `AppDatePicker`
- `AppNumberInput`
- `AppCheckbox`
- `AppTable`
- `AppPagination`
- `AppModal`
- `AppCard`
- `AppPageHeader`
- `AppSearchBox`
- `AppConfirmDialog`
- `AppLoading`
- `AppEmptyState`
- `AppAlert`
- `AppValidationSummary`
- `AppBreadcrumbs`

Component tests should verify:

- Correct rendering
- Parameters
- Disabled/loading states
- Event callbacks
- Validation display
- Accessibility attributes where applicable
- Empty/loading/error states
- Responsive class behavior where applicable

### Integration tests

Create integration tests for important application flows.

Test at least:

- Application starts with a valid configuration.
- Database migrations can be applied.
- Roles are seeded.
- Admin user is seeded from configuration/secrets.
- Authentication-protected pages reject anonymous users.
- Authorized users can access allowed pages.
- Unauthorized users cannot execute restricted operations.
- CRUD operations persist data correctly.
- Soft delete does not physically remove records unless explicitly intended.
- Audit logs are created for critical operations.
- Localization middleware selects culture based on browser language where possible.

### End-to-end tests

Use Playwright or an equivalent tool when browser-level validation is useful.

Create E2E tests for the most important flows:

- Login
- Logout
- Open dashboard
- Open clients list
- Create client
- Edit client
- Search client
- Delete client with confirmation
- Access denied scenario
- Browser language/culture behavior where practical
- Mobile viewport behavior
- Tablet viewport behavior
- Desktop viewport behavior

Do not overuse E2E tests for everything. Prefer unit and integration tests for most business logic.

### Security tests

Create tests for security-sensitive behavior:

- Anonymous users cannot access protected pages.
- Normal users cannot access admin-only features.
- Users without permission cannot create, update or delete protected records.
- Server-side authorization is enforced even if the UI hides buttons.
- Invalid input is rejected on the server.
- Login lockout behavior works if configured.
- Sensitive values are not returned in DTOs.
- Audit logs do not store passwords, tokens or unnecessary sensitive data.

### Localization tests

Create tests or examples for localization:

- Default culture is `pt-PT`.
- Browser language is detected from `Accept-Language`.
- Supported browser culture is applied automatically.
- Unsupported browser culture falls back to `pt-PT`.
- Dates and numbers are formatted according to the current culture.
- Validation messages can be localized.
- UI labels come from resource files and are not hardcoded in Razor components.

### Responsive tests

Create tests or manual test checklists for responsive behavior:

- Mobile viewport
- Tablet viewport
- Laptop viewport
- Desktop viewport
- Portrait orientation
- Landscape orientation
- Touch-friendly controls
- Collapsible sidebar
- Usable tables
- Usable forms
- Usable modals
- No horizontal overflow on main pages

### UX tests and review

For each user-facing feature, verify:

- The page is understandable without external explanation.
- The main action is clear.
- The user knows what to do next.
- Error messages are actionable.
- Empty states guide the user.
- Success messages confirm the result.
- Destructive actions are clear and confirmed.
- Navigation is understandable.
- The feature feels natural on all supported device sizes.

### Test quality rules

Tests must be:

- Deterministic
- Repeatable
- Isolated
- Fast where possible
- Clear and readable
- Named according to the behavior being tested

Use a naming style similar to:

```text
MethodName_WhenCondition_ShouldExpectedResult
```

or:

```text
GivenCondition_WhenAction_ThenExpectedResult
```

---

## 23. Code generation rules

Before generating code, confirm the assumed decisions:

- Blazor Web App
- Interactive Server
- .NET 10 LTS
- SQL Server `.\SQLEXPRESS`
- Database name equals application name plus `DB`
- EF Core with automatic migrations
- ASP.NET Core Identity
- Native Blazor components only
- No Syncfusion
- No MudBlazor
- No external UI component library
- No multi-tenant
- No TenantId
- Light Clean Architecture
- Mandatory server-side security
- Automatic language based on browser culture
- Responsive UI for mobile, tablet, laptop and desktop
- Automated tests for all implemented features
- UX must be self-explanatory: the application must speak for itself
- Accessibility is mandatory
- Privacy/GDPR principles must be respected

When generating code:

- Always show the file path before the code block.
- Code must be real and compilable.
- Do not generate weak demo code.
- Do not access `DbContext` directly from Razor components.
- Do not return EF entities directly to the UI.
- Use DTOs and ViewModels.
- Include required using statements.
- Include dependency injection registrations where needed.
- Include tests for new functionality.
- Include localization resources for visible UI text.
- Include responsive CSS where UI is created or modified.

Example:

```text
File: src/[NOME_DA_APP].Domain/Entities/Cliente.cs
```

---

## 24. Expected delivery order for large tasks

When creating or expanding the application, deliver in this order:

1. Architecture explanation
2. Project and folder structure
3. `dotnet` commands to create the solution
4. Required NuGet packages
5. `appsettings.Development.json`
6. `Program.cs`
7. `ApplicationDbContext`
8. Identity configuration
9. Automatic migrations configuration
10. Initial roles and admin seed
11. Domain entities
12. Fluent API configurations
13. DTOs and ViewModels
14. Application services
15. Validations
16. Custom UI components
17. Localization configuration and resource files
18. Responsive layout and responsive CSS
19. Accessibility implementation and review
20. Privacy/GDPR data protection review
21. Main layout
22. Blazor pages
23. Complete `Clientes` CRUD
24. Custom table with server-side pagination, search and sorting
25. Global error handling
26. Logging
27. Audit
28. Unit tests
29. Component tests
30. Integration tests
31. E2E tests for critical flows where useful
32. Responsive test checklist
33. UX/self-explanatory review checklist
34. Execution instructions
35. Migration commands
36. How to configure the initial admin with User Secrets

---
## 25. Final objective

The application must be:

- Secure
- Maintainable
- Testable
- Responsive
- Localized automatically by browser language
- Accessible
- Privacy-aware and GDPR-conscious
- Fast enough for real business usage
- Easy to understand
- Easy to use
- Professional
- Self-explanatory

The application must speak for itself.

A user should not need an explanation to understand the main flow of each screen.
