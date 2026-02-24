-- Expand Enquiry table with full V1 structure
-- Run with: psql -U postgres -d heatconerp -f scripts/expand-enquiry-schema.sql

-- Add new columns
ALTER TABLE "Enquiries" ADD COLUMN IF NOT EXISTS "EnquiryNumber" text;
ALTER TABLE "Enquiries" ADD COLUMN IF NOT EXISTS "DateReceived" timestamp with time zone;
ALTER TABLE "Enquiries" ADD COLUMN IF NOT EXISTS "Source" text DEFAULT 'Manual';
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

-- Migrate existing data (ReferenceNumber->EnquiryNumber, CustomerName->CompanyName, Subject->ProductDescription)
-- Only migrate if source columns exist
DO $$
BEGIN
    -- Migrate ReferenceNumber -> EnquiryNumber (if ReferenceNumber exists)
    IF EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name='Enquiries' AND column_name='ReferenceNumber') THEN
        UPDATE "Enquiries" SET "EnquiryNumber" = COALESCE("ReferenceNumber", '') WHERE "EnquiryNumber" IS NULL OR "EnquiryNumber" = '';
        RAISE NOTICE 'Migrated ReferenceNumber -> EnquiryNumber';
    END IF;
    
    -- Migrate CustomerName -> CompanyName (if CustomerName exists)
    IF EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name='Enquiries' AND column_name='CustomerName') THEN
        UPDATE "Enquiries" SET "CompanyName" = COALESCE("CustomerName", '') WHERE "CompanyName" IS NULL OR "CompanyName" = '';
        RAISE NOTICE 'Migrated CustomerName -> CompanyName';
    END IF;
    
    -- Migrate Subject -> ProductDescription (if Subject exists)
    IF EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name='Enquiries' AND column_name='Subject') THEN
        UPDATE "Enquiries" SET "ProductDescription" = COALESCE("Subject", '') WHERE "ProductDescription" IS NULL;
        RAISE NOTICE 'Migrated Subject -> ProductDescription';
    END IF;
END $$;

-- Set defaults for other fields
UPDATE "Enquiries" SET "DateReceived" = "CreatedAt" WHERE "DateReceived" IS NULL;
UPDATE "Enquiries" SET "Source" = COALESCE("Source", 'Manual') WHERE "Source" IS NULL;
UPDATE "Enquiries" SET "Status" = CASE WHEN "Status" = 'Open' THEN 'Under Review' WHEN "Status" = 'Closed' THEN 'Closed' ELSE "Status" END;
UPDATE "Enquiries" SET "Priority" = COALESCE("Priority", 'Medium') WHERE "Priority" IS NULL;
UPDATE "Enquiries" SET "FeasibilityStatus" = COALESCE("FeasibilityStatus", 'Pending') WHERE "FeasibilityStatus" IS NULL;
UPDATE "Enquiries" SET "IsAerospace" = COALESCE("IsAerospace", false) WHERE "IsAerospace" IS NULL;
UPDATE "Enquiries" SET "IsDeleted" = COALESCE("IsDeleted", false) WHERE "IsDeleted" IS NULL;
