INSERT OR IGNORE INTO SystemEntries (Name, Description, IsActive, CreatedAt, UpdatedAt)
VALUES
    ('Web Application',     'Front-end web application',            1, datetime('now'), datetime('now')),
    ('API Service',         'Back-end API service',                 1, datetime('now'), datetime('now')),
    ('Background Service',  'Background / worker service',          1, datetime('now'), datetime('now')),
    ('Database',            'Relational database',                  1, datetime('now'), datetime('now')),
    ('Other / Free Form',   'Custom or free-form system entry',     1, datetime('now'), datetime('now'));

INSERT OR IGNORE INTO ReleaseTemplates (Name, Description, MarkdownTemplate, IsDefault, IsActive, CreatedAt, UpdatedAt)
VALUES (
    'Default Release Plan',
    'Standard release plan template with all sections',
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
- Save copies of current configuration files if applicable.

## Backup Steps

1. [Backup step 1]
2. [Backup step 2]

---

# Deployment

## Deployment Steps

{{DeploymentSteps}}

---

# Execute SQL Scripts

## SQL Script List

{{SqlScripts}}

## SQL Execution Steps

1. Connect to the target SQL Server instance.
2. Execute scripts in the listed order.
3. Confirm scripts complete successfully.

---

# Validation

## Application Validation

{{ValidationSteps}}

---

# Rollback Plan

## Rollback Steps

{{RollbackSteps}}

---

# Notes

{{Notes}}
',
    1,
    1,
    datetime('now'),
    datetime('now')
);
