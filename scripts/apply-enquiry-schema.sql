-- Enquiry V1 schema - run with: .\scripts\apply-enquiry-schema.ps1
-- Or: psql -U postgres -d heatconerp -f scripts/apply-enquiry-schema.sql

-- Add new columns
ALTER TABLE "Enquiries" ADD COLUMN IF NOT EXISTS "EnquiryNumber" text;
ALTER TABLE "Enquiries" ADD COLUMN IF NOT EXISTS "DateReceived" timestamp with time zone;
ALTER TABLE "Enquiries" ADD COLUMN IF NOT EXISTS "Source" text;
ALTER TABLE "Enquiries" ADD COLUMN IF NOT EXISTS "AssignedTo" text;
ALTER TABLE "Enquiries" ADD COLUMN IF NOT EXISTS "CompanyName" text;
ALTER TABLE "Enquiries" ADD COLUMN IF NOT EXISTS "ContactPerson" text;
ALTER TABLE "Enquiries" ADD COLUMN IF NOT EXISTS "Email" text;
ALTER TABLE "Enquiries" ADD COLUMN IF NOT EXISTS "Phone" text;
ALTER TABLE "Enquiries" ADD COLUMN IF NOT EXISTS "Gst" text;
ALTER TABLE "Enquiries" ADD COLUMN IF NOT EXISTS "Address" text;
ALTER TABLE "Enquiries" ADD COLUMN IF NOT EXISTS "ProductDescription" text;
ALTER TABLE "Enquiries" ADD COLUMN IF NOT EXISTS "Quantity" integer;
ALTER TABLE "Enquiries" ADD COLUMN IF NOT EXISTS "ExpectedDeliveryDate" timestamp with time zone;
ALTER TABLE "Enquiries" ADD COLUMN IF NOT EXISTS "AttachmentPath" text;
ALTER TABLE "Enquiries" ADD COLUMN IF NOT EXISTS "IsAerospace" boolean DEFAULT false;
ALTER TABLE "Enquiries" ADD COLUMN IF NOT EXISTS "Priority" text DEFAULT 'Medium';
ALTER TABLE "Enquiries" ADD COLUMN IF NOT EXISTS "FeasibilityStatus" text DEFAULT 'Pending';
ALTER TABLE "Enquiries" ADD COLUMN IF NOT EXISTS "FeasibilityNotes" text;
ALTER TABLE "Enquiries" ADD COLUMN IF NOT EXISTS "CreatedBy" text;
ALTER TABLE "Enquiries" ADD COLUMN IF NOT EXISTS "UpdatedAt" timestamp with time zone;
ALTER TABLE "Enquiries" ADD COLUMN IF NOT EXISTS "IsDeleted" boolean DEFAULT false;

-- Migrate data from old columns (ReferenceNumber, CustomerName, Subject)
UPDATE "Enquiries" SET "EnquiryNumber" = COALESCE("ReferenceNumber", '') WHERE "EnquiryNumber" IS NULL OR "EnquiryNumber" = '';
UPDATE "Enquiries" SET "CompanyName" = COALESCE("CustomerName", '') WHERE "CompanyName" IS NULL OR "CompanyName" = '';
UPDATE "Enquiries" SET "ProductDescription" = COALESCE("Subject", '') WHERE "ProductDescription" IS NULL;
UPDATE "Enquiries" SET "DateReceived" = "CreatedAt" WHERE "DateReceived" IS NULL;
UPDATE "Enquiries" SET "Source" = COALESCE("Source", 'Manual') WHERE "Source" IS NULL;
UPDATE "Enquiries" SET "Status" = CASE WHEN "Status" = 'Open' THEN 'Under Review' WHEN "Status" = 'Closed' THEN 'Closed' ELSE COALESCE("Status", 'New') END;
UPDATE "Enquiries" SET "Priority" = COALESCE("Priority", 'Medium') WHERE "Priority" IS NULL;
UPDATE "Enquiries" SET "FeasibilityStatus" = COALESCE("FeasibilityStatus", 'Pending') WHERE "FeasibilityStatus" IS NULL;
UPDATE "Enquiries" SET "EnquiryNumber" = '' WHERE "EnquiryNumber" IS NULL;
UPDATE "Enquiries" SET "CompanyName" = '' WHERE "CompanyName" IS NULL;
UPDATE "Enquiries" SET "DateReceived" = "CreatedAt" WHERE "DateReceived" IS NULL;

-- Set NOT NULL and defaults
ALTER TABLE "Enquiries" ALTER COLUMN "EnquiryNumber" SET NOT NULL;
ALTER TABLE "Enquiries" ALTER COLUMN "EnquiryNumber" SET DEFAULT '';
ALTER TABLE "Enquiries" ALTER COLUMN "DateReceived" SET NOT NULL;
ALTER TABLE "Enquiries" ALTER COLUMN "Source" SET NOT NULL;
ALTER TABLE "Enquiries" ALTER COLUMN "Source" SET DEFAULT 'Manual';
ALTER TABLE "Enquiries" ALTER COLUMN "CompanyName" SET NOT NULL;
ALTER TABLE "Enquiries" ALTER COLUMN "CompanyName" SET DEFAULT '';
ALTER TABLE "Enquiries" ALTER COLUMN "Priority" SET NOT NULL;
ALTER TABLE "Enquiries" ALTER COLUMN "Priority" SET DEFAULT 'Medium';
ALTER TABLE "Enquiries" ALTER COLUMN "FeasibilityStatus" SET NOT NULL;
ALTER TABLE "Enquiries" ALTER COLUMN "FeasibilityStatus" SET DEFAULT 'Pending';

-- Drop old columns (only if they exist)
DO $$
BEGIN
    IF EXISTS (SELECT 1 FROM information_schema.columns WHERE table_schema = 'public' AND LOWER(table_name) = 'enquiries' AND LOWER(column_name) = 'referencenumber') THEN
        ALTER TABLE "Enquiries" DROP COLUMN "ReferenceNumber";
    END IF;
    IF EXISTS (SELECT 1 FROM information_schema.columns WHERE table_schema = 'public' AND LOWER(table_name) = 'enquiries' AND LOWER(column_name) = 'customername') THEN
        ALTER TABLE "Enquiries" DROP COLUMN "CustomerName";
    END IF;
    IF EXISTS (SELECT 1 FROM information_schema.columns WHERE table_schema = 'public' AND LOWER(table_name) = 'enquiries' AND LOWER(column_name) = 'subject') THEN
        ALTER TABLE "Enquiries" DROP COLUMN "Subject";
    END IF;
    IF EXISTS (SELECT 1 FROM information_schema.columns WHERE table_schema = 'public' AND LOWER(table_name) = 'enquiries' AND LOWER(column_name) = 'assignedtouserid') THEN
        ALTER TABLE "Enquiries" DROP COLUMN "AssignedToUserId";
    END IF;
END $$;
