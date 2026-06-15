# DevHelper — Implementation Plan

This plan turns `spec.md` into an ordered, buildable sequence of work. It is written so a human programmer or an AI coding agent can execute one step at a time, verify it, and move on. Each step lists **what to build**, **files touched**, and **done-when** checks.

Authoritative sources: `spec.md` (behavior + data model) and `CLAUDE.md` (rules and constraints). Where they disagree, `CLAUDE.md` wins. **Entity Framework is prohibited — use Dapper everywhere.**

---

## 0. Conventions for the implementer

- Target framework: `net10.0` for every project.
- Work in small commits, one logical step per commit. Run `dotnet build` and `dotnet test` before each commit.
- Never write SQL outside a repository class. All SQL is parameterized.
- All timestamps are stored as `TEXT` in ISO-8601 (`DateTime.UtcNow.ToString("O")`). Dates in Markdown render as `yyyy-MM-dd`; filename dates as `yyyyMMdd`.
- Model naming: `SystemEntry`, `ServerEntry`, `DatabaseEntry` (never `System`).
- Scope discipline: build **Phase 1 only** (Release Plan Generator) unless explicitly asked for later phases. Keep Test Plan models/schema in mind only where they shape reusable infrastructure (join-table pattern, `PlanScreenshots`, `IMarkdownGenerationService` taking a plan type).

---

## Phase 1 — Release Plan Generator (end to end)

### Step 1.1 — Solution and project scaffold

**Build**
- Create the solution and two projects:
  ```bash
  dotnet new blazor -n DevHelper.Web -f net10.0 -int Server
  dotnet new xunit  -n DevHelper.Tests -f net10.0
  dotnet new sln    -n DevHelper
  dotnet sln add DevHelper.Web DevHelper.Tests
  dotnet add DevHelper.Tests reference DevHelper.Web
  ```
- Add NuGet packages to `DevHelper.Web`:
  - `Dapper`
  - `Microsoft.Data.Sqlite`
  - `Microsoft.Data.SqlClient`
  - `Serilog.AspNetCore`, `Serilog.Sinks.Console`, `Serilog.Sinks.File`
  - `Markdig`
  - `PuppeteerSharp`
  - Data Protection ships in the framework (`Microsoft.AspNetCore.DataProtection`); reference if not transitive.
- Add test packages to `DevHelper.Tests`: `Microsoft.Data.Sqlite`, `Dapper`, `FluentAssertions` (optional), `Microsoft.NET.Test.Sdk`, `xunit`, `xunit.runner.visualstudio`.
- Create the directory skeleton in `DevHelper.Web`: `Components/Pages`, `Components/Shared`, `Components/Layout`, `Services`, `Repositories`, `Models`, `Data`, `Database/Migrations`, `Database/Scripts`.

**Done-when**
- `dotnet build` succeeds.
- `dotnet test` runs (zero tests is fine).
- No EF package appears in either `.csproj`.

---

### Step 1.2 — Configuration, logging, DI bootstrap

**Build**
- In `appsettings.json` add: SQLite connection string / DB file path (e.g. `Data/devhelper.db`), Serilog config, and a default screenshot directory key.
- Configure Serilog in `Program.cs` with Console + rolling File sinks (`Logs/devhelper-.log`, daily rolling). Log app startup/shutdown.
- Register Data Protection: `services.AddDataProtection()`.
- Add a strongly-typed options class (e.g. `DevHelperOptions`) bound from config for the SQLite path.
- Wire DI placeholders (interfaces registered as they are created in later steps).

**Done-when**
- App starts, writes a startup log line to console and a log file.
- `Logs/` is git-ignored (verify `.gitignore`).

---

### Step 1.3 — SQLite connection factory + migration runner

**Build**
- `Models/` nothing yet. In `Data/`:
  - `ISqliteConnectionFactory` + `SqliteConnectionFactory` returning an open `Microsoft.Data.Sqlite.SqliteConnection`. Enable `PRAGMA foreign_keys = ON` on every connection.
  - `IDatabaseInitializer` + `DatabaseInitializer`:
    1. Ensure DB file/directory exists.
    2. Create `SchemaMigrations` table if missing.
    3. Enumerate embedded/`Database/Migrations/*.sql` ordered by filename prefix.
    4. Apply each migration not present in `SchemaMigrations`, inside a transaction, then record it (`MigrationName`, `AppliedAt`).
    5. Log each applied migration.
- Decide migration file delivery: embed `.sql` files as embedded resources **or** copy to output (`<CopyToOutputDirectory>`). Pick one and be consistent; embedded resources are more robust for tests.
- Call `IDatabaseInitializer.InitializeAsync()` on startup before the app serves requests.

**Done-when**
- Starting the app creates `devhelper.db` and the `SchemaMigrations` table.
- Running twice applies nothing the second time (idempotent).

---

### Step 1.4 — Schema migration: `001_InitialSchema.sql`

**Build** — one migration creating every table in spec §9 (create all now even though Test Plan workflow comes later; schema is shared infra):
- `SchemaMigrations` (created by the runner, but safe to `IF NOT EXISTS` here too).
- `SystemEntries`, `ServerEntries`, `DatabaseEntries`.
- `Templates`.
- `ReleasePlans`, `ReleasePlanTickets`, `SqlScripts`.
- `ReleasePlanSystems`, `ReleasePlanServers`, `ReleasePlanDatabases`.
- `TestPlans`, `TestPlanTickets`, `TestCases`.
- `TestPlanSystems`, `TestPlanServers`, `TestPlanDatabases`.
- `PlanScreenshots`.
- `ApplicationSettings`.

Apply the spec's index/constraint rules exactly:
- Join tables: `ON DELETE CASCADE` to owning plan, unique index on `(PlanId, EntityId)`, index on `PlanId`.
- System join tables: **unique partial index** on `(PlanId, SystemEntryId) WHERE SystemEntryId IS NOT NULL`; either `SystemEntryId` or `OtherSystemName` required (enforce required-one in the service layer; partial index covers dup prevention).
- `PlanScreenshots`: index on `(PlanType, PlanId, SortOrder)`.
- `ApplicationSettings.SettingKey` UNIQUE.

**Done-when**
- Migration applies cleanly; a schema-dump test confirms all tables/indexes exist.

---

### Step 1.5 — Schema migration: `002_SeedData.sql`

**Build**
- Seed default `SystemEntries` (placeholder set — note in the file that the user edits this before first run, per `CLAUDE.md`).
- Seed the **default Release Plan template** whose `MarkdownTemplate` reproduces spec §6.5, using the §6.4 placeholders (`{{Title}}`, `{{ReleaseDate}}`, `{{Environment}}`, `{{CreatedBy}}`, `{{Tickets}}`, `{{Systems}}`, `{{Servers}}`, `{{Databases}}`, `{{SqlScripts}}`, `{{BackupSteps}}`, `{{DeploymentSteps}}`, `{{ValidationSteps}}`, `{{RollbackSteps}}`, `{{Screenshots}}`, `{{Notes}}`). `ToolType = 'ReleasePlan'`, `IsDefault = 1`.
- (Optional, harmless) seed a default `TestPlan` template from §7.6 for later — or defer to Phase 3.

**Done-when**
- After init, querying `Templates` returns one default ReleasePlan template; `SystemEntries` has seed rows.

---

### Step 1.6 — Domain models

**Build** POCOs in `Models/` mirroring spec §9 columns (PascalCase properties matching column names so Dapper maps directly):
- `SystemEntry`, `ServerEntry`, `DatabaseEntry`
- `Template` (with `ToolType` enum or string), `ToolType`/`Environment`/`ServerType` enums (store as TEXT)
- `ReleasePlan`, `ReleasePlanTicket`, `SqlScript`
- `ReleasePlanSystemLink`, `ReleasePlanServerLink`, `ReleasePlanDatabaseLink`
- `PlanScreenshot`
- `ApplicationSetting`
- A composite `ReleasePlanAggregate` (plan + tickets + scripts + system/server/db links + screenshots) used by the service/save path.
- Test Plan models may be stubbed now or deferred to Phase 3 — schema already exists.

**Done-when** project compiles; enums chosen for `Environment` (Development/QA/UAT/Production/Other), `ServerType` (Web/App/Database/File/Service/Other).

---

### Step 1.7 — Repositories (Dapper)

**Build** interfaces + implementations in `Repositories/`. One `using var connection = _factory.CreateConnection();` per method. Parameterized SQL only. Log DB failures.
- `ISystemRepository`, `IServerRepository`, `IDatabaseRepository` — CRUD + list active.
- `ITemplateRepository` — CRUD, duplicate, set-default (clears other defaults for that `ToolType` in a transaction), list by `ToolType`.
- `IReleasePlanRepository` — list/search/get-by-id (hydrate aggregate), insert/update/delete. **Save the plan + all child rows (tickets, SQL scripts, system/server/database links, screenshots) in one transaction**; on update, replace child rows. `delete` relies on cascade for children.
- `ISqlScriptRepository`, `IScreenshotRepository` — may be folded into the release plan repo's transactional save, or standalone; keep the transactional save authoritative.
- `IApplicationSettingRepository` — get/set by key (upsert on `SettingKey`), respecting `IsEncrypted`.

**Done-when** repository tests in Step 1.13 pass.

---

### Step 1.8 — Application settings service + Data Protection

**Build**
- `IApplicationSettingsService` over the repository.
- On write of a **sensitive key** (`TicketLookupSqlServerConnectionString`), encrypt with Data Protection purpose `DevHelper.ApplicationSettings.v1`, store with `IsEncrypted = 1`. Decrypt only on read through the service.
- Typed accessors for `ScreenshotSourceDirectory`, `DefaultEnvironment`, `LogLevel`, `TicketLookupSqlServerConnectionString`.
- Never log decrypted sensitive values.

**Done-when** settings tests confirm round-trip encryption and that the stored `SettingValue` is not plaintext.

---

### Step 1.9 — Markdown generation service (the core contract)

**Build** `IMarkdownGenerationService` (shared; accepts plan type). Implement the §5.5 rendering contract exactly:
- Exact, case-sensitive `{{PlaceholderName}}` replacement.
- Unknown placeholders left unchanged + **warning log**.
- Missing optional scalars → empty string. Empty collections → `None`.
- Dates → `yyyy-MM-dd`.
- Single-line values in tables/lists escaped for `|` and line breaks; long-form fields preserve user Markdown.
- Collections render in `SortOrder` then `Id` order.
- Release plan renderers (§6.4):
  - `{{Tickets}}` → `## [TicketNumber] [TicketName]` + summary when present.
  - `{{Systems}}` → bullet list of system names / `OtherSystemName`.
  - `{{Servers}}` → `- [Name] - [Environment] - [Type]`.
  - `{{Databases}}` → `- [Name] on [SqlServerInstance]`.
  - `{{SqlScripts}}` → Markdown table: `Order | Database | Script Name | Required | Description`.
  - Step fields → split on newlines, non-empty lines as a numbered list.
  - `{{Screenshots}}` → `![Description](path)` per attachment; missing file → clear missing-file marker + warning, never throw.
- Persist generated Markdown to `ReleasePlans.MarkdownOutput` on save.

**Done-when** golden-output tests (Step 1.13) pass against the §6.5 default template.

---

### Step 1.10 — Release plan service (validation + orchestration)

**Build** `IReleasePlanService`:
- Validation (§6.6) before save: Release Date, Environment, Created By, ≥1 ticket (TicketNumber required), ≥1 selected system, selected template. Return structured validation errors.
- Orchestrate transactional save via the repository, generate + store Markdown, return the aggregate.
- List/search/get/delete pass-throughs with logging of save/update/delete.

**Done-when** validation tests reject each missing required field; save round-trips the full aggregate.

---

### Step 1.11 — Screenshot service (path safety)

**Build** `IScreenshotService` honoring §5.6:
- List selectable images from the configured `ScreenshotSourceDirectory` (extensions `.png/.jpg/.jpeg/.webp`).
- Validate/normalize a chosen path with .NET path APIs; reject directories, unsupported extensions, missing files, and **any path resolving outside** the source directory.
- Prefer storing **relative** paths to the source dir.
- Provide a safe resolver for preview/PDF that only serves files inside the source dir; missing file → missing-file state + warning, no hard failure.
- Removing an attachment deletes only the DB row, never the file.

**Done-when** screenshot service tests (later phase coverage, but implement now since Phase 1 plans have screenshots) confirm extension allow-list, outside-dir rejection, relative storage, missing-file handling, and no file deletion.

---

### Step 1.12 — External SQL Server connection service (store + test only)

**Build** `IExternalSqlServerConnectionService`:
- Read the encrypted connection string from settings.
- "Test connection" opens a `Microsoft.Data.SqlClient.SqlConnection` and returns success/failure.
- Log attempt + result (may include host + exception type/message; **never** credentials or full connection string).
- Absent/unreachable must not block the app — manual ticket entry remains available. No ticket-lookup query in v1 (deferred).

**Done-when** tests cover missing connection string and unreachable server without throwing into the UI flow.

---

### Step 1.13 — Phase 1 automated tests (`DevHelper.Tests`)

**Build** (xUnit, temp SQLite DB per test/fixture):
- **Migrations**: initializer creates DB, applies migrations in filename order, records them, idempotent on re-run.
- **Repositories**: CRUD, child-collection persistence, cascade delete, and **transaction rollback** for release plan save (force a mid-save failure → no partial rows).
- **Validation**: each missing required field rejected (date, environment, created-by, ticket, system, template).
- **Markdown golden tests**: placeholder replacement, date formatting, collection ordering, empty→`None`, table escaping, unknown-placeholder warning.
- **Settings**: sensitive value encrypted at rest, decrypted only via service.
- **External SQL**: missing connection string + unreachable server handled gracefully.
- **EF guard**: assert no `Microsoft.EntityFrameworkCore` reference/namespace anywhere (scan project assets + source).

**Done-when** `dotnet test` is green.

---

### Step 1.14 — Blazor UI: layout, navigation, shared components

**Build** (Interactive Server):
- `Components/Layout` nav to: Home, Release Plans (Create/Edit), Templates, Systems, Servers, Databases, Settings.
- Shared components per spec §15: `TicketListEditor`, `TicketEditor`, `SystemSelector`, `ServerSelector`, `DatabaseSelector`, `TemplateSelector`, `SqlScriptEditor`, `ScreenshotPicker`, `MarkdownPreview`, `ExportActions`.
- Keep `TestCaseEditor` for Phase 3.

**Done-when** app renders with working navigation; shared components compile and bind.

---

### Step 1.15 — Blazor pages: shared infrastructure management

**Build** CRUD pages:
- `Systems.razor`, `Servers.razor`, `Databases.razor` — add/edit/deactivate.
- `Templates.razor` — view/create/edit/duplicate/deactivate/set-default, scoped/filtered by tool type.
- `Settings.razor` — External SQL Server (string + Test button), Screenshot Source Directory, Default Environment, Logging Level.

**Done-when** each page performs its CRUD against the services and persists.

---

### Step 1.16 — Blazor pages: Create/Edit Release Plan + Dashboard

**Build**
- `Home.razor` dashboard: recent plans, search by title, filter by environment/type, open/edit, delete.
- `CreateReleasePlan.razor` with sections per §8.2: Release Info, Template Selection, Tickets, Systems, Servers, Databases, SQL Scripts, Backup/Deployment/Validation/Rollback Steps, Screenshots, Notes, **Markdown Preview**, Export Actions (Save, Copy Markdown, Download `.md`; PDF wired in Phase 2).
- Markdown `.md` download + copy-to-clipboard; filename `ReleasePlan_[Environment]_[yyyyMMdd]_[Tickets].md`.

**Done-when** a user can create, edit, preview, save, copy, and download a release plan as `.md`; validation errors surface in the UI.

---

### Step 1.17 — Phase 1 hardening

- Verify Serilog logs all §14 events (save/update/delete, markdown generation success/failure, settings, SQL test, unhandled exceptions via middleware).
- Re-run full `dotnet build` + `dotnet test`. Manual smoke test of the full release-plan flow.

**Phase 1 acceptance** = spec §16 "Release Plan Generator" + "Shared Infrastructure" + "Settings" + "Logging" bullets, minus PDF (Phase 2).

---

## Phase 2 — Release Plan PDF Export

Do after Phase 1 Markdown is stable.

- `IPdfGenerationService` (shared): Markdown → HTML (Markdig) → PDF (PuppeteerSharp). PuppeteerSharp is the **only** PDF provider — no QuestPDF/DinkToPdf.
- Embed screenshots via the safe resolver (§5.6); missing files render a clear placeholder, never fail the whole PDF.
- Filename `ReleasePlan_[Environment]_[yyyyMMdd]_[Tickets].pdf`; persist `PdfFilePath` if stored.
- Wire "Download PDF" into `ExportActions`.
- Handle Chromium provisioning for PuppeteerSharp (download on first run or document a build step).
- **Test**: PDF smoke test produces a non-empty PDF from known Markdown/HTML.

---

## Phase 3 — Test Plan Generator

Only after Phases 1–2 are stable. Schema already exists from Step 1.4.

- Models: `TestPlan`, `TestPlanTicket`, `TestCase`, test-plan join links, plus `TestPlanAggregate`.
- Repositories/service: `ITestPlanRepository`, `ITestPlanService`, `ITestCaseRepository` — mirror release-plan transactional save + validation (§7.7: Test Date, Environment, Tested By, ≥1 ticket, ≥1 test case with name, template).
- Markdown: extend `IMarkdownGenerationService` for test-plan placeholders (§7.5) and `{{TestCases}}` rendering (§7.6) — status, description, pre-conditions, numbered steps, expected/actual results, notes.
- UI: `CreateTestPlan.razor` (§8.3) + `TestCaseEditor.razor`; reuse all shared components and the screenshot picker.
- PDF/Markdown export reusing Phase 1–2 infrastructure; filenames `TestPlan_[Environment]_[yyyyMMdd]_[Tickets].{md,pdf}`.
- Optional `RelatedReleasePlanId` link.
- **Tests** mirror release-plan coverage (validation, transactional save, Markdown golden, screenshot behavior).

---

## Dependency order (quick reference)

```
1.1 scaffold
 └─1.2 config/logging/DI
    └─1.3 connection factory + migration runner
       └─1.4 schema migration ──┬─1.5 seed data
                                 └─1.6 models
                                    ├─1.7 repositories ──┬─1.8 settings+DataProtection
                                    │                    ├─1.9 markdown service
                                    │                    ├─1.10 release plan service
                                    │                    ├─1.11 screenshot service
                                    │                    └─1.12 external SQL service
                                    └─1.13 tests (track each service)
                                       └─1.14 UI layout+shared components
                                          ├─1.15 infra pages
                                          └─1.16 release plan + dashboard
                                             └─1.17 hardening → Phase 2 → Phase 3
```

## Cross-cutting checklist (apply continuously)

- [ ] No Entity Framework anywhere (package or namespace).
- [ ] All SQL parameterized, inside repositories only.
- [ ] Transactions wrap every plan-with-children save.
- [ ] Sensitive settings encrypted (`DevHelper.ApplicationSettings.v1`), never logged decrypted.
- [ ] Screenshot files never uploaded/copied/moved/renamed/deleted; paths confined to the source dir.
- [ ] Markdown rendering matches the §5.5 contract (case-sensitive tokens, `None`, date formats, escaping, ordering, unknown-placeholder warnings).
- [ ] Serilog covers all §14 events; `dotnet build` + `dotnet test` green before each commit.
```
