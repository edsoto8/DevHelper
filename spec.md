# DevHelper — Application Specification

## 1. Overview

**DevHelper** is a .NET 10 Blazor web application that provides a suite of developer productivity tools. The application is designed to be modular — each tool shares a common infrastructure (systems, servers, databases, settings, templates) but operates independently.

### Tools

| Tool | Description | Status |
|---|---|---|
| Release Plan Generator | Create, manage, and export structured software release plans | v1 |
| Test Plan Generator | Create, manage, and export structured test plans with test cases and screenshots | v1 |

---

## 2. Goals

The application should allow users to:

1. Create and manage release plans and test plans.
2. Reuse shared infrastructure — systems, servers, databases, and templates — across both tools.
3. Store all application data in SQLite using Dapper.
4. Configure an optional external SQL Server connection string for future ticket lookup.
5. Generate clean Markdown output from both tools.
6. Export Markdown and PDF from both tools.
7. Attach and display screenshots in test plans.
8. Configure one or more base URLs for the application under test (used in the Test Plan tool).
9. Log all application events and errors using Serilog.

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
* Screenshots (stored as file paths; binary blobs optionally)
* Application settings

### 3.5 External Ticket Lookup

SQL Server is an optional future data source for ticket lookup. v1 only stores the connection string.

### 3.6 Logging

* Serilog with Console and rolling File sinks

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
* Deployment Steps
* Validation Steps
* Rollback Steps
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
| `{{ReleaseDate}}` | Formatted release date |
| `{{Environment}}` | Selected environment |
| `{{CreatedBy}}` | Created by name |
| `{{Tickets}}` | All tickets formatted as Markdown |
| `{{Systems}}` | Selected systems as a bullet list |
| `{{Servers}}` | Selected servers as a bullet list |
| `{{Databases}}` | Selected databases as a bullet list |
| `{{SqlScripts}}` | SQL scripts as a Markdown table |
| `{{DeploymentSteps}}` | Numbered deployment steps |
| `{{ValidationSteps}}` | Numbered validation steps |
| `{{RollbackSteps}}` | Numbered rollback steps |
| `{{Notes}}` | Additional notes |

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

Saving a release plan saves the plan and all child records (tickets, SQL scripts, system/server/database links) in a single transaction.

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
* Screenshots (one or more — see section 7.5)
* Notes
* Sort Order

Users can add, remove, and reorder test cases.

### 7.4 Screenshot Configuration

In Settings, users configure one or more **Application URLs** — the base URL(s) of the application under test.

Fields per URL entry:

* Label (e.g., "QA Environment")
* Base URL (e.g., `http://qa.internal/app`)
* Active / Inactive

These URLs are used to:

* Provide quick-launch links within the test plan form (opens in a new browser tab so the tester can navigate and capture screenshots manually)
* Pre-populate URL metadata in screenshot attachments

### 7.5 Screenshot Attachment

Each test case can have one or more screenshots attached.

Screenshot fields:

* Caption / Description
* File Path (stored in SQLite; file saved to a configured screenshot output directory)
* Captured URL (optional — the page URL where the screenshot was taken)
* Captured At (timestamp)
* Sort Order

Users upload screenshot files through the UI. Files are saved to the configured screenshot directory on the server. The file path and metadata are stored in SQLite.

Screenshots are included in the generated PDF and referenced by file path in the generated Markdown.

### 7.6 Markdown Generation

The test plan is generated from the selected template with the following placeholders:

| Placeholder | Value |
|---|---|
| `{{TestDate}}` | Formatted test date |
| `{{Environment}}` | Selected environment |
| `{{TestedBy}}` | Tested by name |
| `{{Status}}` | Overall test plan status |
| `{{Tickets}}` | Tickets formatted as Markdown |
| `{{Systems}}` | Affected systems as a bullet list |
| `{{Servers}}` | Servers as a bullet list |
| `{{Databases}}` | Databases as a bullet list |
| `{{TestCases}}` | All test cases formatted as Markdown |
| `{{Notes}}` | Additional notes |

### 7.7 Default Test Plan Markdown Format

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

### Screenshots

![Caption](path/to/screenshot.png)

---

# Notes

[Notes]
```

### 7.8 Validation Rules

Required to save:

* Test Date
* Environment
* Tested By
* At least one ticket (Ticket Number required)
* At least one test case (Test Case Name required)
* Selected template

### 7.9 Save Behavior

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
11. Notes
12. Markdown Preview
13. Export Actions (Save, Copy Markdown, Download .md, Download PDF)

### 8.3 Create / Edit Test Plan

Sections:

1. Test Plan Information
2. Template Selection
3. Tickets
4. Affected Systems
5. Servers
6. Databases
7. Test Cases (with inline screenshot upload per test case)
8. Notes
9. Markdown Preview
10. Export Actions (Save, Copy Markdown, Download .md, Download PDF)

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
* **Application URLs** — base URLs for the application under test (used in Test Plan tool); add, edit, deactivate.
* **Screenshot Output Directory** — local or server path where uploaded screenshots are saved.
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

### 9.13 TestCaseScreenshots

| Column | Type |
|---|---|
| Id | INTEGER PK |
| TestCaseId | INTEGER FK (CASCADE DELETE) |
| Caption | TEXT |
| FilePath | TEXT NOT NULL |
| CapturedUrl | TEXT |
| CapturedAt | TEXT |
| SortOrder | INTEGER DEFAULT 0 |
| CreatedAt | TEXT NOT NULL |

### 9.14 TestPlanSystems / TestPlanServers / TestPlanDatabases

Join tables linking a test plan to its selected systems, servers, and databases.

### 9.15 ApplicationSettings

| Column | Type |
|---|---|
| Id | INTEGER PK |
| SettingKey | TEXT NOT NULL UNIQUE |
| SettingValue | TEXT |
| IsEncrypted | INTEGER DEFAULT 0 |
| CreatedAt | TEXT NOT NULL |
| UpdatedAt | TEXT NOT NULL |

Example setting keys: `TicketLookupSqlServerConnectionString`, `ScreenshotOutputDirectory`, `DefaultEnvironment`, `LogLevel`.

### 9.16 ApplicationUrls

| Column | Type |
|---|---|
| Id | INTEGER PK |
| Label | TEXT NOT NULL |
| BaseUrl | TEXT NOT NULL |
| IsActive | INTEGER DEFAULT 1 |
| CreatedAt | TEXT NOT NULL |
| UpdatedAt | TEXT NOT NULL |

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
IApplicationUrlRepository
```

### Suggested Service Interfaces

```
IReleasePlanService
ITestPlanService
IMarkdownGenerationService        (shared; accepts a plan type)
IPdfGenerationService             (shared)
IApplicationSettingsService
IApplicationUrlService
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
3. Convert HTML to PDF (QuestPDF or PuppeteerSharp).
4. Embed screenshots where referenced.
5. Download as `.pdf`.

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
* Screenshot upload and save
* SQL Server connection test attempts and results
* Any unhandled exceptions

---

## 15. Project Structure

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
        ScreenshotUploader.razor
      Layout/
    Services/
    Repositories/
    Models/
    Data/
    Database/
      Migrations/
    wwwroot/
    Program.cs
    appsettings.json
  DevHelper.Tests/
```

---

## 16. Acceptance Criteria

### General

* App runs on .NET 10 with Blazor Interactive Server.
* SQLite schema is created and migrated on startup.
* Entity Framework is not used anywhere.

### Shared Infrastructure

* Systems, servers, and databases are maintained independently and reused by both tools.
* Templates are scoped to tool type and support create, edit, duplicate, deactivate, set default.

### Release Plan Generator

* User can create, edit, and delete release plans.
* Release plans support multiple tickets, systems, servers, databases, and SQL scripts.
* Markdown is generated from the selected template with all placeholders resolved.
* Markdown can be previewed, copied, downloaded, and saved to SQLite.
* PDF can be generated and downloaded.
* Save uses a transaction over all child records.

### Test Plan Generator

* User can create, edit, and delete test plans.
* Test plans support multiple tickets, systems, servers, databases, and test cases.
* Each test case supports multiple screenshot attachments.
* Screenshots are saved to the configured output directory; file paths stored in SQLite.
* Application URLs can be configured in Settings and used as quick-launch links in the test plan form.
* Markdown is generated from the selected template with all placeholders resolved.
* Generated Markdown references screenshots by file path.
* PDF includes embedded screenshots.
* Save uses a transaction over all child records.

### Settings

* SQL Server connection string can be stored (encrypted) and tested.
* Application URLs can be added, edited, and deactivated.
* Screenshot output directory can be configured.

### Logging

* All errors and key operations are logged via Serilog.

---

## 17. Future Enhancements

* SQL Server ticket lookup query and result mapping.
* In-app screenshot capture via Playwright / PuppeteerSharp from a configured application URL.
* Link test plans directly to release plans for traceability.
* Test plan status tracking and sign-off workflow.
* User authentication and role-based access.
* Email release plan or test plan to stakeholders.
* Audit log.
* Additional developer tools (e.g., deployment checklist generator, runbook generator).
