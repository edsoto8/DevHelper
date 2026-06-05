CREATE TABLE IF NOT EXISTS SchemaMigrations (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    MigrationName TEXT NOT NULL,
    AppliedAt TEXT NOT NULL
);

CREATE TABLE IF NOT EXISTS ReleaseTemplates (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    Name TEXT NOT NULL,
    Description TEXT,
    MarkdownTemplate TEXT NOT NULL,
    IsDefault INTEGER NOT NULL DEFAULT 0,
    IsActive INTEGER NOT NULL DEFAULT 1,
    CreatedAt TEXT NOT NULL,
    UpdatedAt TEXT NOT NULL
);

CREATE TABLE IF NOT EXISTS SystemEntries (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    Name TEXT NOT NULL,
    Description TEXT,
    IsActive INTEGER NOT NULL DEFAULT 1,
    CreatedAt TEXT NOT NULL,
    UpdatedAt TEXT NOT NULL
);

CREATE TABLE IF NOT EXISTS ServerEntries (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    Name TEXT NOT NULL,
    Environment TEXT NOT NULL,
    SystemEntryId INTEGER,
    ServerType TEXT NOT NULL,
    Notes TEXT,
    IsActive INTEGER NOT NULL DEFAULT 1,
    CreatedAt TEXT NOT NULL,
    UpdatedAt TEXT NOT NULL,
    FOREIGN KEY (SystemEntryId) REFERENCES SystemEntries(Id)
);

CREATE TABLE IF NOT EXISTS DatabaseEntries (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    Name TEXT NOT NULL,
    SqlServerInstance TEXT NOT NULL,
    Environment TEXT NOT NULL,
    SystemEntryId INTEGER,
    Notes TEXT,
    IsActive INTEGER NOT NULL DEFAULT 1,
    CreatedAt TEXT NOT NULL,
    UpdatedAt TEXT NOT NULL,
    FOREIGN KEY (SystemEntryId) REFERENCES SystemEntries(Id)
);

CREATE TABLE IF NOT EXISTS ReleasePlans (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    Title TEXT NOT NULL,
    ReleaseDate TEXT NOT NULL,
    Environment TEXT NOT NULL,
    CreatedBy TEXT NOT NULL,
    TemplateId INTEGER,
    DeploymentSteps TEXT,
    ValidationSteps TEXT,
    RollbackSteps TEXT,
    Notes TEXT,
    MarkdownOutput TEXT,
    PdfFilePath TEXT,
    CreatedAt TEXT NOT NULL,
    UpdatedAt TEXT NOT NULL,
    FOREIGN KEY (TemplateId) REFERENCES ReleaseTemplates(Id)
);

CREATE TABLE IF NOT EXISTS ReleasePlanTickets (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    ReleasePlanId INTEGER NOT NULL,
    TicketNumber TEXT NOT NULL,
    TicketName TEXT NOT NULL,
    TicketSummary TEXT,
    SortOrder INTEGER NOT NULL DEFAULT 0,
    CreatedAt TEXT NOT NULL,
    UpdatedAt TEXT NOT NULL,
    FOREIGN KEY (ReleasePlanId) REFERENCES ReleasePlans(Id) ON DELETE CASCADE
);

CREATE TABLE IF NOT EXISTS SqlScripts (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    ReleasePlanId INTEGER NOT NULL,
    DatabaseEntryId INTEGER,
    ScriptName TEXT NOT NULL,
    ScriptDescription TEXT,
    ExecutionOrder INTEGER NOT NULL DEFAULT 1,
    IsRequired INTEGER NOT NULL DEFAULT 1,
    Notes TEXT,
    FOREIGN KEY (ReleasePlanId) REFERENCES ReleasePlans(Id) ON DELETE CASCADE,
    FOREIGN KEY (DatabaseEntryId) REFERENCES DatabaseEntries(Id)
);

CREATE TABLE IF NOT EXISTS ReleasePlanSystems (
    ReleasePlanId INTEGER NOT NULL,
    SystemEntryId INTEGER NOT NULL,
    PRIMARY KEY (ReleasePlanId, SystemEntryId),
    FOREIGN KEY (ReleasePlanId) REFERENCES ReleasePlans(Id) ON DELETE CASCADE,
    FOREIGN KEY (SystemEntryId) REFERENCES SystemEntries(Id)
);

CREATE TABLE IF NOT EXISTS ReleasePlanServers (
    ReleasePlanId INTEGER NOT NULL,
    ServerEntryId INTEGER NOT NULL,
    PRIMARY KEY (ReleasePlanId, ServerEntryId),
    FOREIGN KEY (ReleasePlanId) REFERENCES ReleasePlans(Id) ON DELETE CASCADE,
    FOREIGN KEY (ServerEntryId) REFERENCES ServerEntries(Id)
);

CREATE TABLE IF NOT EXISTS ReleasePlanDatabases (
    ReleasePlanId INTEGER NOT NULL,
    DatabaseEntryId INTEGER NOT NULL,
    PRIMARY KEY (ReleasePlanId, DatabaseEntryId),
    FOREIGN KEY (ReleasePlanId) REFERENCES ReleasePlans(Id) ON DELETE CASCADE,
    FOREIGN KEY (DatabaseEntryId) REFERENCES DatabaseEntries(Id)
);

CREATE TABLE IF NOT EXISTS ApplicationSettings (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    SettingKey TEXT NOT NULL UNIQUE,
    SettingValue TEXT,
    IsEncrypted INTEGER NOT NULL DEFAULT 0,
    CreatedAt TEXT NOT NULL,
    UpdatedAt TEXT NOT NULL
);
