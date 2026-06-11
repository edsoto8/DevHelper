# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project

**Release Plan Generator** — a .NET Blazor web application for generating structured software release plans. Full spec is in `spec.md`.

---

## Tech Stack

| Layer | Technology |
|---|---|
| Frontend | Blazor / Razor Components |
| Runtime | .NET 10 |
| Backend | ASP.NET Core, C# |
| App database | SQLite via Dapper + `Microsoft.Data.Sqlite` |
| External ticket lookup | SQL Server via `Microsoft.Data.SqlClient` (future, optional) |
| Logging | Serilog (Console + rolling File sinks) |
| PDF generation | Markdown → HTML → PDF (QuestPDF, DinkToPdf, or PuppeteerSharp) |

**Entity Framework is explicitly prohibited.** Use Dapper for all data access.

---

## Commands

Once the project is scaffolded:

```bash
# Run the web app
dotnet run --project ReleasePlanGenerator.Web

# Run tests
dotnet test

# Run a single test
dotnet test --filter "FullyQualifiedName~<TestName>"

# Build
dotnet build

# Restore packages
dotnet restore
```

---

## Project Structure

```
ReleasePlanGenerator/
  ReleasePlanGenerator.Web/
    Components/        # Reusable Blazor components
    Pages/             # Route-level Razor pages
    Layout/
    Services/          # Business logic services
    Repositories/      # Dapper-based data access
    Models/            # Domain/data models
    Data/
    Database/
      Migrations/      # SQL migration scripts
      Scripts/         # Schema creation SQL
    wwwroot/
    appsettings.json
    Program.cs
  ReleasePlanGenerator.Tests/
```

For a larger solution, domain logic can be split into separate `Application`, `Infrastructure`, and `Domain` projects.

---

## Architecture

### Data Access Pattern

All database access goes through repository interfaces injected via DI. Repositories use Dapper internally. Never write raw SQL outside a repository.

```csharp
public interface ISqliteConnectionFactory
{
    IDbConnection CreateConnection();
}
```

Use `using var connection = _connectionFactory.CreateConnection();` per repository method. Use transactions when saving a release plan with its related tickets, scripts, systems, servers, or databases.

### Database Migrations

On startup, `IDatabaseInitializer` checks/creates the SQLite database and runs pending migrations. Migration state is tracked in a `SchemaMigrations` table. Migration files live in `Database/Migrations/`.

### Model Naming

Use `SystemEntry` (not `System`) to avoid collision with the .NET `System` namespace. Similarly `ServerEntry` and `DatabaseEntry`.

### Service Layer

Services handle business logic; repositories handle data access only. Key service interfaces: `IReleasePlanService`, `IMarkdownGenerationService`, `IPdfGenerationService`, `IApplicationSettingsService`, `IExternalSqlServerConnectionService`.

### PDF Generation

Generate Markdown first → convert to HTML → convert to PDF. Filename format: `ReleasePlan_[Environment]_[ReleaseDate]_[TicketNumbers].pdf`.

### Settings Storage

Application settings (including the optional SQL Server connection string) are stored in an `ApplicationSettings` SQLite table with a `SettingKey` / `SettingValue` / `IsEncrypted` structure. Sensitive values must not be stored as plain text.

---

## Key Rules

- Target `net10.0` for all projects. Use `dotnet` commands to scaffold, then edit/add files as needed.
- All SQL must be parameterized — no string concatenation with user input.
- Log all database errors, release plan operations, PDF/Markdown generation failures, and SQL Server connection test results via Serilog.
- The SQL Server connection string is optional. If absent or unreachable, fall back to manual ticket entry gracefully without blocking the user.
- Ticket lookup against SQL Server is deferred to a future version; the v1 scaffolding only stores the connection string.
- Release plan save requires at least: a release date, an environment, one ticket, one system, and a selected template.
- Use transactions when persisting a release plan alongside its child records (tickets, scripts, etc.).

---

## Default Systems (seed data)

Configured in `Database/Migrations/002_SeedData.sql`. Edit that file to match your environment before first run.
