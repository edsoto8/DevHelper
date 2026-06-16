-- 002_SeedData.sql
-- Seed data for first run. EDIT THIS FILE to match your environment before first run:
-- replace the placeholder systems below with your real systems.

-- ---------------------------------------------------------------------------
-- Default systems (placeholders — edit before first run)
-- ---------------------------------------------------------------------------
INSERT INTO SystemEntries (Name, Description, IsActive, CreatedAt, UpdatedAt)
VALUES
    ('Web Application', 'Primary customer-facing web application.', 1, strftime('%Y-%m-%dT%H:%M:%fZ','now'), strftime('%Y-%m-%dT%H:%M:%fZ','now')),
    ('Internal API',    'Backend services and APIs.',               1, strftime('%Y-%m-%dT%H:%M:%fZ','now'), strftime('%Y-%m-%dT%H:%M:%fZ','now')),
    ('Reporting',       'Reporting and analytics system.',          1, strftime('%Y-%m-%dT%H:%M:%fZ','now'), strftime('%Y-%m-%dT%H:%M:%fZ','now'));

-- ---------------------------------------------------------------------------
-- Default Release Plan template (reproduces spec section 6.5)
-- ---------------------------------------------------------------------------
INSERT INTO Templates (ToolType, Name, Description, MarkdownTemplate, IsDefault, IsActive, CreatedAt, UpdatedAt)
VALUES (
    'ReleasePlan',
    'Default Release Plan',
    'Standard release plan layout.',
    '# Release Plan

## Release Information

**Release Date:** {{ReleaseDate}}
**Environment:** {{Environment}}
**Created By:** {{CreatedBy}}

---

# Tickets

{{Tickets}}

---

# Affected Systems

{{Systems}}

---

# Servers

{{Servers}}

---

# Databases

{{Databases}}

---

# Back Up Scripts

## Backup Requirements

- Confirm current database backup exists.
- Back up affected SQL scripts before deployment.

## Backup Steps

{{BackupSteps}}

---

# Deployment Steps

{{DeploymentSteps}}

---

# Execute SQL Scripts

## SQL Script List

{{SqlScripts}}

---

# Validation

## Validation Steps

{{ValidationSteps}}

---

# Rollback Plan

## Rollback Steps

{{RollbackSteps}}

---

# Screenshots

{{Screenshots}}

---

# Notes

{{Notes}}
',
    1,
    1,
    strftime('%Y-%m-%dT%H:%M:%fZ','now'),
    strftime('%Y-%m-%dT%H:%M:%fZ','now')
);
