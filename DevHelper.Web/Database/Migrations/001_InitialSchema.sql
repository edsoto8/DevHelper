-- 001_InitialSchema.sql
-- Creates the full DevHelper schema. The Test Plan tables are created up front
-- because the schema is shared infrastructure, even though the Test Plan workflow
-- is implemented in a later phase.

-- ---------------------------------------------------------------------------
-- Shared infrastructure
-- ---------------------------------------------------------------------------

CREATE TABLE IF NOT EXISTS SystemEntries (
    Id          INTEGER PRIMARY KEY AUTOINCREMENT,
    Name        TEXT NOT NULL,
    Description TEXT,
    IsActive    INTEGER NOT NULL DEFAULT 1,
    CreatedAt   TEXT NOT NULL,
    UpdatedAt   TEXT NOT NULL
);

CREATE TABLE IF NOT EXISTS ServerEntries (
    Id            INTEGER PRIMARY KEY AUTOINCREMENT,
    Name          TEXT NOT NULL,
    Environment   TEXT NOT NULL,
    SystemEntryId INTEGER REFERENCES SystemEntries(Id) ON DELETE SET NULL,
    ServerType    TEXT NOT NULL,
    Notes         TEXT,
    IsActive      INTEGER NOT NULL DEFAULT 1,
    CreatedAt     TEXT NOT NULL,
    UpdatedAt     TEXT NOT NULL
);

CREATE TABLE IF NOT EXISTS DatabaseEntries (
    Id                INTEGER PRIMARY KEY AUTOINCREMENT,
    Name              TEXT NOT NULL,
    SqlServerInstance TEXT NOT NULL,
    Environment       TEXT NOT NULL,
    SystemEntryId     INTEGER REFERENCES SystemEntries(Id) ON DELETE SET NULL,
    Notes             TEXT,
    IsActive          INTEGER NOT NULL DEFAULT 1,
    CreatedAt         TEXT NOT NULL,
    UpdatedAt         TEXT NOT NULL
);

CREATE TABLE IF NOT EXISTS Templates (
    Id               INTEGER PRIMARY KEY AUTOINCREMENT,
    ToolType         TEXT NOT NULL,
    Name             TEXT NOT NULL,
    Description      TEXT,
    MarkdownTemplate TEXT NOT NULL,
    IsDefault        INTEGER NOT NULL DEFAULT 0,
    IsActive         INTEGER NOT NULL DEFAULT 1,
    CreatedAt        TEXT NOT NULL,
    UpdatedAt        TEXT NOT NULL
);

-- ---------------------------------------------------------------------------
-- Release plans
-- ---------------------------------------------------------------------------

CREATE TABLE IF NOT EXISTS ReleasePlans (
    Id              INTEGER PRIMARY KEY AUTOINCREMENT,
    Title           TEXT NOT NULL,
    ReleaseDate     TEXT NOT NULL,
    Environment     TEXT NOT NULL,
    CreatedBy       TEXT NOT NULL,
    TemplateId      INTEGER REFERENCES Templates(Id) ON DELETE SET NULL,
    BackupSteps     TEXT,
    DeploymentSteps TEXT,
    ValidationSteps TEXT,
    RollbackSteps   TEXT,
    Notes           TEXT,
    MarkdownOutput  TEXT,
    PdfFilePath     TEXT,
    CreatedAt       TEXT NOT NULL,
    UpdatedAt       TEXT NOT NULL
);

CREATE TABLE IF NOT EXISTS ReleasePlanTickets (
    Id            INTEGER PRIMARY KEY AUTOINCREMENT,
    ReleasePlanId INTEGER NOT NULL REFERENCES ReleasePlans(Id) ON DELETE CASCADE,
    TicketNumber  TEXT NOT NULL,
    TicketName    TEXT NOT NULL,
    TicketSummary TEXT,
    SortOrder     INTEGER NOT NULL DEFAULT 0,
    CreatedAt     TEXT NOT NULL,
    UpdatedAt     TEXT NOT NULL
);
CREATE INDEX IF NOT EXISTS IX_ReleasePlanTickets_ReleasePlanId ON ReleasePlanTickets(ReleasePlanId);

CREATE TABLE IF NOT EXISTS SqlScripts (
    Id                INTEGER PRIMARY KEY AUTOINCREMENT,
    ReleasePlanId     INTEGER NOT NULL REFERENCES ReleasePlans(Id) ON DELETE CASCADE,
    DatabaseEntryId   INTEGER REFERENCES DatabaseEntries(Id) ON DELETE SET NULL,
    ScriptName        TEXT NOT NULL,
    ScriptDescription TEXT,
    ExecutionOrder    INTEGER NOT NULL DEFAULT 1,
    IsRequired        INTEGER NOT NULL DEFAULT 1,
    Notes             TEXT
);
CREATE INDEX IF NOT EXISTS IX_SqlScripts_ReleasePlanId ON SqlScripts(ReleasePlanId);

CREATE TABLE IF NOT EXISTS ReleasePlanSystems (
    Id              INTEGER PRIMARY KEY AUTOINCREMENT,
    ReleasePlanId   INTEGER NOT NULL REFERENCES ReleasePlans(Id) ON DELETE CASCADE,
    SystemEntryId   INTEGER REFERENCES SystemEntries(Id) ON DELETE CASCADE,
    OtherSystemName TEXT,
    SortOrder       INTEGER NOT NULL DEFAULT 0
);
CREATE UNIQUE INDEX IF NOT EXISTS UX_ReleasePlanSystems_Plan_System
    ON ReleasePlanSystems(ReleasePlanId, SystemEntryId) WHERE SystemEntryId IS NOT NULL;
CREATE INDEX IF NOT EXISTS IX_ReleasePlanSystems_ReleasePlanId ON ReleasePlanSystems(ReleasePlanId);

CREATE TABLE IF NOT EXISTS ReleasePlanServers (
    Id            INTEGER PRIMARY KEY AUTOINCREMENT,
    ReleasePlanId INTEGER NOT NULL REFERENCES ReleasePlans(Id) ON DELETE CASCADE,
    ServerEntryId INTEGER NOT NULL REFERENCES ServerEntries(Id) ON DELETE CASCADE,
    SortOrder     INTEGER NOT NULL DEFAULT 0
);
CREATE UNIQUE INDEX IF NOT EXISTS UX_ReleasePlanServers_Plan_Server
    ON ReleasePlanServers(ReleasePlanId, ServerEntryId);
CREATE INDEX IF NOT EXISTS IX_ReleasePlanServers_ReleasePlanId ON ReleasePlanServers(ReleasePlanId);

CREATE TABLE IF NOT EXISTS ReleasePlanDatabases (
    Id              INTEGER PRIMARY KEY AUTOINCREMENT,
    ReleasePlanId   INTEGER NOT NULL REFERENCES ReleasePlans(Id) ON DELETE CASCADE,
    DatabaseEntryId INTEGER NOT NULL REFERENCES DatabaseEntries(Id) ON DELETE CASCADE,
    SortOrder       INTEGER NOT NULL DEFAULT 0
);
CREATE UNIQUE INDEX IF NOT EXISTS UX_ReleasePlanDatabases_Plan_Database
    ON ReleasePlanDatabases(ReleasePlanId, DatabaseEntryId);
CREATE INDEX IF NOT EXISTS IX_ReleasePlanDatabases_ReleasePlanId ON ReleasePlanDatabases(ReleasePlanId);

-- ---------------------------------------------------------------------------
-- Test plans (schema only; workflow implemented in a later phase)
-- ---------------------------------------------------------------------------

CREATE TABLE IF NOT EXISTS TestPlans (
    Id                   INTEGER PRIMARY KEY AUTOINCREMENT,
    Title                TEXT NOT NULL,
    TestDate             TEXT NOT NULL,
    Environment          TEXT NOT NULL,
    TestedBy             TEXT NOT NULL,
    RelatedReleasePlanId INTEGER REFERENCES ReleasePlans(Id) ON DELETE SET NULL,
    TemplateId           INTEGER REFERENCES Templates(Id) ON DELETE SET NULL,
    Status               TEXT NOT NULL DEFAULT 'Draft',
    Notes                TEXT,
    MarkdownOutput       TEXT,
    PdfFilePath          TEXT,
    CreatedAt            TEXT NOT NULL,
    UpdatedAt            TEXT NOT NULL
);

CREATE TABLE IF NOT EXISTS TestPlanTickets (
    Id            INTEGER PRIMARY KEY AUTOINCREMENT,
    TestPlanId    INTEGER NOT NULL REFERENCES TestPlans(Id) ON DELETE CASCADE,
    TicketNumber  TEXT NOT NULL,
    TicketName    TEXT NOT NULL,
    TicketSummary TEXT,
    SortOrder     INTEGER NOT NULL DEFAULT 0,
    CreatedAt     TEXT NOT NULL,
    UpdatedAt     TEXT NOT NULL
);
CREATE INDEX IF NOT EXISTS IX_TestPlanTickets_TestPlanId ON TestPlanTickets(TestPlanId);

CREATE TABLE IF NOT EXISTS TestCases (
    Id             INTEGER PRIMARY KEY AUTOINCREMENT,
    TestPlanId     INTEGER NOT NULL REFERENCES TestPlans(Id) ON DELETE CASCADE,
    TestCaseNumber TEXT,
    Name           TEXT NOT NULL,
    Description    TEXT,
    PreConditions  TEXT,
    Steps          TEXT,
    ExpectedResult TEXT,
    ActualResult   TEXT,
    Status         TEXT NOT NULL DEFAULT 'Not Run',
    Notes          TEXT,
    SortOrder      INTEGER NOT NULL DEFAULT 0,
    CreatedAt      TEXT NOT NULL,
    UpdatedAt      TEXT NOT NULL
);
CREATE INDEX IF NOT EXISTS IX_TestCases_TestPlanId ON TestCases(TestPlanId);

CREATE TABLE IF NOT EXISTS TestPlanSystems (
    Id              INTEGER PRIMARY KEY AUTOINCREMENT,
    TestPlanId      INTEGER NOT NULL REFERENCES TestPlans(Id) ON DELETE CASCADE,
    SystemEntryId   INTEGER REFERENCES SystemEntries(Id) ON DELETE CASCADE,
    OtherSystemName TEXT,
    SortOrder       INTEGER NOT NULL DEFAULT 0
);
CREATE UNIQUE INDEX IF NOT EXISTS UX_TestPlanSystems_Plan_System
    ON TestPlanSystems(TestPlanId, SystemEntryId) WHERE SystemEntryId IS NOT NULL;
CREATE INDEX IF NOT EXISTS IX_TestPlanSystems_TestPlanId ON TestPlanSystems(TestPlanId);

CREATE TABLE IF NOT EXISTS TestPlanServers (
    Id            INTEGER PRIMARY KEY AUTOINCREMENT,
    TestPlanId    INTEGER NOT NULL REFERENCES TestPlans(Id) ON DELETE CASCADE,
    ServerEntryId INTEGER NOT NULL REFERENCES ServerEntries(Id) ON DELETE CASCADE,
    SortOrder     INTEGER NOT NULL DEFAULT 0
);
CREATE UNIQUE INDEX IF NOT EXISTS UX_TestPlanServers_Plan_Server
    ON TestPlanServers(TestPlanId, ServerEntryId);
CREATE INDEX IF NOT EXISTS IX_TestPlanServers_TestPlanId ON TestPlanServers(TestPlanId);

CREATE TABLE IF NOT EXISTS TestPlanDatabases (
    Id              INTEGER PRIMARY KEY AUTOINCREMENT,
    TestPlanId      INTEGER NOT NULL REFERENCES TestPlans(Id) ON DELETE CASCADE,
    DatabaseEntryId INTEGER NOT NULL REFERENCES DatabaseEntries(Id) ON DELETE CASCADE,
    SortOrder       INTEGER NOT NULL DEFAULT 0
);
CREATE UNIQUE INDEX IF NOT EXISTS UX_TestPlanDatabases_Plan_Database
    ON TestPlanDatabases(TestPlanId, DatabaseEntryId);
CREATE INDEX IF NOT EXISTS IX_TestPlanDatabases_TestPlanId ON TestPlanDatabases(TestPlanId);

-- ---------------------------------------------------------------------------
-- Cross-tool tables
-- ---------------------------------------------------------------------------

CREATE TABLE IF NOT EXISTS PlanScreenshots (
    Id          INTEGER PRIMARY KEY AUTOINCREMENT,
    PlanType    TEXT NOT NULL,
    PlanId      INTEGER NOT NULL,
    Description TEXT,
    FilePath    TEXT NOT NULL,
    AttachedAt  TEXT NOT NULL,
    SortOrder   INTEGER NOT NULL DEFAULT 0,
    CreatedAt   TEXT NOT NULL,
    UpdatedAt   TEXT NOT NULL
);
CREATE INDEX IF NOT EXISTS IX_PlanScreenshots_Plan ON PlanScreenshots(PlanType, PlanId, SortOrder);

CREATE TABLE IF NOT EXISTS ApplicationSettings (
    Id           INTEGER PRIMARY KEY AUTOINCREMENT,
    SettingKey   TEXT NOT NULL UNIQUE,
    SettingValue TEXT,
    IsEncrypted  INTEGER NOT NULL DEFAULT 0,
    CreatedAt    TEXT NOT NULL,
    UpdatedAt    TEXT NOT NULL
);
