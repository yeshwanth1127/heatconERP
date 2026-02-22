-- Comprehensive migration fix script
-- Adds all missing columns/tables from migrations 20260221100000, 20260221110000, and 20260221120000
-- Run: psql -U postgres -d heatconerp -f scripts/fix-all-migrations.sql

-- Ensure EF migrations history table exists (so later scripts can insert records safely)
DO $$
BEGIN
    IF NOT EXISTS (SELECT 1 FROM information_schema.tables WHERE table_name = '__EFMigrationsHistory') THEN
        CREATE TABLE "__EFMigrationsHistory" (
            "MigrationId" character varying(150) NOT NULL,
            "ProductVersion" character varying(32) NOT NULL,
            CONSTRAINT "PK___EFMigrationsHistory" PRIMARY KEY ("MigrationId")
        );
        RAISE NOTICE 'Created __EFMigrationsHistory table';
    END IF;
END $$;

-- ============================================
-- Migration: 20260221100000_AddPurchaseOrderQuotationAndLineItems
-- ============================================

-- Add columns to PurchaseOrders
DO $$
BEGIN
    IF NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name = 'PurchaseOrders' AND column_name = 'QuotationId') THEN
        ALTER TABLE "PurchaseOrders" ADD COLUMN "QuotationId" uuid NULL;
        RAISE NOTICE 'Added QuotationId column';
    END IF;
    
    IF NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name = 'PurchaseOrders' AND column_name = 'CustomerPONumber') THEN
        ALTER TABLE "PurchaseOrders" ADD COLUMN "CustomerPONumber" text NOT NULL DEFAULT '';
        RAISE NOTICE 'Added CustomerPONumber column';
    END IF;
    
    IF NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name = 'PurchaseOrders' AND column_name = 'PODate') THEN
        ALTER TABLE "PurchaseOrders" ADD COLUMN "PODate" timestamp with time zone NOT NULL DEFAULT CURRENT_TIMESTAMP;
        RAISE NOTICE 'Added PODate column';
    END IF;
    
    IF NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name = 'PurchaseOrders' AND column_name = 'DeliveryTerms') THEN
        ALTER TABLE "PurchaseOrders" ADD COLUMN "DeliveryTerms" text NULL;
        RAISE NOTICE 'Added DeliveryTerms column';
    END IF;
    
    IF NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name = 'PurchaseOrders' AND column_name = 'PaymentTerms') THEN
        ALTER TABLE "PurchaseOrders" ADD COLUMN "PaymentTerms" text NULL;
        RAISE NOTICE 'Added PaymentTerms column';
    END IF;
END $$;

-- Create PurchaseOrderLineItems table
DO $$
BEGIN
    IF NOT EXISTS (SELECT 1 FROM information_schema.tables WHERE table_name = 'PurchaseOrderLineItems') THEN
        CREATE TABLE "PurchaseOrderLineItems" (
            "Id" uuid NOT NULL,
            "PurchaseOrderId" uuid NOT NULL,
            "SortOrder" integer NOT NULL,
            "PartNumber" text NOT NULL,
            "Description" text NOT NULL,
            "Quantity" integer NOT NULL,
            "UnitPrice" numeric NOT NULL,
            "TaxPercent" numeric NOT NULL,
            "AttachmentPath" text NULL,
            CONSTRAINT "PK_PurchaseOrderLineItems" PRIMARY KEY ("Id")
        );
        RAISE NOTICE 'Created PurchaseOrderLineItems table';
    END IF;
END $$;

-- Add indexes and foreign keys for PurchaseOrderLineItems
DO $$
BEGIN
    IF NOT EXISTS (SELECT 1 FROM pg_indexes WHERE indexname = 'IX_PurchaseOrderLineItems_PurchaseOrderId') THEN
        CREATE INDEX "IX_PurchaseOrderLineItems_PurchaseOrderId" ON "PurchaseOrderLineItems" ("PurchaseOrderId");
        RAISE NOTICE 'Created index IX_PurchaseOrderLineItems_PurchaseOrderId';
    END IF;
    
    IF NOT EXISTS (SELECT 1 FROM pg_indexes WHERE indexname = 'IX_PurchaseOrders_QuotationId') THEN
        CREATE INDEX "IX_PurchaseOrders_QuotationId" ON "PurchaseOrders" ("QuotationId");
        RAISE NOTICE 'Created index IX_PurchaseOrders_QuotationId';
    END IF;
    
    IF NOT EXISTS (SELECT 1 FROM information_schema.table_constraints WHERE constraint_name = 'FK_PurchaseOrderLineItems_PurchaseOrders_PurchaseOrderId') THEN
        ALTER TABLE "PurchaseOrderLineItems"
        ADD CONSTRAINT "FK_PurchaseOrderLineItems_PurchaseOrders_PurchaseOrderId"
        FOREIGN KEY ("PurchaseOrderId") REFERENCES "PurchaseOrders" ("Id") ON DELETE CASCADE;
        RAISE NOTICE 'Added FK_PurchaseOrderLineItems_PurchaseOrders_PurchaseOrderId';
    END IF;
    
    IF NOT EXISTS (SELECT 1 FROM information_schema.table_constraints WHERE constraint_name = 'FK_PurchaseOrders_Quotations_QuotationId') THEN
        ALTER TABLE "PurchaseOrders"
        ADD CONSTRAINT "FK_PurchaseOrders_Quotations_QuotationId"
        FOREIGN KEY ("QuotationId") REFERENCES "Quotations" ("Id") ON DELETE SET NULL;
        RAISE NOTICE 'Added FK_PurchaseOrders_Quotations_QuotationId';
    END IF;
END $$;

-- ============================================
-- Migration: 20260221110000_AddPurchaseInvoices
-- ============================================

-- Create PurchaseInvoices table if it doesn't exist
DO $$
BEGIN
    IF NOT EXISTS (SELECT 1 FROM information_schema.tables WHERE table_name = 'PurchaseInvoices') THEN
        CREATE TABLE "PurchaseInvoices" (
            "Id" uuid NOT NULL,
            "InvoiceNumber" text NOT NULL,
            "PurchaseOrderId" uuid NOT NULL,
            "Status" text NOT NULL,
            "InvoiceDate" timestamp with time zone NOT NULL,
            "DueDate" timestamp with time zone NULL,
            "TotalAmount" numeric NOT NULL,
            "CreatedAt" timestamp with time zone NOT NULL,
            "CreatedByUserName" text NOT NULL,
            CONSTRAINT "PK_PurchaseInvoices" PRIMARY KEY ("Id")
        );
        RAISE NOTICE 'Created PurchaseInvoices table';
    ELSE
        RAISE NOTICE 'PurchaseInvoices table already exists';
    END IF;
END $$;

-- Create PurchaseInvoiceLineItems table if it doesn't exist
DO $$
BEGIN
    IF NOT EXISTS (SELECT 1 FROM information_schema.tables WHERE table_name = 'PurchaseInvoiceLineItems') THEN
        CREATE TABLE "PurchaseInvoiceLineItems" (
            "Id" uuid NOT NULL,
            "PurchaseInvoiceId" uuid NOT NULL,
            "SortOrder" integer NOT NULL,
            "PartNumber" text NOT NULL,
            "Description" text NOT NULL,
            "Quantity" integer NOT NULL,
            "UnitPrice" numeric NOT NULL,
            "TaxPercent" numeric NOT NULL,
            "LineTotal" numeric NULL,
            CONSTRAINT "PK_PurchaseInvoiceLineItems" PRIMARY KEY ("Id")
        );
        RAISE NOTICE 'Created PurchaseInvoiceLineItems table';
    ELSE
        RAISE NOTICE 'PurchaseInvoiceLineItems table already exists';
    END IF;
END $$;

-- Add indexes and foreign keys for PurchaseInvoices
DO $$
BEGIN
    IF NOT EXISTS (SELECT 1 FROM pg_indexes WHERE indexname = 'IX_PurchaseInvoiceLineItems_PurchaseInvoiceId') THEN
        CREATE INDEX "IX_PurchaseInvoiceLineItems_PurchaseInvoiceId" ON "PurchaseInvoiceLineItems" ("PurchaseInvoiceId");
        RAISE NOTICE 'Created index IX_PurchaseInvoiceLineItems_PurchaseInvoiceId';
    END IF;
    
    IF NOT EXISTS (SELECT 1 FROM pg_indexes WHERE indexname = 'IX_PurchaseInvoices_PurchaseOrderId') THEN
        CREATE INDEX "IX_PurchaseInvoices_PurchaseOrderId" ON "PurchaseInvoices" ("PurchaseOrderId");
        RAISE NOTICE 'Created index IX_PurchaseInvoices_PurchaseOrderId';
    END IF;
    
    IF NOT EXISTS (SELECT 1 FROM information_schema.table_constraints WHERE constraint_name = 'FK_PurchaseInvoices_PurchaseOrders_PurchaseOrderId') THEN
        ALTER TABLE "PurchaseInvoices"
        ADD CONSTRAINT "FK_PurchaseInvoices_PurchaseOrders_PurchaseOrderId"
        FOREIGN KEY ("PurchaseOrderId") REFERENCES "PurchaseOrders" ("Id") ON DELETE RESTRICT;
        RAISE NOTICE 'Added FK_PurchaseInvoices_PurchaseOrders_PurchaseOrderId';
    END IF;
    
    IF NOT EXISTS (SELECT 1 FROM information_schema.table_constraints WHERE constraint_name = 'FK_PurchaseInvoiceLineItems_PurchaseInvoices_PurchaseInvoiceId') THEN
        ALTER TABLE "PurchaseInvoiceLineItems"
        ADD CONSTRAINT "FK_PurchaseInvoiceLineItems_PurchaseInvoices_PurchaseInvoiceId"
        FOREIGN KEY ("PurchaseInvoiceId") REFERENCES "PurchaseInvoices" ("Id") ON DELETE CASCADE;
        RAISE NOTICE 'Added FK_PurchaseInvoiceLineItems_PurchaseInvoices_PurchaseInvoiceId';
    END IF;
END $$;

-- ============================================
-- Migration: 20260221120000_AddSentToCustomerAndPoRevisionLink
-- ============================================

-- Add SentToCustomerAt to QuotationRevisions
DO $$
BEGIN
    IF NOT EXISTS (
        SELECT 1 FROM information_schema.columns 
        WHERE table_name = 'QuotationRevisions' AND column_name = 'SentToCustomerAt'
    ) THEN
        ALTER TABLE "QuotationRevisions" ADD COLUMN "SentToCustomerAt" timestamp with time zone NULL;
        RAISE NOTICE 'Added SentToCustomerAt column';
    END IF;
END $$;

-- Add SentToCustomerBy to QuotationRevisions
DO $$
BEGIN
    IF NOT EXISTS (
        SELECT 1 FROM information_schema.columns 
        WHERE table_name = 'QuotationRevisions' AND column_name = 'SentToCustomerBy'
    ) THEN
        ALTER TABLE "QuotationRevisions" ADD COLUMN "SentToCustomerBy" text NULL;
        RAISE NOTICE 'Added SentToCustomerBy column';
    END IF;
END $$;

-- Add QuotationRevisionId to PurchaseOrders
DO $$
BEGIN
    IF NOT EXISTS (
        SELECT 1 FROM information_schema.columns 
        WHERE table_name = 'PurchaseOrders' AND column_name = 'QuotationRevisionId'
    ) THEN
        ALTER TABLE "PurchaseOrders" ADD COLUMN "QuotationRevisionId" uuid NULL;
        RAISE NOTICE 'Added QuotationRevisionId column';
    END IF;
END $$;

-- Create index for QuotationRevisionId
DO $$
BEGIN
    IF NOT EXISTS (SELECT 1 FROM pg_indexes WHERE indexname = 'IX_PurchaseOrders_QuotationRevisionId') THEN
        CREATE INDEX "IX_PurchaseOrders_QuotationRevisionId" ON "PurchaseOrders" ("QuotationRevisionId");
        RAISE NOTICE 'Created index IX_PurchaseOrders_QuotationRevisionId';
    END IF;
END $$;

-- Add foreign key for QuotationRevisionId
DO $$
BEGIN
    IF NOT EXISTS (
        SELECT 1 FROM information_schema.table_constraints 
        WHERE constraint_name = 'FK_PurchaseOrders_QuotationRevisions_QuotationRevisionId'
    ) THEN
        ALTER TABLE "PurchaseOrders"
        ADD CONSTRAINT "FK_PurchaseOrders_QuotationRevisions_QuotationRevisionId"
        FOREIGN KEY ("QuotationRevisionId") REFERENCES "QuotationRevisions" ("Id") ON DELETE SET NULL;
        RAISE NOTICE 'Added FK_PurchaseOrders_QuotationRevisions_QuotationRevisionId';
    END IF;
END $$;

-- ============================================
-- Update migration history
-- ============================================

-- ============================================
-- Migration: 20260221130000_AddWorkOrderPurchaseInvoiceLink
-- ============================================

-- Add PurchaseInvoiceId to WorkOrders and FK/index
DO $$
BEGIN
    IF NOT EXISTS (
        SELECT 1 FROM information_schema.columns
        WHERE table_name = 'WorkOrders' AND column_name = 'PurchaseInvoiceId'
    ) THEN
        ALTER TABLE "WorkOrders" ADD COLUMN "PurchaseInvoiceId" uuid NULL;
        RAISE NOTICE 'Added PurchaseInvoiceId column to WorkOrders';
    END IF;

    IF NOT EXISTS (SELECT 1 FROM pg_indexes WHERE indexname = 'IX_WorkOrders_PurchaseInvoiceId') THEN
        CREATE INDEX "IX_WorkOrders_PurchaseInvoiceId" ON "WorkOrders" ("PurchaseInvoiceId");
        RAISE NOTICE 'Created index IX_WorkOrders_PurchaseInvoiceId';
    END IF;

    IF NOT EXISTS (
        SELECT 1 FROM information_schema.table_constraints
        WHERE constraint_name = 'FK_WorkOrders_PurchaseInvoices_PurchaseInvoiceId'
    ) THEN
        ALTER TABLE "WorkOrders"
        ADD CONSTRAINT "FK_WorkOrders_PurchaseInvoices_PurchaseInvoiceId"
        FOREIGN KEY ("PurchaseInvoiceId") REFERENCES "PurchaseInvoices" ("Id") ON DELETE SET NULL;
        RAISE NOTICE 'Added FK_WorkOrders_PurchaseInvoices_PurchaseInvoiceId';
    END IF;
END $$;

-- ============================================
-- Migration: 20260221150000_AddWorkOrderProductionDispatch
-- ============================================
-- Add dispatch/receive columns to WorkOrders
DO $$
BEGIN
    IF NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name = 'WorkOrders' AND column_name = 'SentToProductionAt') THEN
        ALTER TABLE "WorkOrders" ADD COLUMN "SentToProductionAt" timestamp with time zone NULL;
        RAISE NOTICE 'Added SentToProductionAt column to WorkOrders';
    END IF;
    IF NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name = 'WorkOrders' AND column_name = 'SentToProductionBy') THEN
        ALTER TABLE "WorkOrders" ADD COLUMN "SentToProductionBy" text NULL;
        RAISE NOTICE 'Added SentToProductionBy column to WorkOrders';
    END IF;
    IF NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name = 'WorkOrders' AND column_name = 'ProductionReceivedAt') THEN
        ALTER TABLE "WorkOrders" ADD COLUMN "ProductionReceivedAt" timestamp with time zone NULL;
        RAISE NOTICE 'Added ProductionReceivedAt column to WorkOrders';
    END IF;
    IF NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name = 'WorkOrders' AND column_name = 'ProductionReceivedBy') THEN
        ALTER TABLE "WorkOrders" ADD COLUMN "ProductionReceivedBy" text NULL;
        RAISE NOTICE 'Added ProductionReceivedBy column to WorkOrders';
    END IF;
END $$;

-- Record migration in EF history (best-effort; avoids EF thinking it's missing)
INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES ('20260221150000_AddWorkOrderProductionDispatch', '10.0.0')
ON CONFLICT ("MigrationId") DO NOTHING;
-- ============================================
-- Migration: 20260221140000_AddWorkOrderLineItemsAndAssignment
-- ============================================

-- Add AssignedToUserName to WorkOrders
DO $$
BEGIN
    IF NOT EXISTS (
        SELECT 1 FROM information_schema.columns
        WHERE table_name = 'WorkOrders' AND column_name = 'AssignedToUserName'
    ) THEN
        ALTER TABLE "WorkOrders" ADD COLUMN "AssignedToUserName" text NULL;
        RAISE NOTICE 'Added AssignedToUserName column to WorkOrders';
    END IF;
END $$;

-- Create WorkOrderLineItems table
DO $$
BEGIN
    IF NOT EXISTS (SELECT 1 FROM information_schema.tables WHERE table_name = 'WorkOrderLineItems') THEN
        CREATE TABLE "WorkOrderLineItems" (
            "Id" uuid NOT NULL,
            "WorkOrderId" uuid NOT NULL,
            "SortOrder" integer NOT NULL,
            "PartNumber" text NOT NULL,
            "Description" text NOT NULL,
            "Quantity" integer NOT NULL,
            "UnitPrice" numeric NOT NULL,
            "TaxPercent" numeric NOT NULL,
            "LineTotal" numeric NULL,
            CONSTRAINT "PK_WorkOrderLineItems" PRIMARY KEY ("Id")
        );
        RAISE NOTICE 'Created WorkOrderLineItems table';
    END IF;
END $$;

-- Add indexes and foreign keys for WorkOrderLineItems
DO $$
BEGIN
    IF NOT EXISTS (SELECT 1 FROM pg_indexes WHERE indexname = 'IX_WorkOrderLineItems_WorkOrderId') THEN
        CREATE INDEX "IX_WorkOrderLineItems_WorkOrderId" ON "WorkOrderLineItems" ("WorkOrderId");
        RAISE NOTICE 'Created index IX_WorkOrderLineItems_WorkOrderId';
    END IF;

    IF NOT EXISTS (SELECT 1 FROM information_schema.table_constraints WHERE constraint_name = 'FK_WorkOrderLineItems_WorkOrders_WorkOrderId') THEN
        ALTER TABLE "WorkOrderLineItems"
        ADD CONSTRAINT "FK_WorkOrderLineItems_WorkOrders_WorkOrderId"
        FOREIGN KEY ("WorkOrderId") REFERENCES "WorkOrders" ("Id") ON DELETE CASCADE;
        RAISE NOTICE 'Added FK_WorkOrderLineItems_WorkOrders_WorkOrderId';
    END IF;
END $$;

-- Ensure migration history records exist
DO $$
BEGIN
    IF NOT EXISTS (SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260221100000_AddPurchaseOrderQuotationAndLineItems') THEN
        INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
        VALUES ('20260221100000_AddPurchaseOrderQuotationAndLineItems', '10.0.0');
        RAISE NOTICE 'Added migration record: 20260221100000_AddPurchaseOrderQuotationAndLineItems';
    END IF;
    
    IF NOT EXISTS (SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260221110000_AddPurchaseInvoices') THEN
        INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
        VALUES ('20260221110000_AddPurchaseInvoices', '10.0.0');
        RAISE NOTICE 'Added migration record: 20260221110000_AddPurchaseInvoices';
    END IF;
    
    IF NOT EXISTS (SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260221120000_AddSentToCustomerAndPoRevisionLink') THEN
        INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
        VALUES ('20260221120000_AddSentToCustomerAndPoRevisionLink', '10.0.0');
        RAISE NOTICE 'Added migration record: 20260221120000_AddSentToCustomerAndPoRevisionLink';
    END IF;

    IF NOT EXISTS (SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260221130000_AddWorkOrderPurchaseInvoiceLink') THEN
        INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
        VALUES ('20260221130000_AddWorkOrderPurchaseInvoiceLink', '10.0.0');
        RAISE NOTICE 'Added migration record: 20260221130000_AddWorkOrderPurchaseInvoiceLink';
    END IF;

    IF NOT EXISTS (SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260221140000_AddWorkOrderLineItemsAndAssignment') THEN
        INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
        VALUES ('20260221140000_AddWorkOrderLineItemsAndAssignment', '10.0.0');
        RAISE NOTICE 'Added migration record: 20260221140000_AddWorkOrderLineItemsAndAssignment';
    END IF;
END $$;

-- ============================================
-- Migration: 20260222_Phase1QualityGatesAndNcr (manual)
-- ============================================
-- This section is idempotent and can be safely re-run.
-- It creates WorkOrderQualityGates, WorkOrderQualityChecks (append-only), and Ncrs.
-- It also migrates/drops legacy QualityInspections.

\i scripts/migrate-quality-gates.sql

SELECT 'All migrations applied successfully!' AS result;
