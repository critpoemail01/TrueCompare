# GitHub Copilot Instructions - TrueCompare

Use `AGENTS.md` as the main source of project instructions.

Before changing product behavior, search/ranking logic, stores, prices, alerts, LLM behavior, localization, authentication, billing or data persistence, also read:

- `docs/TRUECOMPARE_BLUEPRINT.md`
- `docs/TESTING_AND_QUALITY.md`

Keep changes aligned with the repository stack:

- .NET 10
- Blazor Web App
- Interactive Server render mode
- SQL Server
- Entity Framework Core
- ASP.NET Core Identity
- Native Razor components, HTML and CSS

Do not introduce external Blazor UI component libraries or multi-tenant architecture.
