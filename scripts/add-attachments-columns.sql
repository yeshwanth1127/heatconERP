ALTER TABLE "Quotations" ADD COLUMN IF NOT EXISTS "Description" text;
ALTER TABLE "Quotations" ADD COLUMN IF NOT EXISTS "Attachments" text;

-- Record migration in EF history (so future migrations work correctly)
INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES ('20260220130000_AddQuotationDescriptionAndAttachments', '10.0.0')
ON CONFLICT ("MigrationId") DO NOTHING;
