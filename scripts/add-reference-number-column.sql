-- Fix missing schema: ReferenceNumber + QualityInspections
-- Run with: psql -U postgres -d heatconerp -f scripts/add-reference-number-column.sql

-- Add ReferenceNumber to Enquiries
ALTER TABLE "Enquiries" ADD COLUMN IF NOT EXISTS "ReferenceNumber" text NOT NULL DEFAULT '';

-- Create QualityInspections table if missing
CREATE TABLE IF NOT EXISTS "QualityInspections" (
    "Id" uuid NOT NULL,
    "WorkOrderId" uuid NOT NULL,
    "WorkOrderNumber" text NOT NULL,
    "Result" text NOT NULL,
    "Notes" text,
    "InspectedAt" timestamp with time zone NOT NULL,
    "InspectedBy" text NOT NULL,
    CONSTRAINT "PK_QualityInspections" PRIMARY KEY ("Id")
);
