-- Add missing columns to Quotations table (from AddQuotationLineItemsAndRevisions migration)
ALTER TABLE "Quotations" ADD COLUMN IF NOT EXISTS "Version" text NOT NULL DEFAULT 'v1.0';
ALTER TABLE "Quotations" ADD COLUMN IF NOT EXISTS "ClientName" text;
ALTER TABLE "Quotations" ADD COLUMN IF NOT EXISTS "ProjectName" text;
ALTER TABLE "Quotations" ADD COLUMN IF NOT EXISTS "CreatedByUserName" text;
ALTER TABLE "Quotations" ADD COLUMN IF NOT EXISTS "Description" text;
ALTER TABLE "Quotations" ADD COLUMN IF NOT EXISTS "Attachments" text;

-- Create QuotationLineItems table if not exists
CREATE TABLE IF NOT EXISTS "QuotationLineItems" (
    "Id" uuid NOT NULL,
    "QuotationId" uuid NOT NULL,
    "SortOrder" integer NOT NULL,
    "PartNumber" text NOT NULL,
    "Description" text NOT NULL,
    "Quantity" integer NOT NULL,
    "UnitPrice" numeric NOT NULL,
    "TaxPercent" numeric NOT NULL,
    "AttachmentPath" text,
    CONSTRAINT "PK_QuotationLineItems" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_QuotationLineItems_Quotations_QuotationId" FOREIGN KEY ("QuotationId") REFERENCES "Quotations" ("Id") ON DELETE CASCADE
);
CREATE INDEX IF NOT EXISTS "IX_QuotationLineItems_QuotationId" ON "QuotationLineItems" ("QuotationId");

-- Create QuotationRevisions table if not exists
CREATE TABLE IF NOT EXISTS "QuotationRevisions" (
    "Id" uuid NOT NULL,
    "QuotationId" uuid NOT NULL,
    "Version" text NOT NULL,
    "Action" text NOT NULL,
    "ChangedBy" text NOT NULL,
    "ChangedAt" timestamp with time zone NOT NULL,
    "ChangeDetails" text,
    "AttachmentPath" text,
    "AttachmentFileName" text,
    CONSTRAINT "PK_QuotationRevisions" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_QuotationRevisions_Quotations_QuotationId" FOREIGN KEY ("QuotationId") REFERENCES "Quotations" ("Id") ON DELETE CASCADE
);
CREATE INDEX IF NOT EXISTS "IX_QuotationRevisions_QuotationId" ON "QuotationRevisions" ("QuotationId");

-- Record migration in EF history (so future migrations work correctly)
INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES ('20260220120000_AddQuotationLineItemsAndRevisions', '10.0.0')
ON CONFLICT ("MigrationId") DO NOTHING;
