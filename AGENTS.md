# Codex Instructions - TrueCompare

These instructions apply whenever code, tests, documentation or architecture are changed in this repository.

Keep this file short. Detailed product and validation rules live in:

- `docs/TRUECOMPARE_BLUEPRINT.md`
- `docs/TESTING_AND_QUALITY.md`

Read those documents before changing product behavior, search/ranking logic, stores, prices, alerts, LLM behavior, localization, authentication, billing or data persistence.

## Product goal

TrueCompare lets a user search for any product by text or image, compares valid market options, verifies stores/prices/product URLs and lets the user buy now or create a price alert.

The application must not invent products, stores, prices, sellers, stock, warranties or URLs.

## Mandatory stack

Use:

- .NET 10
- Blazor Web App
- Interactive Server render mode
- SQL Server
- Entity Framework Core
- ASP.NET Core Identity
- Native Razor components, HTML and CSS
- Automatic localization based on browser language
- Automated tests for implemented features

Do not use external Blazor UI libraries such as Syncfusion, MudBlazor, Radzen, Telerik, DevExpress, Blazorise or AntDesign.

Do not introduce multi-tenant architecture, `TenantId`, tenant isolation or database-per-tenant design.

## Architecture rules

- Keep a light clean architecture with clear separation between UI, application logic, domain and infrastructure.
- Razor components must call application services.
- Do not put business logic in Razor components.
- Do not access `DbContext` directly from Razor components.
- Do not expose EF Core entities directly to the UI.
- Use DTOs/ViewModels for UI and service boundaries.
- Do not expose `IQueryable` outside infrastructure/application internals.
- Prefer existing project patterns over new abstractions.
- Create interfaces only when they help testing, replacement or decoupling.
- Keep the repository clean: do not leave generated artifacts, local logs, screenshots, publish outputs, build outputs, temporary datasets or unused files in the project.
- Add new files only when they are necessary for runtime, tests, configuration or documentation.
- If a task creates temporary files, remove them before finishing.

## Database rules

- Use SQL Server.
- Development database: `TrueCompareDB`.
- Development connection string:

```text
Server=.\\SQLExpress;Database=TrueCompareDB;Trusted_Connection=True;TrustServerCertificate=True;MultipleActiveResultSets=true
```

- Use EF Core migrations.
- Use `Database.MigrateAsync()` on startup when automatic migrations are enabled.
- Never use `EnsureCreated()`.
- Never mix `EnsureCreated()` with migrations.
- Seed roles and initial admin from configuration/secrets, never hardcoded passwords.
- Use UTC for stored dates where appropriate.

## Security rules

- Never commit real secrets, passwords, tokens or production connection strings.
- Use User Secrets, environment variables or a secure secret provider.
- Use ASP.NET Core Identity for authentication.
- Use role/policy authorization for protected operations.
- UI checks are not enough; critical application services must validate permissions.
- Avoid `MarkupString` for user content.
- Do not log passwords, tokens or unnecessary personal data.
- Use structured logging for relevant security and business events.

## Product data quality rules

Follow `docs/TRUECOMPARE_BLUEPRINT.md`.

Core rules:

- A suggested product must match the requested category.
- Explicit user constraints, especially maximum price, must be respected.
- A recommended store must have a direct product URL and confirmed price.
- Search/category/homepage URLs are not valid product URLs.
- Marketplace platform and real seller must be separated.
- Results must be market-aware: country, language, currency, taxes, warranty and local stores.
- Portugal and USA are examples only; the architecture must support developed markets through configuration and connectors.
- If data is incomplete, show it as incomplete instead of presenting it as validated.

## LLM rules

- Prefer local Ollama first when available.
- Use cloud/fallback providers only when local is unavailable or unsuitable.
- LLMs may interpret intent, extract constraints, summarize and compare.
- LLMs must not be treated as the final source for price, stock, seller, product URL, warranty or offer existence.
- Validate factual claims against structured data, connectors or product pages before showing them as true.

## UI and UX rules

- The UI must be self-explanatory.
- Important actions need clear labels.
- Loading, empty, success and error states must be clear.
- The app must work on mobile, tablet, laptop and desktop.
- Avoid horizontal overflow.
- Use accessible semantic HTML.
- Inputs must have labels or accessible names.
- Keyboard navigation must work for important flows.
- Do not add long in-app explanations when better labels or flow design can solve the issue.

## Localization and markets

- Use browser language by default.
- Fallback culture is `pt-PT` unless explicitly changed.
- Prepare at least `pt-PT` and `en-US`.
- UI text should be localizable, not hardcoded in Razor components when it is stable product text.
- Format currency, dates and numbers according to the active culture/market.

## Testing rules

Follow `docs/TESTING_AND_QUALITY.md`.

Do not run the full test suite automatically after every small change.

Use this scope:

- Documentation-only changes: do not run application tests.
- Small CSS/layout/copy changes: prefer focused browser/manual verification.
- Narrow code changes: run targeted tests with `dotnet test --filter ...`.
- Business logic, security, billing, authentication, authorization, database, migrations, shared services or broad refactors: run relevant tests and then the full suite when appropriate.
- If tests are skipped or only targeted tests are run, say so in the final response.

Important features need tests for:

- Happy path.
- Invalid input.
- Unauthorized/forbidden access.
- Not found.
- Duplicate/conflict cases where applicable.
- Validation errors.
- Persistence errors where practical.
- Localization behavior where applicable.
- Responsive behavior where applicable.

## Documentation rules

Keep documentation focused:

- `README.md`: how to run and configure the project.
- `AGENTS.md`: short rules for Codex/AI agents.
- `.github/copilot-instructions.md`: short pointer for GitHub Copilot.
- `docs/TRUECOMPARE_BLUEPRINT.md`: product, domain and architecture blueprint.
- `docs/TESTING_AND_QUALITY.md`: validation and test plan.

When product behavior changes, update the blueprint.

When functionality or validation expectations change, update the testing document.

Whenever `docs/TRUECOMPARE_BLUEPRINT.md` changes, review `docs/TESTING_AND_QUALITY.md` in the same task and update it when the blueprint change affects behavior, architecture, data quality, supported platforms, integrations, validation rules or release criteria. If no testing update is needed, state the reason in the final response.

## Definition of done

A change is complete only when:

- Code compiles when code was changed.
- Relevant tests or focused verification were run, or skipped with a clear reason.
- Security and privacy implications were considered.
- UI remains responsive and accessible for affected flows.
- Documentation was updated when behavior changed.
- No unrelated user changes were reverted.
