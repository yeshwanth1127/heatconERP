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
UPDATE "Enquiries" SET "EnquiryNumber" = COALESCE("ReferenceNumber", '') WHERE "EnquiryNumber" IS NULL OR "EnquiryNumber" = '';
UPDATE "Enquiries" SET "CompanyName" = COALESCE("CustomerName", '') WHERE "CompanyName" IS NULL OR "CompanyName" = '';
UPDATE "Enquiries" SET "ProductDescription" = COALESCE("Subject", '') WHERE "ProductDescription" IS NULL;
UPDATE "Enquiries" SET "DateReceived" = "CreatedAt" WHERE "DateReceived" IS NULL;
UPDATE "Enquiries" SET "Source" = COALESCE("Source", 'Manual');
UPDATE "Enquiries" SET "Status" = CASE WHEN "Status" = 'Open' THEN 'Under Review' WHEN "Status" = 'Closed' THEN 'Closed' ELSE "Status" END;
UPDATE "Enquiries" SET "Priority" = COALESCE("Priority", 'Medium');
UPDATE "Enquiries" SET "FeasibilityStatus" = COALESCE("FeasibilityStatus", 'Pending');
UPDATE "Enquiries" SET "IsAerospace" = COALESCE("IsAerospace", false);
UPDATE "Enquiries" SET "IsDeleted" = COALESCE("IsDeleted", false);
