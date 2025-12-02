-- ⚠️ COPY AND PASTE THIS ENTIRE FILE INTO SUPABASE SQL EDITOR
-- ⚠️ RUN ALL AT ONCE

-- Step 1: Delete records with id=0
DELETE FROM tenant_ucb_assignment_completions WHERE id = 0;
DELETE FROM tenant_upb_assignment_completions WHERE id = 0;
DELETE FROM tenant_gmail_assignment_completions WHERE id = 0;
DELETE FROM tenant_ucb_assignment_files WHERE id = 0;
DELETE FROM tenant_upb_assignment_files WHERE id = 0;
DELETE FROM tenant_gmail_assignment_files WHERE id = 0;

-- Step 2: Convert ID columns to BIGINT
ALTER TABLE tenant_ucb_assignment_completions ALTER COLUMN id TYPE BIGINT;
ALTER TABLE tenant_upb_assignment_completions ALTER COLUMN id TYPE BIGINT;
ALTER TABLE tenant_gmail_assignment_completions ALTER COLUMN id TYPE BIGINT;
ALTER TABLE tenant_ucb_assignment_files ALTER COLUMN id TYPE BIGINT;
ALTER TABLE tenant_upb_assignment_files ALTER COLUMN id TYPE BIGINT;
ALTER TABLE tenant_gmail_assignment_files ALTER COLUMN id TYPE BIGINT;

-- Step 3: Reset sequences
SELECT setval('tenant_ucb_assignment_completions_id_seq', COALESCE((SELECT MAX(id) FROM tenant_ucb_assignment_completions), 0) + 1, false);
SELECT setval('tenant_upb_assignment_completions_id_seq', COALESCE((SELECT MAX(id) FROM tenant_upb_assignment_completions), 0) + 1, false);
SELECT setval('tenant_gmail_assignment_completions_id_seq', COALESCE((SELECT MAX(id) FROM tenant_gmail_assignment_completions), 0) + 1, false);
SELECT setval('tenant_ucb_assignment_files_id_seq', COALESCE((SELECT MAX(id) FROM tenant_ucb_assignment_files), 0) + 1, false);
SELECT setval('tenant_upb_assignment_files_id_seq', COALESCE((SELECT MAX(id) FROM tenant_upb_assignment_files), 0) + 1, false);
SELECT setval('tenant_gmail_assignment_files_id_seq', COALESCE((SELECT MAX(id) FROM tenant_gmail_assignment_files), 0) + 1, false);
