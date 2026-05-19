# Copilot Instructions

This repository contains a professional enterprise application built with C#, .NET, Blazor Web App and SQL Server.

These instructions must be followed whenever code is generated, modified or refactored in this project.

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

## 2. Database

Use SQL Server instance:

```text
Server=.\SQLEXPRESS
```

Development connection string format:

```text
Server=.\\SQLEXPRESS;Database=[NOME_DA_APP]DB;Trusted_Connection=True;TrustServerCertificate=True;MultipleActiveResultSets=true;
```

Rules:

- The database name must always be the application name followed by `DB`.
- Example: if the application is called `Workvert`, the database must be called `WorkvertDB`.
- Do not ask for a separate database name unless explicitly requested.
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

Production rule:

- Automatic migrations must be possible to enable or disable through configuration.

---

## 3. Architecture

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
  [NOME_DA_APP].Tests/
```

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

### [NOME_DA_APP].Tests

Responsibilities:

- Unit tests
- Application tests
- Domain rule tests
- Basic integration tests

---

## 4. Architecture rules

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

## 5. Security

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

## 6. Secrets

Rules:

- Never commit real secrets.
- Never place passwords in code.
- Never place tokens in code.
- Never place production connection strings in the repository.
- Use User Secrets in development.
- Use environment variables or a secure secret provider in production.
- `appsettings.json` must not contain real production secrets.

---

## 7. Logging and error handling

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

## 8. Audit

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

## 9. Entity Framework Core

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

## 10. Blazor UI

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

---

## 11. Layout

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

---

## 12. Initial features

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

---

## 13. Validation

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

---

## 14. Performance

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

---

## 15. Code quality

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

## 16. Tests

Create tests for:

- Domain rules
- `Cliente` validation
- Application services
- Pagination
- Soft delete
- Authorization of critical operations, when possible
- Audit behavior, when possible

Full coverage is not required at the beginning, but the structure must be ready to grow.

---

## 17. Code generation rules

Before generating code, confirm the assumed decisions:

- Blazor Web App
- Interactive Server
- .NET 10 LTS
- SQL Server `.\SQLEXPRESS`
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

When generating code:

- Always show the file path before the code block.
- Code must be real and compilable.
- Do not generate weak demo code.
- Do not access `DbContext` directly from Razor components.
- Do not return EF entities directly to the UI.
- Use DTOs and ViewModels.
- Include required using statements.
- Include dependency injection registrations where needed.

Example:

```text
File: src/[NOME_DA_APP].Domain/Entities/Cliente.cs
```

---

## 18. Expected delivery order for large tasks

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
17. Main layout
18. Blazor pages
19. Complete `Clientes` CRUD
20. Custom table with server-side pagination, search and sorting
21. Global error handling
22. Logging
23. Audit
24. Basic tests
25. Execution instructions
26. Migration commands
27. How to configure the initial admin with User Secrets

---

## 19. Final objective

The goal is not to create a weak demo application.

The goal is to create a professional, secure, maintainable and scalable foundation for a real business application.
