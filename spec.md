# Release Plan Generator Web App Specification

## 1. Overview

Build a .NET-based Blazor web application that helps users generate structured software release plans. The app should support multiple release plan templates, allow users to add one or more tickets to a release plan, maintain systems, servers, and databases, and generate release plans in Markdown and PDF formats.

The application should use SQLite as its internal application database for storing release plans, templates, systems, servers, databases, SQL scripts, settings, and user-entered data.

The application should use Dapper for data access. Entity Framework should not be used.

The app should optionally support ticket lookup from an external SQL Server database by allowing the user to configure a SQL Server connection string. The actual SQL query and lookup implementation can be handled later.

If the external SQL Server database is unavailable or not configured, the user should still be able to manually enter ticket information.

---

## 2. Goals

The application should allow users to:

1. Create a new release plan.
2. Add one or more tickets to the release plan.
3. Store all app data in SQLite.
4. Use Dapper for all SQLite data access.
5. Configure an optional external SQL Server connection string for future ticket lookup.
6. Manually enter ticket information when SQL Server lookup is unavailable or not implemented.
7. Select one or more affected systems.
8. Maintain a list of systems, servers, and databases.
9. Use reusable release plan templates.
10. Generate a clean Markdown release plan.
11. Export the release plan as a PDF.
12. Copy the generated Markdown to the clipboard.
13. Log application errors and events using Serilog.

---

## 3. Technology Stack

## 3.1 Frontend

* Blazor
* Razor Components
* C#
* CSS, Bootstrap, Tailwind CSS, or another component styling system
* Markdown preview component or custom Markdown preview rendering

## 3.2 Backend / Application Layer

* .NET
* ASP.NET Core
* C#
* Service-based application architecture

## 3.3 Data Access

* Dapper
* Microsoft.Data.Sqlite
* Microsoft.Data.SqlClient for future SQL Server lookup support

Entity Framework should not be used.

## 3.4 Application Database

* SQLite

SQLite should be used for storing:

* Release plans
* Release plan tickets
* Templates
* Systems
* Servers
* Databases
* SQL scripts
* Application settings
* Generated Markdown output
* PDF metadata or file paths

## 3.5 External Ticket Lookup Database

* SQL Server

SQL Server is only used as an optional external source for future ticket lookup.

The app should allow the user to configure and save a SQL Server connection string.

The app does not need to define the SQL Server ticket lookup query in the initial version. The query and mapping logic can be added later.

## 3.6 Logging

* Serilog

Serilog should be used for structured logging.

Recommended log targets:

* Console
* Rolling file logs

---

## 4. Primary Users

## Release Coordinator / Developer

A user responsible for preparing deployment instructions, SQL script execution steps, backups, validation steps, and release documentation.

---

## 5. Core Features

## 5.1 Release Plan Creation

Users should be able to create a new release plan with the following fields:

* Release Plan Title
* Release Date
* Environment

  * Development
  * QA
  * UAT
  * Production
  * Other
* Created By
* Ticket List
* Selected Template
* Affected Systems
* Servers
* Databases
* SQL Scripts
* Deployment Steps
* Validation Steps
* Rollback Steps
* Notes

---

## 5.2 Ticket Entry and Future Ticket Lookup

At the top of the release plan creation screen, the user should be able to enter ticket information.

### Ticket Fields

* Ticket Number
* Ticket Name
* Ticket Description / Summary

### Initial Behavior

For the initial version, ticket information can be entered manually.

The user should be able to manually enter:

* Ticket Number
* Ticket Name
* Ticket Description / Summary

### Future SQL Server Lookup Behavior

The app should allow a SQL Server connection string to be configured and stored.

Later, the app may use this connection string to look up ticket data from an external SQL Server database.

The actual query, table names, column names, and mapping logic will be handled later.

### Fallback Behavior

If SQL Server is not configured, unavailable, or lookup is not implemented:

1. Do not block the user.
2. Allow manual ticket entry.
3. Save manually entered ticket information to SQLite.
4. Log any connection errors using Serilog.

---

## 5.3 Multiple Tickets

Users should be able to add multiple tickets to a single release plan.

Each ticket should include:

* Ticket Number
* Ticket Name
* Summary / Description
* Affected Systems
* Notes
* Sort Order

Example:

```markdown
# F3121 First Ticket

## Summary

Brief summary of the ticket goes here.
```

The generated release plan should group ticket information clearly.

---

## 5.4 System Management

The app should include a maintainable list of known systems stored in SQLite.

Default systems:

* CashTrackNet
* CashTrackSettlement
* Recon Pro
* Cash Track Import Service
* Other / Free Form

Users should be able to:

* Add a system
* Edit a system
* Delete or deactivate a system
* Select one or more systems for a release plan
* Enter a free-form system name when needed

Each system may optionally have related:

* Servers
* Databases
* Common deployment steps
* Common validation steps

---

## 5.5 Server Management

Users should be able to maintain a list of servers stored in SQLite.

Server fields:

* Server Name
* Environment
* Related System
* Server Type

  * Web Server
  * App Server
  * Database Server
  * File Server
  * Service Server
  * Other
* Notes
* Active / Inactive

Users should be able to select servers when creating a release plan.

---

## 5.6 Database Management

Users should be able to maintain a list of databases stored in SQLite.

Database fields:

* Database Name
* SQL Server Instance
* Environment
* Related System
* Notes
* Active / Inactive

Users should be able to select databases when creating a release plan.

---

## 5.7 Template Management

The app should support multiple reusable release plan templates stored in SQLite.

Users should be able to:

* Create a template
* Edit a template
* Duplicate a template
* Delete or deactivate a template
* Set a default template
* Select a template when generating a release plan

Templates should define which sections appear in the generated release plan.

Example template sections:

* Summary
* Back Up Scripts
* Cash Track Net Release Plan
* Execute SQL Scripts
* Validation
* Rollback Plan
* Deployment Notes
* Post-Deployment Checks

---

## 5.8 Release Plan Generator

The app should generate a release plan from:

* Selected template
* Ticket information
* Selected systems
* Selected servers
* Selected databases
* SQL scripts
* User-entered deployment steps
* User-entered validation steps
* User-entered rollback steps
* Notes

The generated output should be displayed in a preview area before export.

Users should be able to:

* Preview Markdown
* Copy Markdown
* Download Markdown file
* Download PDF file
* Save the release plan to SQLite

---

## 6. Default Release Plan Markdown Format

The default generated release plan should look similar to this:

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

- CashTrackNet
- CashTrackSettlement
- Recon Pro
- Cash Track Import Service
- [Other System]

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
- Save copies of current configuration files if applicable.

## Backup Steps

1. [Backup step 1]
2. [Backup step 2]
3. [Backup step 3]

---

# Cash Track Net Release Plan

## Deployment Steps

1. Install latest version of [application/service].
2. Deploy updated files to [server].
3. Update configuration values if needed.
4. Restart related services or app pools.
5. Confirm application starts successfully.

---

# Execute SQL Scripts

## SQL Script List

| Order | Database | Script Name | Required |
|---|---|---|---|
| 1 | [Database Name] | [Script Name] | Yes |

## SQL Execution Steps

1. Connect to [SQL Server Instance].
2. Select database [Database Name].
3. Execute scripts in the listed order.
4. Confirm scripts complete successfully.
5. Save execution results if required.

---

# Validation

## Application Validation

- Install latest version of [application/service].
- Confirm application loads successfully.
- Confirm no startup errors are present.
- Confirm ticket-specific functionality works as expected.

## Database Validation

- Confirm expected schema changes exist.
- Confirm data updates were applied successfully.
- Confirm no unexpected errors were reported.

## Service Validation

- Confirm related services are running.
- Confirm logs do not show new errors.

---

# Rollback Plan

## Rollback Steps

1. Restore previous application files if needed.
2. Restore database backup if required.
3. Re-run previous known-good SQL scripts if applicable.
4. Restart affected services.
5. Validate system is back to prior working state.

---

# Notes

[Additional notes]
```

---

## 7. Screens / Pages

## 7.1 Dashboard

Purpose: View existing release plans stored in SQLite.

Features:

* Create new release plan
* Search release plans
* Filter by environment
* Filter by release date
* View recently generated plans
* Open previous release plans
* Copy or export previously generated plans

---

## 7.2 Create Release Plan Page

Main page for building a release plan.

Sections:

1. Release Information
2. Ticket Entry
3. Ticket List
4. Template Selection
5. Affected Systems
6. Servers
7. Databases
8. Deployment Steps
9. SQL Scripts
10. Validation Steps
11. Rollback Steps
12. Markdown Preview
13. Export Actions

Actions:

* Add Ticket
* Remove Ticket
* Reorder Tickets
* Add Custom System
* Generate Preview
* Copy Markdown
* Download Markdown
* Download PDF
* Save Release Plan

---

## 7.3 Template Management Page

Features:

* View templates
* Create template
* Edit template
* Duplicate template
* Deactivate template
* Set default template

Template fields:

* Template Name
* Description
* Markdown Template
* Active / Inactive
* Default / Not Default

---

## 7.4 Systems Management Page

Features:

* View systems
* Add system
* Edit system
* Deactivate system

System fields:

* System Name
* Description
* Active / Inactive

---

## 7.5 Servers Management Page

Features:

* View servers
* Add server
* Edit server
* Deactivate server

Server fields:

* Server Name
* Environment
* Related System
* Server Type
* Notes
* Active / Inactive

---

## 7.6 Databases Management Page

Features:

* View databases
* Add database
* Edit database
* Deactivate database

Database fields:

* Database Name
* SQL Server Instance
* Environment
* Related System
* Notes
* Active / Inactive

---

## 7.7 Settings Page

Purpose: Maintain application settings stored in SQLite or configuration files.

Settings may include:

* SQLite database path
* Optional SQL Server ticket lookup connection string
* Default environment
* Default template
* PDF output location
* Markdown output location
* Logging level

Sensitive values such as SQL Server passwords should not be stored as plain text.

---

## 8. SQLite Data Model

## 8.1 ReleasePlan

Fields:

* Id
* Title
* ReleaseDate
* Environment
* CreatedBy
* TemplateId
* MarkdownOutput
* PdfFilePath
* CreatedAt
* UpdatedAt

---

## 8.2 ReleasePlanTicket

Fields:

* Id
* ReleasePlanId
* TicketNumber
* TicketName
* TicketSummary
* SortOrder
* CreatedAt
* UpdatedAt

---

## 8.3 SystemEntry

Fields:

* Id
* Name
* Description
* IsActive
* CreatedAt
* UpdatedAt

Note: Use `SystemEntry` or another name instead of `System` in code to avoid confusion with the .NET `System` namespace.

---

## 8.4 ServerEntry

Fields:

* Id
* Name
* Environment
* SystemEntryId
* ServerType
* Notes
* IsActive
* CreatedAt
* UpdatedAt

---

## 8.5 DatabaseEntry

Fields:

* Id
* Name
* SqlServerInstance
* Environment
* SystemEntryId
* Notes
* IsActive
* CreatedAt
* UpdatedAt

---

## 8.6 ReleaseTemplate

Fields:

* Id
* Name
* Description
* MarkdownTemplate
* IsDefault
* IsActive
* CreatedAt
* UpdatedAt

---

## 8.7 SqlScript

Fields:

* Id
* ReleasePlanId
* DatabaseEntryId
* ScriptName
* ScriptDescription
* ExecutionOrder
* IsRequired
* Notes

---

## 8.8 ApplicationSetting

Fields:

* Id
* SettingKey
* SettingValue
* IsEncrypted
* CreatedAt
* UpdatedAt

Example setting keys:

* TicketLookupSqlServerConnectionString
* DefaultTemplateId
* DefaultEnvironment
* PdfOutputPath
* MarkdownOutputPath
* LogLevel

---

## 9. SQLite Schema

The application should initialize the SQLite database if it does not already exist.

Because Entity Framework is not being used, schema creation and updates should be handled through SQL scripts or a lightweight custom migration system.

Suggested approach:

1. On application startup, check whether the SQLite database exists.
2. If it does not exist, create it.
3. Run required schema creation scripts.
4. Store applied migration versions in a `SchemaMigrations` table.
5. On future app startup, apply only migrations that have not already been applied.

### SchemaMigrations Table

Fields:

* Id
* MigrationName
* AppliedAt

Example:

```sql
CREATE TABLE IF NOT EXISTS SchemaMigrations (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    MigrationName TEXT NOT NULL,
    AppliedAt TEXT NOT NULL
);
```

---

## 10. Dapper Data Access

The app should use Dapper for all SQLite data access.

Recommended packages:

* Dapper
* Microsoft.Data.Sqlite
* Microsoft.Data.SqlClient

Do not use Entity Framework or EF Core migrations.

### Connection Factory

Create a reusable SQLite connection factory.

Suggested interface:

```csharp
public interface ISqliteConnectionFactory
{
    IDbConnection CreateConnection();
}
```

Suggested implementation:

```csharp
public class SqliteConnectionFactory : ISqliteConnectionFactory
{
    private readonly string _connectionString;

    public SqliteConnectionFactory(IConfiguration configuration)
    {
        _connectionString = configuration.GetConnectionString("Sqlite")
            ?? "Data Source=release-plan-generator.db";
    }

    public IDbConnection CreateConnection()
    {
        return new SqliteConnection(_connectionString);
    }
}
```

### Repository Pattern

Use repositories or data services that use Dapper internally.

Suggested repositories:

```csharp
IReleasePlanRepository
IReleasePlanTicketRepository
IReleaseTemplateRepository
ISystemRepository
IServerRepository
IDatabaseRepository
ISqlScriptRepository
IApplicationSettingRepository
```

Example repository method:

```csharp
public async Task<IEnumerable<ReleasePlan>> GetAllAsync()
{
    using var connection = _connectionFactory.CreateConnection();

    const string sql = """
        SELECT
            Id,
            Title,
            ReleaseDate,
            Environment,
            CreatedBy,
            TemplateId,
            MarkdownOutput,
            PdfFilePath,
            CreatedAt,
            UpdatedAt
        FROM ReleasePlans
        ORDER BY ReleaseDate DESC;
        """;

    return await connection.QueryAsync<ReleasePlan>(sql);
}
```

### Dapper Guidelines

* Use parameterized SQL for all queries.
* Do not concatenate user input into SQL statements.
* Keep SQL statements readable and close to the repository method that uses them.
* Use transactions when saving a release plan with related tickets, scripts, systems, servers, or databases.
* Log failed database operations with Serilog.
* Keep database models simple and explicit.

---

## 11. External SQL Server Connection String Support

The app should support storing an optional external SQL Server connection string.

This is for future ticket lookup support only.

The first version does not need to implement the actual ticket lookup query.

### Settings Requirement

The Settings page should allow the user to enter:

* SQL Server connection string

Example:

```text
Server=my-server;Database=my-ticket-db;Trusted_Connection=True;TrustServerCertificate=True;
```

or:

```text
Server=my-server;Database=my-ticket-db;User Id=my-user;Password=my-password;TrustServerCertificate=True;
```

### Optional Connection Test

The app may include a “Test Connection” button.

If implemented, it should:

1. Attempt to open a SQL Server connection using the configured connection string.
2. Show whether the connection succeeded or failed.
3. Log failures using Serilog.
4. Not require a ticket lookup query.

Suggested interface:

```csharp
public interface IExternalSqlServerConnectionService
{
    Task<bool> TestConnectionAsync(string connectionString);
}
```

Suggested implementation behavior:

```csharp
public async Task<bool> TestConnectionAsync(string connectionString)
{
    try
    {
        await using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync();
        return true;
    }
    catch (Exception ex)
    {
        _logger.Error(ex, "External SQL Server connection test failed.");
        return false;
    }
}
```

### Future Ticket Lookup

The future ticket lookup implementation should use the configured SQL Server connection string.

The exact query, parameters, result mapping, and table structure will be handled later.

---

## 12. Serilog Logging

The app should use Serilog for structured logging.

The app should log:

* Application startup
* Application shutdown
* SQLite database initialization
* SQLite migration execution
* SQLite database errors
* Release plan save/update/export operations
* Template save/update/delete operations
* System, server, and database maintenance actions
* Optional SQL Server connection test attempts
* Optional SQL Server connection test failures
* PDF generation errors
* Markdown generation errors

Recommended log levels:

* Information for normal app activity
* Warning for recoverable issues
* Error for exceptions or failed operations
* Debug for detailed troubleshooting during development

Example log events:

```csharp
Log.Information("Release plan created. ReleasePlanId: {ReleasePlanId}", releasePlan.Id);

Log.Information("SQLite migration applied. MigrationName: {MigrationName}", migrationName);

Log.Warning("External SQL Server is not configured. Manual ticket entry will be used.");

Log.Error(ex, "PDF generation failed for ReleasePlanId {ReleasePlanId}", releasePlan.Id);
```

Recommended Serilog sinks:

* Console
* File

Example rolling file path:

```text
logs/release-plan-generator-.log
```

---

## 13. PDF Generation

The app should generate a PDF from the final Markdown output.

PDF requirements:

* Preserve headings
* Preserve bullet lists
* Preserve tables
* Include release title
* Include ticket numbers
* Include generated date
* Use readable formatting
* Download as `.pdf`

Recommended .NET PDF options:

* QuestPDF
* DinkToPdf
* Playwright for .NET
* PuppeteerSharp

Preferred approach:

1. Generate Markdown.
2. Convert Markdown to HTML.
3. Convert HTML to PDF.
4. Save or download the PDF.

Suggested filename format:

```text
ReleasePlan_[Environment]_[ReleaseDate]_[TicketNumbers].pdf
```

Example:

```text
ReleasePlan_Production_2026-06-04_F3121.pdf
```

---

## 14. Markdown Export

Users should be able to:

* Copy Markdown to clipboard
* Download Markdown as `.md`
* Save generated Markdown to SQLite as part of the release plan record

Suggested filename format:

```text
ReleasePlan_[Environment]_[ReleaseDate]_[TicketNumbers].md
```

Example:

```text
ReleasePlan_Production_2026-06-04_F3121.md
```

---

## 15. Application Architecture

## 15.1 Suggested Project Structure

```text
ReleasePlanGenerator/
  ReleasePlanGenerator.Web/
    Components/
    Pages/
    Layout/
    Services/
    Repositories/
    Models/
    Data/
    Database/
      Migrations/
      Scripts/
    wwwroot/
    appsettings.json
    Program.cs

  ReleasePlanGenerator.Tests/
```

For a larger solution, use separate projects:

```text
ReleasePlanGenerator/
  ReleasePlanGenerator.Web/
  ReleasePlanGenerator.Application/
  ReleasePlanGenerator.Infrastructure/
  ReleasePlanGenerator.Domain/
  ReleasePlanGenerator.Tests/
```

---

## 15.2 Suggested Services

```csharp
IReleasePlanService
ITemplateService
ISystemService
IServerService
IDatabaseService
IMarkdownGenerationService
IPdfGenerationService
IApplicationSettingsService
IExternalSqlServerConnectionService
IDatabaseInitializer
```

---

## 15.3 Suggested Repositories

```csharp
IReleasePlanRepository
IReleasePlanTicketRepository
IReleaseTemplateRepository
ISystemRepository
IServerRepository
IDatabaseRepository
ISqlScriptRepository
IApplicationSettingRepository
```

---

## 15.4 Blazor Components

Suggested reusable components:

* TicketEntryForm
* TicketListEditor
* ReleaseInfoForm
* TemplateSelector
* SystemSelector
* ServerSelector
* DatabaseSelector
* SqlScriptEditor
* DeploymentStepEditor
* ValidationStepEditor
* RollbackStepEditor
* MarkdownPreview
* ExportActions
* SettingsForm

---

## 16. Configuration

Use `appsettings.json` for default configuration.

Example:

```json
{
  "ConnectionStrings": {
    "Sqlite": "Data Source=release-plan-generator.db",
    "ExternalSqlServer": ""
  },
  "Serilog": {
    "MinimumLevel": "Information",
    "WriteTo": [
      {
        "Name": "Console"
      },
      {
        "Name": "File",
        "Args": {
          "path": "logs/release-plan-generator-.log",
          "rollingInterval": "Day"
        }
      }
    ]
  }
}
```

The SQLite connection string should be required.

The external SQL Server connection string should be optional.

---

## 17. Validation Rules

Required fields:

* Release Date
* Environment
* At least one ticket
* At least one selected system or manually entered system
* Selected template

Ticket requirements:

* Ticket Number is required.
* Ticket Name or Description is required.
* Manual entry is always allowed.

Template requirements:

* Template Name is required.
* Markdown Template is required.

System requirements:

* System Name is required.

Server requirements:

* Server Name is required.
* Environment is required.

Database requirements:

* Database Name is required.
* SQL Server Instance is required.

SQL script requirements:

* Script Name is required.
* Execution Order is required when multiple scripts are included.

---

## 18. User Flow

## Create Release Plan Flow

1. User opens the Blazor app.
2. User clicks “Create Release Plan.”
3. User enters release information.
4. User manually enters ticket information.
5. User adds one or more tickets.
6. User selects a release template.
7. User selects affected systems.
8. User selects servers and databases.
9. User adds SQL scripts, deployment steps, validation steps, and rollback steps.
10. User clicks “Generate Preview.”
11. App generates Markdown preview.
12. User reviews and edits as needed.
13. User saves the release plan to SQLite using Dapper.
14. User copies Markdown or exports PDF.

---

## 19. Acceptance Criteria

### .NET / Blazor App

* The application is built using .NET and Blazor.
* The app runs locally or on an internal server.
* The app uses Razor components for the UI.

### SQLite Storage

* Release plans are saved to SQLite.
* Templates are saved to SQLite.
* Systems, servers, and databases are saved to SQLite.
* Generated Markdown is saved to SQLite.
* SQLite schema is created through SQL scripts or a lightweight custom migration system.
* Entity Framework is not used.

### Dapper Data Access

* All SQLite reads and writes use Dapper.
* All SQL statements are parameterized.
* Repository or data service classes are used for data access.
* Transactions are used when saving release plans with related child records.
* Database failures are logged with Serilog.

### External SQL Server Connection

* The app allows an optional SQL Server connection string to be configured.
* The app can store the SQL Server connection string securely or mark it as sensitive.
* The app may provide a connection test.
* No ticket lookup query is required in the initial version.
* Manual ticket entry works whether SQL Server is configured or not.

### Multiple Tickets

* User can add multiple tickets to a release plan.
* User can remove tickets.
* User can reorder tickets.
* Generated Markdown includes all selected tickets.

### Templates

* User can select from available templates.
* User can create and edit templates.
* Generated release plan follows the selected template.
* Templates are stored in SQLite.

### Systems

* Default systems are available:

  * CashTrackNet
  * CashTrackSettlement
  * Recon Pro
  * Cash Track Import Service
* User can add a custom system.
* User can maintain the system list.
* Systems are stored in SQLite.

### Servers and Databases

* User can maintain servers.
* User can maintain databases.
* User can associate servers and databases with systems.
* User can include selected servers and databases in the generated release plan.
* Servers and databases are stored in SQLite.

### Markdown Export

* User can preview Markdown.
* User can copy Markdown to clipboard.
* User can download Markdown as a `.md` file.

### PDF Export

* User can generate a PDF.
* PDF matches the generated release plan content.
* PDF preserves headings, tables, and bullet lists.

### Logging

* Application errors are logged using Serilog.
* SQLite initialization and migration failures are logged.
* PDF generation failures are logged.
* Release plan creation and export events are logged.
* Optional SQL Server connection test results are logged.

---

## 20. Future Enhancements

Possible future features:

* SQL Server ticket lookup query configuration
* Ticket lookup mapping configuration
* User authentication
* Saved release plan history
* Approval workflow
* Release plan status tracking
* Attach SQL script files
* Attach screenshots or validation evidence
* Email release plan to stakeholders
* Integration with ticketing systems
* Version history for templates
* Audit log
* Role-based permissions
* Environment-specific templates
* Automated database backup checks
* Automated service status checks
* AI-assisted validation step generation
