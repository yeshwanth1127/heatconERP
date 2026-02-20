-- Add snapshot columns to QuotationRevisions (for viewing past revision details)
ALTER TABLE "QuotationRevisions" ADD COLUMN IF NOT EXISTS "SnapshotClientName" text;
ALTER TABLE "QuotationRevisions" ADD COLUMN IF NOT EXISTS "SnapshotProjectName" text;
ALTER TABLE "QuotationRevisions" ADD COLUMN IF NOT EXISTS "SnapshotDescription" text;
ALTER TABLE "QuotationRevisions" ADD COLUMN IF NOT EXISTS "SnapshotAttachments" text;
ALTER TABLE "QuotationRevisions" ADD COLUMN IF NOT EXISTS "SnapshotStatus" text;
ALTER TABLE "QuotationRevisions" ADD COLUMN IF NOT EXISTS "SnapshotAmount" numeric;
ALTER TABLE "QuotationRevisions" ADD COLUMN IF NOT EXISTS "SnapshotLineItemsJson" text;

-- Record migration in EF history
INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES ('20260220140000_AddQuotationRevisionSnapshot', '10.0.0')
ON CONFLICT ("MigrationId") DO NOTHING;
