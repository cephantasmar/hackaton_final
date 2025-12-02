-- Cleanup and migration script for BIGSERIAL IDs
-- Run this in your Supabase SQL Editor

-- Step 1: Delete records with id=0 from all tenant completion tables
DELETE FROM tenant_ucb_assignment_completions WHERE id = 0;
DELETE FROM tenant_upb_assignment_completions WHERE id = 0;
DELETE FROM tenant_gmail_assignment_completions WHERE id = 0;

-- Step 2: Delete records with id=0 from all tenant file tables
DELETE FROM tenant_ucb_assignment_files WHERE id = 0;
DELETE FROM tenant_upb_assignment_files WHERE id = 0;
DELETE FROM tenant_gmail_assignment_files WHERE id = 0;

-- Step 3: Alter tables to use BIGSERIAL for completion tables
ALTER TABLE tenant_ucb_assignment_completions ALTER COLUMN id TYPE BIGINT;
ALTER TABLE tenant_upb_assignment_completions ALTER COLUMN id TYPE BIGINT;
ALTER TABLE tenant_gmail_assignment_completions ALTER COLUMN id TYPE BIGINT;

-- Step 4: Alter tables to use BIGSERIAL for file tables (already BIGSERIAL if using schema_assignment_files.sql)
ALTER TABLE tenant_ucb_assignment_files ALTER COLUMN id TYPE BIGINT;
ALTER TABLE tenant_upb_assignment_files ALTER COLUMN id TYPE BIGINT;
ALTER TABLE tenant_gmail_assignment_files ALTER COLUMN id TYPE BIGINT;

-- Step 5: Reset sequences to start from 1 or next available number
SELECT setval('tenant_ucb_assignment_completions_id_seq', COALESCE((SELECT MAX(id) FROM tenant_ucb_assignment_completions), 0) + 1, false);
SELECT setval('tenant_upb_assignment_completions_id_seq', COALESCE((SELECT MAX(id) FROM tenant_upb_assignment_completions), 0) + 1, false);
SELECT setval('tenant_gmail_assignment_completions_id_seq', COALESCE((SELECT MAX(id) FROM tenant_gmail_assignment_completions), 0) + 1, false);

SELECT setval('tenant_ucb_assignment_files_id_seq', COALESCE((SELECT MAX(id) FROM tenant_ucb_assignment_files), 0) + 1, false);
SELECT setval('tenant_upb_assignment_files_id_seq', COALESCE((SELECT MAX(id) FROM tenant_upb_assignment_files), 0) + 1, false);
SELECT setval('tenant_gmail_assignment_files_id_seq', COALESCE((SELECT MAX(id) FROM tenant_gmail_assignment_files), 0) + 1, false);
