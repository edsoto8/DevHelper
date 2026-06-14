# AGENTS.md

This file provides guidance to AI coding agents (e.g., OpenAI Codex) when working with code in this repository. It mirrors the intent of `CLAUDE.md`.

## Project

**DevHelper** — a .NET Blazor web application that provides developer productivity tools. The app is being rebuilt from scratch. The full spec covering both tools is in `spec.md`.

---

## Roadmap

| Tool | Status |
|---|---|
| Release Plan Generator | In progress |
| Test Plan Generator | Planned |

Current implementation priority is Phase 1: deliver the Release Plan Generator end to end with shared infrastructure. Do not build the Test Plan Generator workflow until explicitly requested; keep its shared models and schema in mind where they affect reusable infrastructure.

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
| PDF generation | Markdown → HTML with Markdig → PDF with PuppeteerSharp |

**Entity Framework is explicitly prohibited.** Use Dapper for all data access.

---

## Commands

```bash
# Run the web app
dotnet run --project DevHelper.Web

# Scaffold the web app if it does not exist yet
dotnet new blazor -n DevHelper.Web -f net10.0 -int Server

# Run tests
dotnet test

# Run a single test
dotnet test --filter "FullyQualifiedName~<TestName>"

# Build
dotnet build

# Restore packages
dotnet restore
```

## Testing Expectations

Use `DevHelper.Tests` for automated tests, preferably xUnit unless another framework already exists. Phase 1 changes should include focused tests for migration idempotency, Dapper repository CRUD and transaction rollback, Release Plan validation, Markdown generation golden output, encrypted settings storage, optional SQL Server connection failure handling, and a guard that Entity Framework packages/namespaces are absent.

---

## Project Structure

```
DevHelper/
  DevHelper.Web/
    Components/
      Pages/           # Route-level Razor pages
      Shared/          # Reusable Blazor components
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
  DevHelper.Tests/
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

Join tables must be explicit database tables with foreign keys, cascade delete from the owning plan, uniqueness constraints to prevent duplicate links, and indexes on the owning plan id.

### Database Migrations

On startup, `IDatabaseInitializer` checks/creates the SQLite database and runs pending migrations. Migration state is tracked in a `SchemaMigrations` table. Migration files live in `Database/Migrations/`.

### Model Naming

Use `SystemEntry` (not `System`) to avoid collision with the .NET `System` namespace. Similarly `ServerEntry` and `DatabaseEntry`.

### Service Layer

Services handle business logic; repositories handle data access only. Key service interfaces: `IReleasePlanService`, `IMarkdownGenerationService`, `IPdfGenerationService`, `IApplicationSettingsService`, `IExternalSqlServerConnectionService`.

Markdown generation must follow the template rendering contract in `spec.md`: exact case-sensitive `{{PlaceholderName}}` replacement, stable date formatting, ordered collection rendering, Markdown-safe table/list values, and warning logs for unknown placeholders.

### PDF Generation

Generate Markdown first → convert to HTML with Markdig → convert to PDF with PuppeteerSharp. Filename format: `ReleasePlan_[Environment]_[ReleaseDate]_[TicketNumbers].pdf`. Do not add QuestPDF, DinkToPdf, or another PDF provider unless explicitly requested.

### Settings Storage

Application settings are stored in an `ApplicationSettings` SQLite table with a `SettingKey` / `SettingValue` / `IsEncrypted` structure. Use ASP.NET Core Data Protection with purpose `DevHelper.ApplicationSettings.v1` for sensitive values. `TicketLookupSqlServerConnectionString` must be encrypted at rest, must have `IsEncrypted = 1`, and must never be written to logs in decrypted form.

### File Uploads

Screenshot uploads must validate extension and size, generate server-side file names, store only paths relative to the configured screenshot output directory, reject path traversal, and log failed cleanup without failing the database delete.

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
