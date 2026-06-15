# DevHelper — Application Specification

## 1. Overview

**DevHelper** is a .NET 10 Blazor web application that provides a suite of developer productivity tools. The application is designed to be modular — each tool shares a common infrastructure (systems, servers, databases, settings, templates) but operates independently.

### Tools

| Tool | Description | Status |
|---|---|---|
| Release Plan Generator | Create, manage, and export structured software release plans with optional screenshot references | Current build phase |
| Test Plan Generator | Create, manage, and export structured test plans with test cases and optional screenshot references | Planned after Release Plan |

---

## 2. Goals

The application should allow users to:

1. Create and manage release plans and test plans.
2. Reuse shared infrastructure — systems, servers, databases, and templates — across both tools.
3. Store all application data in SQLite using Dapper.
4. Configure an optional external SQL Server connection string for future ticket lookup.
5. Generate clean Markdown output from both tools.
6. Export Markdown and PDF from both tools.
7. Attach and display screenshots in release plans and test plans by referencing files from a configured screenshot folder.
8. Configure a screenshot source folder that works with Windows, macOS, and Linux path formats.
9. Log all application events and errors using Serilog.

---

## 2.1 Implementation Phases

Build the application in focused phases. Do not implement later-phase user workflows until the current phase is complete unless explicitly requested.

### Phase 1 — Release Plan Generator

Deliver the Release Plan Generator end to end:

* Blazor app scaffold and shared layout/navigation
* SQLite database initialization and migrations
* Shared systems, servers, databases, templates, and settings infrastructure
* Release plan create, edit, list, delete, validation, and transactional save
* Markdown generation, preview, copy, and `.md` download for release plans
* Optional SQL Server connection string storage and connection test
* Automated tests covering migrations, repositories, validation, and Markdown generation

### Phase 2 — Release Plan PDF Export

Add PDF export for release plans after Markdown generation is stable.

### Phase 3 — Test Plan Generator

Implement the Test Plan Generator after the Release Plan Generator and shared infrastructure are stable. Section 7 defines the target behavior for this later phase.

---

## 3. Technology Stack

### 3.1 Frontend

* Blazor (Interactive Server rendering)
* Razor Components
* C#
* Bootstrap or similar component styling

### 3.2 Backend / Application Layer

* .NET 10
* ASP.NET Core
* C#
* Service-based architecture

### 3.3 Data Access

* Dapper
* Microsoft.Data.Sqlite
* Microsoft.Data.SqlClient (future ticket lookup)

**Entity Framework is explicitly prohibited.** Use Dapper for all data access.

### 3.4 Application Database

SQLite stores all application data:

* Release plans and their child records
* Test plans and their child records
* Templates (release plan and test plan templates stored separately)
* Systems, servers, databases
* Screenshots (stored as file path references; binary blobs are not stored)
* Application settings

### 3.5 External Ticket Lookup

SQL Server is an optional future data source for ticket lookup. v1 only stores the connection string.

### 3.6 Logging

* Serilog with Console and rolling File sinks

### 3.7 PDF Generation

* Markdig for Markdown to HTML conversion
* PuppeteerSharp for HTML to PDF rendering

### 3.8 Sensitive Settings Protection

* ASP.NET Core Data Protection for encrypting sensitive application settings

---

## 4. Primary Users

**Release Coordinator / Developer** — prepares deployment instructions, SQL scripts, and release documentation.

**QA Engineer / Developer** — creates structured test plans, records test results, and attaches screenshots.

---

## 5. Shared Infrastructure

Both tools share the following managed resources. These are maintained independently and selected when building a release plan or test plan.

### 5.1 Systems

A maintainable list of known systems stored in SQLite.

Fields:

* System Name
* Description
* Active / Inactive

Users can add, edit, and deactivate systems. A free-form "Other" entry is always available.

### 5.2 Servers

Fields:

* Server Name
* Environment (Development / QA / UAT / Production / Other)
* Related System
* Server Type (Web / App / Database / File / Service / Other)
* Notes
* Active / Inactive

### 5.3 Databases

Fields:

* Database Name
* SQL Server Instance
* Environment
* Related System
* Notes
* Active / Inactive

### 5.4 Templates

Templates are stored in SQLite and are scoped to a tool type (`ReleasePlan` or `TestPlan`).

Fields:

* Template Name
* Description
* Tool Type
* Markdown Template (with `{{placeholders}}`)
* Is Default
* Active / Inactive

Users can create, edit, duplicate, deactivate, and set a default template per tool type.

### 5.5 Template Rendering Contract

Templates use exact `{{PlaceholderName}}` tokens. Rendering is case-sensitive.

Rules:

* All supported placeholders for the selected tool type must be replaced during Markdown generation.
* Unknown placeholders must remain unchanged and generate a warning log entry.
* Missing optional scalar values render as an empty string.
* Empty collections render as `None`.
* Dates render as `yyyy-MM-dd`.
* Filename dates render as `yyyyMMdd`.
* Single-line values used in Markdown tables or list items must be escaped for Markdown table separators and line breaks.
* Long-form fields such as summaries, steps, notes, expected results, and actual results may preserve user-entered Markdown.
* Collection placeholders render in `SortOrder`, then `Id`, order.

### 5.6 Screenshot Source Folder and Attachments

Screenshots are selected from a configured **Screenshot Source Directory**. The application does not upload, copy, rename, move, capture, or delete screenshot image files.

Settings:

* **Screenshot Source Directory** — a folder path accessible to the DevHelper process.
* The path may use Windows, macOS, or Linux path formats. Use .NET path APIs; do not hard-code path separators.
* If DevHelper is hosted on a server, this directory is on the server or a mounted share visible to the server process, not the browser client's private local filesystem.

Plan screenshot fields:

* Description
* File Path (stored in SQLite as a reference to the selected file)
* Attached At (timestamp)
* Sort Order

User workflow:

1. While editing a release plan or test plan, the user chooses **Add Screenshot**.
2. The app shows selectable image files from the configured screenshot source directory.
3. The user selects one image file.
4. The app shows a preview of the selected image before the attachment is saved.
5. The user can enter or edit a description.
6. Saving the plan stores only the screenshot metadata and file path reference.

Rules:

* Supported extensions: `.png`, `.jpg`, `.jpeg`, `.webp`.
* Only files inside the configured screenshot source directory can be selected.
* Resolve and normalize paths before saving; reject missing files, directories, unsupported extensions, and paths outside the configured source directory.
* Prefer storing paths relative to the configured screenshot source directory. If an absolute path is stored, it must still resolve inside the configured source directory.
* Removing a screenshot attachment removes only the database reference. It must not delete the image file.
* Preview and PDF rendering must not serve arbitrary filesystem locations; they may only resolve screenshot files through the configured source directory.
* If a referenced screenshot file is missing during preview, Markdown generation, or PDF generation, show a clear missing-file state and log a warning without failing the entire plan.

---

## 6. Tool 1 — Release Plan Generator

### 6.1 Release Plan Fields

* Title
* Release Date
* Environment (Development / QA / UAT / Production / Other)
* Created By
* Selected Template
* Tickets (one or more)
* Affected Systems (selected from shared list)
* Servers (selected from shared list)
* Databases (selected from shared list)
* SQL Scripts
* Backup Steps
* Deployment Steps
* Validation Steps
* Rollback Steps
* Screenshots (one or more — see section 5.6)
* Notes

### 6.2 Ticket Fields

* Ticket Number (required)
* Ticket Name (required)
* Summary / Description
* Sort Order

Multiple tickets per release plan. Users can add, remove, and reorder tickets.

### 6.3 SQL Script Fields

* Script Name
* Database (linked to shared DatabaseEntry)
* Script Description
* Execution Order
* Is Required
* Notes

### 6.4 Markdown Generation

The release plan is generated from the selected template with the following placeholders:

| Placeholder | Value |
|---|---|
| `{{Title}}` | Release plan title |
| `{{ReleaseDate}}` | Formatted release date |
| `{{Environment}}` | Selected environment |
| `{{CreatedBy}}` | Created by name |
| `{{Tickets}}` | All tickets formatted as Markdown |
| `{{Systems}}` | Selected systems as a bullet list |
| `{{Servers}}` | Selected servers as a bullet list |
| `{{Databases}}` | Selected databases as a bullet list |
| `{{SqlScripts}}` | SQL scripts as a Markdown table |
| `{{BackupSteps}}` | Numbered backup steps |
| `{{DeploymentSteps}}` | Numbered deployment steps |
| `{{ValidationSteps}}` | Numbered validation steps |
| `{{RollbackSteps}}` | Numbered rollback steps |
| `{{Screenshots}}` | Selected screenshots rendered as Markdown image references with descriptions |
| `{{Notes}}` | Additional notes |

Release plan collection rendering:

* `{{Tickets}}`: each ticket renders as `## [TicketNumber] [TicketName]`, followed by its summary when present.
* `{{Systems}}`: bullet list of selected system names and free-form other system names.
* `{{Servers}}`: bullet list formatted as `[Server Name] - [Environment] - [Server Type]`.
* `{{Databases}}`: bullet list formatted as `[Database Name] on [SQL Server Instance]`.
* `{{SqlScripts}}`: Markdown table with columns `Order`, `Database`, `Script Name`, `Required`, and `Description`.
* `{{Screenshots}}`: each screenshot renders as `![Description](path/to/screenshot.png)` using the selected file path reference.
* Step placeholders split stored text on new lines and render non-empty lines as a numbered list.

### 6.5 Default Release Plan Markdown Format

```markdown
# Release Plan

## Release Information

**Release Date:** [Release Date]
**Environment:** [Environment]
**Created By:** [Created By]

---

# Tickets

# [Ticket Number] [Ticket Name]

## Summary

[Ticket Summary]

---

# Affected Systems

- [System Name]

---

# Servers

- [Server Name] - [Environment] - [Server Type]

---

# Databases

- [Database Name] on [SQL Server Instance]

---

# Back Up Scripts

## Backup Requirements

- Confirm current database backup exists.
- Back up affected SQL scripts before deployment.

## Backup Steps

1. [Backup step 1]

---

# Deployment Steps

1. [Deployment step 1]

---

# Execute SQL Scripts

## SQL Script List

| Order | Database | Script Name | Required |
|---|---|---|---|
| 1 | [Database] | [Script Name] | Yes |

---

# Validation

## Validation Steps

1. [Validation step 1]

---

# Rollback Plan

## Rollback Steps

1. [Rollback step 1]

---

# Screenshots

![Description](path/to/screenshot.png)

---

# Notes

[Notes]
```

### 6.6 Validation Rules

Required to save:

* Release Date
* Environment
* Created By
* At least one ticket (Ticket Number required)
* At least one selected system
* Selected template

### 6.7 Save Behavior

Saving a release plan saves the plan and all child records (tickets, SQL scripts, screenshots, system/server/database links) in a single transaction.

---

## 7. Tool 2 — Test Plan Generator

### 7.1 Test Plan Fields

* Title
* Test Date
* Environment (Development / QA / UAT / Production / Other)
* Tested By
* Related Release Plan (optional link)
* Selected Template
* Tickets (one or more — reused from the same ticket concept)
* Affected Systems (selected from shared list)
* Servers (selected from shared list)
* Databases (selected from shared list)
* Test Cases (one or more)
* Screenshots (one or more — see section 5.6)
* Notes
* Status (Draft / In Progress / Completed / Failed)

### 7.2 Ticket Fields

Same as release plan tickets:

* Ticket Number (required)
* Ticket Name (required)
* Summary / Description
* Sort Order

### 7.3 Test Case Fields

* Test Case ID / Number
* Test Case Name
* Description
* Pre-conditions
* Steps (one per line)
* Expected Result
* Actual Result
* Status (Not Run / Pass / Fail / Blocked / Skipped)
* Notes
* Sort Order

Users can add, remove, and reorder test cases.

### 7.4 Test Plan Screenshots

Test plans use the shared screenshot source folder and attachment behavior from section 5.6.

Screenshots are plan-level attachments. Test case records do not upload, copy, or own screenshot files.

Screenshots are included in the generated PDF and referenced by file path in the generated Markdown.

### 7.5 Markdown Generation

The test plan is generated from the selected template with the following placeholders:

| Placeholder | Value |
|---|---|
| `{{Title}}` | Test plan title |
| `{{TestDate}}` | Formatted test date |
| `{{Environment}}` | Selected environment |
| `{{TestedBy}}` | Tested by name |
| `{{Status}}` | Overall test plan status |
| `{{RelatedReleasePlan}}` | Related release plan title or empty string |
| `{{Tickets}}` | Tickets formatted as Markdown |
| `{{Systems}}` | Affected systems as a bullet list |
| `{{Servers}}` | Servers as a bullet list |
| `{{Databases}}` | Databases as a bullet list |
| `{{TestCases}}` | All test cases formatted as Markdown |
| `{{Screenshots}}` | Selected screenshots rendered as Markdown image references with descriptions |
| `{{Notes}}` | Additional notes |

Test plan collection rendering:

* `{{Tickets}}`: each ticket renders as `## [TicketNumber] [TicketName]`, followed by its summary when present.
* `{{Systems}}`: bullet list of selected system names and free-form other system names.
* `{{Servers}}`: bullet list formatted as `[Server Name] - [Environment] - [Server Type]`.
* `{{Databases}}`: bullet list formatted as `[Database Name] on [SQL Server Instance]`.
* `{{TestCases}}`: each test case renders with status, description, pre-conditions, numbered steps, expected result, actual result, and notes when present.
* `{{Screenshots}}`: each screenshot renders as `![Description](path/to/screenshot.png)` using the selected file path reference.
* Test case steps split stored text on new lines and render non-empty lines as a numbered list.

### 7.6 Default Test Plan Markdown Format

```markdown
# Test Plan

## Test Information

**Test Date:** [Test Date]
**Environment:** [Environment]
**Tested By:** [Tested By]
**Status:** [Status]

---

# Tickets

# [Ticket Number] [Ticket Name]

## Summary

[Ticket Summary]

---

# Affected Systems

- [System Name]

---

# Servers

- [Server Name] - [Environment] - [Server Type]

---

# Databases

- [Database Name] on [SQL Server Instance]

---

# Test Cases

## [Test Case ID] [Test Case Name]

**Status:** [Status]

### Description

[Description]

### Pre-conditions

[Pre-conditions]

### Steps

1. [Step 1]
2. [Step 2]

### Expected Result

[Expected Result]

### Actual Result

[Actual Result]

---

# Screenshots

![Description](path/to/screenshot.png)

---

# Notes

[Notes]
```

### 7.7 Validation Rules

Required to save:

* Test Date
* Environment
* Tested By
* At least one ticket (Ticket Number required)
* At least one test case (Test Case Name required)
* Selected template

### 7.8 Save Behavior

Saving a test plan saves the plan and all child records (tickets, test cases, screenshots, system/server/database links) in a single transaction.

---

## 8. Screens / Pages

### 8.1 Dashboard (Home)

Displays a combined view of recent release plans and test plans, or separate tabs for each.

Features:

* Create new release plan
* Create new test plan
* Search plans by title
* Filter by environment
* Filter by type (release plan / test plan)
* Open / edit existing plans
* Delete plans

### 8.2 Create / Edit Release Plan

Sections:

1. Release Information
2. Template Selection
3. Tickets
4. Affected Systems
5. Servers
6. Databases
7. SQL Scripts
8. Deployment Steps
9. Validation Steps
10. Rollback Steps
11. Screenshots
12. Notes
13. Markdown Preview
14. Export Actions (Save, Copy Markdown, Download .md, Download PDF)

### 8.3 Create / Edit Test Plan

Sections:

1. Test Plan Information
2. Template Selection
3. Tickets
4. Affected Systems
5. Servers
6. Databases
7. Test Cases
8. Screenshots
9. Notes
10. Markdown Preview
11. Export Actions (Save, Copy Markdown, Download .md, Download PDF)

### 8.4 Templates Page

Scoped view per tool type (Release Plan / Test Plan tab or filter).

Features: view, create, edit, duplicate, deactivate, set default.

### 8.5 Systems Page

Add, edit, deactivate systems.

### 8.6 Servers Page

Add, edit, deactivate servers.

### 8.7 Databases Page

Add, edit, deactivate databases.

### 8.8 Settings Page

Settings sections:

* **External SQL Server** — optional connection string for future ticket lookup; test connection button.
* **Screenshot Source Directory** — local or server path where existing screenshot images are selected from.
* **Default Environment** — pre-selected environment on new plan forms.
* **Logging Level** — adjustable log level.

---

## 9. SQLite Data Model

### 9.1 SchemaMigrations

| Column | Type |
|---|---|
| Id | INTEGER PK |
| MigrationName | TEXT NOT NULL |
| AppliedAt | TEXT NOT NULL |

### 9.2 SystemEntries

| Column | Type |
|---|---|
| Id | INTEGER PK |
| Name | TEXT NOT NULL |
| Description | TEXT |
| IsActive | INTEGER DEFAULT 1 |
| CreatedAt | TEXT NOT NULL |
| UpdatedAt | TEXT NOT NULL |

### 9.3 ServerEntries

| Column | Type |
|---|---|
| Id | INTEGER PK |
| Name | TEXT NOT NULL |
| Environment | TEXT NOT NULL |
| SystemEntryId | INTEGER FK |
| ServerType | TEXT NOT NULL |
| Notes | TEXT |
| IsActive | INTEGER DEFAULT 1 |
| CreatedAt | TEXT NOT NULL |
| UpdatedAt | TEXT NOT NULL |

### 9.4 DatabaseEntries

| Column | Type |
|---|---|
| Id | INTEGER PK |
| Name | TEXT NOT NULL |
| SqlServerInstance | TEXT NOT NULL |
| Environment | TEXT NOT NULL |
| SystemEntryId | INTEGER FK |
| Notes | TEXT |
| IsActive | INTEGER DEFAULT 1 |
| CreatedAt | TEXT NOT NULL |
| UpdatedAt | TEXT NOT NULL |

### 9.5 Templates

| Column | Type |
|---|---|
| Id | INTEGER PK |
| ToolType | TEXT NOT NULL (`ReleasePlan` or `TestPlan`) |
| Name | TEXT NOT NULL |
| Description | TEXT |
| MarkdownTemplate | TEXT NOT NULL |
| IsDefault | INTEGER DEFAULT 0 |
| IsActive | INTEGER DEFAULT 1 |
| CreatedAt | TEXT NOT NULL |
| UpdatedAt | TEXT NOT NULL |

### 9.6 ReleasePlans

| Column | Type |
|---|---|
| Id | INTEGER PK |
| Title | TEXT NOT NULL |
| ReleaseDate | TEXT NOT NULL |
| Environment | TEXT NOT NULL |
| CreatedBy | TEXT NOT NULL |
| TemplateId | INTEGER FK |
| BackupSteps | TEXT |
| DeploymentSteps | TEXT |
| ValidationSteps | TEXT |
| RollbackSteps | TEXT |
| Notes | TEXT |
| MarkdownOutput | TEXT |
| PdfFilePath | TEXT |
| CreatedAt | TEXT NOT NULL |
| UpdatedAt | TEXT NOT NULL |

### 9.7 ReleasePlanTickets

| Column | Type |
|---|---|
| Id | INTEGER PK |
| ReleasePlanId | INTEGER FK (CASCADE DELETE) |
| TicketNumber | TEXT NOT NULL |
| TicketName | TEXT NOT NULL |
| TicketSummary | TEXT |
| SortOrder | INTEGER DEFAULT 0 |
| CreatedAt | TEXT NOT NULL |
| UpdatedAt | TEXT NOT NULL |

### 9.8 SqlScripts

| Column | Type |
|---|---|
| Id | INTEGER PK |
| ReleasePlanId | INTEGER FK (CASCADE DELETE) |
| DatabaseEntryId | INTEGER FK |
| ScriptName | TEXT NOT NULL |
| ScriptDescription | TEXT |
| ExecutionOrder | INTEGER DEFAULT 1 |
| IsRequired | INTEGER DEFAULT 1 |
| Notes | TEXT |

### 9.9 ReleasePlanSystems / ReleasePlanServers / ReleasePlanDatabases

Join tables linking a release plan to its selected systems, servers, and databases.

#### ReleasePlanSystems

| Column | Type |
|---|---|
| Id | INTEGER PK |
| ReleasePlanId | INTEGER NOT NULL FK to `ReleasePlans.Id` ON DELETE CASCADE |
| SystemEntryId | INTEGER FK to `SystemEntries.Id` |
| OtherSystemName | TEXT |
| SortOrder | INTEGER DEFAULT 0 |

Rules:

* Either `SystemEntryId` or `OtherSystemName` is required.
* `OtherSystemName` is used only for the free-form "Other" system option.
* Add a unique partial index on `(ReleasePlanId, SystemEntryId)` where `SystemEntryId IS NOT NULL`.
* Add an index on `ReleasePlanId`.

#### ReleasePlanServers

| Column | Type |
|---|---|
| Id | INTEGER PK |
| ReleasePlanId | INTEGER NOT NULL FK to `ReleasePlans.Id` ON DELETE CASCADE |
| ServerEntryId | INTEGER NOT NULL FK to `ServerEntries.Id` |
| SortOrder | INTEGER DEFAULT 0 |

Rules:

* Add a unique index on `(ReleasePlanId, ServerEntryId)`.
* Add an index on `ReleasePlanId`.

#### ReleasePlanDatabases

| Column | Type |
|---|---|
| Id | INTEGER PK |
| ReleasePlanId | INTEGER NOT NULL FK to `ReleasePlans.Id` ON DELETE CASCADE |
| DatabaseEntryId | INTEGER NOT NULL FK to `DatabaseEntries.Id` |
| SortOrder | INTEGER DEFAULT 0 |

Rules:

* Add a unique index on `(ReleasePlanId, DatabaseEntryId)`.
* Add an index on `ReleasePlanId`.

### 9.10 TestPlans

| Column | Type |
|---|---|
| Id | INTEGER PK |
| Title | TEXT NOT NULL |
| TestDate | TEXT NOT NULL |
| Environment | TEXT NOT NULL |
| TestedBy | TEXT NOT NULL |
| RelatedReleasePlanId | INTEGER FK (nullable) |
| TemplateId | INTEGER FK |
| Status | TEXT DEFAULT 'Draft' |
| Notes | TEXT |
| MarkdownOutput | TEXT |
| PdfFilePath | TEXT |
| CreatedAt | TEXT NOT NULL |
| UpdatedAt | TEXT NOT NULL |

### 9.11 TestPlanTickets

| Column | Type |
|---|---|
| Id | INTEGER PK |
| TestPlanId | INTEGER FK (CASCADE DELETE) |
| TicketNumber | TEXT NOT NULL |
| TicketName | TEXT NOT NULL |
| TicketSummary | TEXT |
| SortOrder | INTEGER DEFAULT 0 |
| CreatedAt | TEXT NOT NULL |
| UpdatedAt | TEXT NOT NULL |

### 9.12 TestCases

| Column | Type |
|---|---|
| Id | INTEGER PK |
| TestPlanId | INTEGER FK (CASCADE DELETE) |
| TestCaseNumber | TEXT |
| Name | TEXT NOT NULL |
| Description | TEXT |
| PreConditions | TEXT |
| Steps | TEXT |
| ExpectedResult | TEXT |
| ActualResult | TEXT |
| Status | TEXT DEFAULT 'Not Run' |
| Notes | TEXT |
| SortOrder | INTEGER DEFAULT 0 |
| CreatedAt | TEXT NOT NULL |
| UpdatedAt | TEXT NOT NULL |

### 9.13 PlanScreenshots

| Column | Type |
|---|---|
| Id | INTEGER PK |
| PlanType | TEXT NOT NULL (`ReleasePlan` or `TestPlan`) |
| PlanId | INTEGER NOT NULL |
| Description | TEXT |
| FilePath | TEXT NOT NULL |
| AttachedAt | TEXT NOT NULL |
| SortOrder | INTEGER DEFAULT 0 |
| CreatedAt | TEXT NOT NULL |
| UpdatedAt | TEXT NOT NULL |

Rules:

* `PlanType` and `PlanId` identify the owning release plan or test plan.
* Save and delete operations must maintain screenshot rows in the same transaction as the owning plan.
* `FilePath` must pass the configured screenshot source directory validation in section 5.6.
* Add an index on `(PlanType, PlanId, SortOrder)`.

### 9.14 TestPlanSystems / TestPlanServers / TestPlanDatabases

Join tables linking a test plan to its selected systems, servers, and databases.

#### TestPlanSystems

| Column | Type |
|---|---|
| Id | INTEGER PK |
| TestPlanId | INTEGER NOT NULL FK to `TestPlans.Id` ON DELETE CASCADE |
| SystemEntryId | INTEGER FK to `SystemEntries.Id` |
| OtherSystemName | TEXT |
| SortOrder | INTEGER DEFAULT 0 |

Rules:

* Either `SystemEntryId` or `OtherSystemName` is required.
* `OtherSystemName` is used only for the free-form "Other" system option.
* Add a unique partial index on `(TestPlanId, SystemEntryId)` where `SystemEntryId IS NOT NULL`.
* Add an index on `TestPlanId`.

#### TestPlanServers

| Column | Type |
|---|---|
| Id | INTEGER PK |
| TestPlanId | INTEGER NOT NULL FK to `TestPlans.Id` ON DELETE CASCADE |
| ServerEntryId | INTEGER NOT NULL FK to `ServerEntries.Id` |
| SortOrder | INTEGER DEFAULT 0 |

Rules:

* Add a unique index on `(TestPlanId, ServerEntryId)`.
* Add an index on `TestPlanId`.

#### TestPlanDatabases

| Column | Type |
|---|---|
| Id | INTEGER PK |
| TestPlanId | INTEGER NOT NULL FK to `TestPlans.Id` ON DELETE CASCADE |
| DatabaseEntryId | INTEGER NOT NULL FK to `DatabaseEntries.Id` |
| SortOrder | INTEGER DEFAULT 0 |

Rules:

* Add a unique index on `(TestPlanId, DatabaseEntryId)`.
* Add an index on `TestPlanId`.

### 9.15 ApplicationSettings

| Column | Type |
|---|---|
| Id | INTEGER PK |
| SettingKey | TEXT NOT NULL UNIQUE |
| SettingValue | TEXT |
| IsEncrypted | INTEGER DEFAULT 0 |
| CreatedAt | TEXT NOT NULL |
| UpdatedAt | TEXT NOT NULL |

Example setting keys: `TicketLookupSqlServerConnectionString`, `ScreenshotSourceDirectory`, `DefaultEnvironment`, `LogLevel`.

Sensitive settings must be encrypted before they are persisted. Use ASP.NET Core Data Protection with a stable purpose string of `DevHelper.ApplicationSettings.v1`. Store encrypted values in `SettingValue` with `IsEncrypted = 1`; non-sensitive values use `IsEncrypted = 0`.

Sensitive setting keys:

* `TicketLookupSqlServerConnectionString`

Never log decrypted sensitive values. Connection test logs may include success/failure, SQL Server host name if available, and exception type/message, but must not include credentials or the full connection string.

---

## 10. Data Access

* Use Dapper for all SQLite reads and writes.
* All SQL must be parameterized — no string concatenation with user input.
* Use the `ISqliteConnectionFactory` pattern (one connection per repository method, opened with `using var`).
* Use transactions when saving a plan with child records.
* Log all database failures with Serilog.

### Suggested Repository Interfaces

```
IReleasePlanRepository
ITestPlanRepository
ITestCaseRepository
IScreenshotRepository
ITemplateRepository
ISystemRepository
IServerRepository
IDatabaseRepository
ISqlScriptRepository
IApplicationSettingRepository
```

### Suggested Service Interfaces

```
IReleasePlanService
ITestPlanService
IMarkdownGenerationService        (shared; accepts a plan type)
IPdfGenerationService             (shared)
IApplicationSettingsService
IScreenshotService
IExternalSqlServerConnectionService
IDatabaseInitializer
```

---

## 11. Database Migrations

On startup `IDatabaseInitializer`:

1. Creates the SQLite database if it does not exist.
2. Creates the `SchemaMigrations` table if it does not exist.
3. Scans `Database/Migrations/` for `*.sql` files ordered by filename prefix.
4. Runs any migration not yet recorded in `SchemaMigrations`.
5. Logs each applied migration with Serilog.

---

## 12. PDF Generation

1. Generate Markdown.
2. Convert Markdown to HTML (Markdig).
3. Convert HTML to PDF with PuppeteerSharp.
4. Embed screenshots where referenced.
5. Download as `.pdf`.

Use PuppeteerSharp as the only PDF provider unless explicitly requested otherwise. Do not add QuestPDF, DinkToPdf, or another PDF provider for the v1 implementation.

Filename format:

```
ReleasePlan_[Environment]_[Date]_[Tickets].pdf
TestPlan_[Environment]_[Date]_[Tickets].pdf
```

---

## 13. Markdown Export

* Copy Markdown to clipboard.
* Download as `.md`.
* Save generated Markdown to SQLite as part of the plan record.

Filename format:

```
ReleasePlan_[Environment]_[Date]_[Tickets].md
TestPlan_[Environment]_[Date]_[Tickets].md
```

---

## 14. Logging

Log with Serilog at appropriate levels:

* Application startup / shutdown
* SQLite initialization and migration execution
* Release plan and test plan save / update / delete
* Template save / update / delete
* Markdown and PDF generation success and failure
* Screenshot source directory validation and attachment add/remove
* SQL Server connection test attempts and results
* Any unhandled exceptions

---

## 15. Project Structure

Use the .NET Blazor Web App template with Interactive Server rendering:

```bash
dotnet new blazor -n DevHelper.Web -f net10.0 -int Server
```

```
DevHelper/
  DevHelper.Web/
    Components/
      Pages/
        Home.razor
        CreateReleasePlan.razor
        CreateTestPlan.razor
        Templates.razor
        Systems.razor
        Servers.razor
        Databases.razor
        Settings.razor
      Shared/
        TicketEditor.razor
        TicketListEditor.razor
        SystemSelector.razor
        ServerSelector.razor
        DatabaseSelector.razor
        TemplateSelector.razor
        MarkdownPreview.razor
        ExportActions.razor
        SqlScriptEditor.razor
        TestCaseEditor.razor
        ScreenshotPicker.razor
      Layout/
    Services/
    Repositories/
    Models/
    Data/
    Database/
      Migrations/
      Scripts/
    wwwroot/
    Program.cs
    appsettings.json
  DevHelper.Tests/
```

---

## 16. Acceptance Criteria

### Automated Test Expectations

Use `DevHelper.Tests` for automated tests. Prefer xUnit unless an existing test framework is already present.

Required coverage for Phase 1:

* `IDatabaseInitializer` creates the SQLite database, applies migrations in filename order, records migrations, and is idempotent when run more than once.
* Repository tests use a temporary SQLite database and verify create, read, update, delete, child collection persistence, cascade delete behavior, and transaction rollback for release plan saves.
* Service validation tests reject missing release date, environment, created by, ticket, selected system, and template.
* Markdown generation golden tests verify placeholder replacement, date formatting, collection ordering, empty collection rendering, Markdown table escaping, and unknown-placeholder warning behavior.
* Application settings tests verify sensitive values are encrypted at rest and decrypted only through the settings service.
* External SQL Server connection tests cover missing connection string and unreachable server without blocking manual ticket entry.
* A guard test verifies Entity Framework packages and `Microsoft.EntityFrameworkCore` namespaces are not used.

Required coverage for later phases:

* PDF generation has a smoke test that verifies a non-empty PDF is produced from known Markdown/HTML.
* Screenshot service tests verify allowed extensions, configured source directory validation, relative path storage, outside-directory rejection, missing-file handling, and that removing an attachment does not delete the image file.
* Test Plan service tests mirror Release Plan validation, transactional save, and Markdown generation coverage.

### General

* App runs on .NET 10 with Blazor Interactive Server.
* SQLite schema is created and migrated on startup.
* Entity Framework is not used anywhere.

### Shared Infrastructure

* Systems, servers, and databases are maintained independently and reused by both tools.
* Templates are scoped to tool type and support create, edit, duplicate, deactivate, set default.

### Release Plan Generator

* User can create, edit, and delete release plans.
* Release plans support multiple tickets, systems, servers, databases, SQL scripts, and screenshot attachments.
* Markdown is generated from the selected template with all placeholders resolved.
* Markdown can be previewed, copied, downloaded, and saved to SQLite.
* PDF can be generated and downloaded.
* Save uses a transaction over all child records.

### Test Plan Generator

* User can create, edit, and delete test plans.
* Test plans support multiple tickets, systems, servers, databases, and test cases.
* Test plans support multiple screenshot attachments selected from the configured screenshot source directory.
* Adding a screenshot shows a preview and lets the user enter a description before saving.
* Screenshot files are not uploaded, copied, renamed, moved, or deleted by the app.
* Markdown is generated from the selected template with all placeholders resolved.
* Generated Markdown references screenshots by file path.
* PDF includes embedded screenshots.
* Save uses a transaction over all child records.

### Settings

* SQL Server connection string can be stored (encrypted) and tested.
* Screenshot source directory can be configured.

### Logging

* All errors and key operations are logged via Serilog.

---

## 17. Future Enhancements

* SQL Server ticket lookup query and result mapping.
* Optional application URL settings and in-app screenshot capture via Playwright / PuppeteerSharp.
* Link test plans directly to release plans for traceability.
* Test plan status tracking and sign-off workflow.
* User authentication and role-based access.
* Email release plan or test plan to stakeholders.
* Audit log.
* Additional developer tools (e.g., deployment checklist generator, runbook generator).
