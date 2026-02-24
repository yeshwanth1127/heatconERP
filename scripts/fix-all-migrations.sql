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
-- Migration: 20260221170000_AddInventoryProcurementModule
-- ============================================

-- Create MaterialCategories table
DO $$
BEGIN
    IF NOT EXISTS (SELECT 1 FROM information_schema.tables WHERE table_name = 'MaterialCategories') THEN
        CREATE TABLE "MaterialCategories" (
            "Id" uuid NOT NULL,
            "Name" text NOT NULL,
            "Description" text NULL,
            "CreatedAt" timestamp with time zone NOT NULL,
            "UpdatedAt" timestamp with time zone NULL,
            "IsDeleted" boolean NOT NULL DEFAULT false,
            "RowVersion" bytea NOT NULL DEFAULT '\x',
            CONSTRAINT "PK_MaterialCategories" PRIMARY KEY ("Id")
        );
        RAISE NOTICE 'Created MaterialCategories table';
    END IF;
END $$;

-- Create MaterialVariants table
DO $$
BEGIN
    IF NOT EXISTS (SELECT 1 FROM information_schema.tables WHERE table_name = 'MaterialVariants') THEN
        CREATE TABLE "MaterialVariants" (
            "Id" uuid NOT NULL,
            "MaterialCategoryId" uuid NOT NULL,
            "Grade" text NOT NULL DEFAULT '',
            "Size" text NOT NULL DEFAULT '',
            "Unit" text NOT NULL,
            "SKU" text NOT NULL,
            "MinimumStockLevel" numeric NOT NULL,
            "CreatedAt" timestamp with time zone NOT NULL,
            "UpdatedAt" timestamp with time zone NULL,
            "IsDeleted" boolean NOT NULL DEFAULT false,
            "RowVersion" bytea NOT NULL DEFAULT '\x',
            CONSTRAINT "PK_MaterialVariants" PRIMARY KEY ("Id"),
            CONSTRAINT "FK_MaterialVariants_MaterialCategories_MaterialCategoryId"
                FOREIGN KEY ("MaterialCategoryId") REFERENCES "MaterialCategories" ("Id") ON DELETE RESTRICT
        );
        RAISE NOTICE 'Created MaterialVariants table';
    END IF;
END $$;

-- Create indexes for MaterialVariants
DO $$
BEGIN
    IF NOT EXISTS (SELECT 1 FROM pg_indexes WHERE indexname = 'IX_MaterialVariants_MaterialCategoryId') THEN
        CREATE INDEX "IX_MaterialVariants_MaterialCategoryId" ON "MaterialVariants" ("MaterialCategoryId");
        RAISE NOTICE 'Created index IX_MaterialVariants_MaterialCategoryId';
    END IF;
    
    IF NOT EXISTS (SELECT 1 FROM pg_indexes WHERE indexname = 'IX_MaterialVariants_SKU') THEN
        CREATE UNIQUE INDEX "IX_MaterialVariants_SKU" ON "MaterialVariants" ("SKU");
        RAISE NOTICE 'Created index IX_MaterialVariants_SKU';
    END IF;
END $$;

-- Create Vendors table
DO $$
BEGIN
    IF NOT EXISTS (SELECT 1 FROM information_schema.tables WHERE table_name = 'Vendors') THEN
        CREATE TABLE "Vendors" (
            "Id" uuid NOT NULL,
            "Name" text NOT NULL,
            "GSTNumber" text NULL,
            "ContactDetails" text NULL,
            "IsApprovedVendor" boolean NOT NULL,
            "CreatedAt" timestamp with time zone NOT NULL,
            "UpdatedAt" timestamp with time zone NULL,
            "IsDeleted" boolean NOT NULL DEFAULT false,
            "RowVersion" bytea NOT NULL DEFAULT '\x',
            CONSTRAINT "PK_Vendors" PRIMARY KEY ("Id")
        );
        RAISE NOTICE 'Created Vendors table';
    END IF;
END $$;

-- Create VendorPurchaseOrders table
DO $$
BEGIN
    IF NOT EXISTS (SELECT 1 FROM information_schema.tables WHERE table_name = 'VendorPurchaseOrders') THEN
        CREATE TABLE "VendorPurchaseOrders" (
            "Id" uuid NOT NULL,
            "VendorId" uuid NOT NULL,
            "OrderDate" timestamp with time zone NOT NULL,
            "Status" text NOT NULL,
            "CreatedAt" timestamp with time zone NOT NULL,
            "UpdatedAt" timestamp with time zone NULL,
            "IsDeleted" boolean NOT NULL DEFAULT false,
            "RowVersion" bytea NOT NULL DEFAULT '\x',
            CONSTRAINT "PK_VendorPurchaseOrders" PRIMARY KEY ("Id"),
            CONSTRAINT "FK_VendorPurchaseOrders_Vendors_VendorId"
                FOREIGN KEY ("VendorId") REFERENCES "Vendors" ("Id") ON DELETE RESTRICT
        );
        RAISE NOTICE 'Created VendorPurchaseOrders table';
    END IF;
END $$;

-- Create VendorPurchaseOrderLineItems table
DO $$
BEGIN
    IF NOT EXISTS (SELECT 1 FROM information_schema.tables WHERE table_name = 'VendorPurchaseOrderLineItems') THEN
        CREATE TABLE "VendorPurchaseOrderLineItems" (
            "Id" uuid NOT NULL,
            "VendorPurchaseOrderId" uuid NOT NULL,
            "MaterialVariantId" uuid NOT NULL,
            "OrderedQuantity" numeric NOT NULL,
            "UnitPrice" numeric NOT NULL,
            "CreatedAt" timestamp with time zone NOT NULL,
            "UpdatedAt" timestamp with time zone NULL,
            "IsDeleted" boolean NOT NULL DEFAULT false,
            "RowVersion" bytea NOT NULL DEFAULT '\x',
            CONSTRAINT "PK_VendorPurchaseOrderLineItems" PRIMARY KEY ("Id"),
            CONSTRAINT "FK_VendorPurchaseOrderLineItems_VendorPurchaseOrders_VendorPurchaseOrderId"
                FOREIGN KEY ("VendorPurchaseOrderId") REFERENCES "VendorPurchaseOrders" ("Id") ON DELETE RESTRICT,
            CONSTRAINT "FK_VendorPurchaseOrderLineItems_MaterialVariants_MaterialVariantId"
                FOREIGN KEY ("MaterialVariantId") REFERENCES "MaterialVariants" ("Id") ON DELETE RESTRICT
        );
        RAISE NOTICE 'Created VendorPurchaseOrderLineItems table';
    END IF;
END $$;

-- Create indexes for VendorPurchaseOrders and VendorPurchaseOrderLineItems
DO $$
BEGIN
    IF NOT EXISTS (SELECT 1 FROM pg_indexes WHERE indexname = 'IX_VendorPurchaseOrders_VendorId') THEN
        CREATE INDEX "IX_VendorPurchaseOrders_VendorId" ON "VendorPurchaseOrders" ("VendorId");
        RAISE NOTICE 'Created index IX_VendorPurchaseOrders_VendorId';
    END IF;
    
    IF NOT EXISTS (SELECT 1 FROM pg_indexes WHERE indexname = 'IX_VendorPurchaseOrderLineItems_VendorPurchaseOrderId') THEN
        CREATE INDEX "IX_VendorPurchaseOrderLineItems_VendorPurchaseOrderId" ON "VendorPurchaseOrderLineItems" ("VendorPurchaseOrderId");
        RAISE NOTICE 'Created index IX_VendorPurchaseOrderLineItems_VendorPurchaseOrderId';
    END IF;
    
    IF NOT EXISTS (SELECT 1 FROM pg_indexes WHERE indexname = 'IX_VendorPurchaseOrderLineItems_MaterialVariantId') THEN
        CREATE INDEX "IX_VendorPurchaseOrderLineItems_MaterialVariantId" ON "VendorPurchaseOrderLineItems" ("MaterialVariantId");
        RAISE NOTICE 'Created index IX_VendorPurchaseOrderLineItems_MaterialVariantId';
    END IF;
END $$;

-- Create GRNs table
DO $$
BEGIN
    IF NOT EXISTS (SELECT 1 FROM information_schema.tables WHERE table_name = 'GRNs') THEN
        CREATE TABLE "GRNs" (
            "Id" uuid NOT NULL,
            "VendorPurchaseOrderId" uuid NOT NULL,
            "ReceivedDate" timestamp with time zone NOT NULL,
            "InvoiceNumber" text NOT NULL,
            "CreatedAt" timestamp with time zone NOT NULL,
            "UpdatedAt" timestamp with time zone NULL,
            "IsDeleted" boolean NOT NULL DEFAULT false,
            "RowVersion" bytea NOT NULL DEFAULT '\x',
            CONSTRAINT "PK_GRNs" PRIMARY KEY ("Id"),
            CONSTRAINT "FK_GRNs_VendorPurchaseOrders_VendorPurchaseOrderId"
                FOREIGN KEY ("VendorPurchaseOrderId") REFERENCES "VendorPurchaseOrders" ("Id") ON DELETE RESTRICT
        );
        RAISE NOTICE 'Created GRNs table';
    END IF;
END $$;

-- Create GRNLineItems table
DO $$
BEGIN
    IF NOT EXISTS (SELECT 1 FROM information_schema.tables WHERE table_name = 'GRNLineItems') THEN
        CREATE TABLE "GRNLineItems" (
            "Id" uuid NOT NULL,
            "GRNId" uuid NOT NULL,
            "MaterialVariantId" uuid NOT NULL,
            "BatchNumber" text NOT NULL,
            "QuantityReceived" numeric NOT NULL,
            "UnitPrice" numeric NOT NULL,
            "QualityStatus" text NOT NULL,
            "CreatedAt" timestamp with time zone NOT NULL,
            "UpdatedAt" timestamp with time zone NULL,
            "IsDeleted" boolean NOT NULL DEFAULT false,
            "RowVersion" bytea NOT NULL DEFAULT '\x',
            CONSTRAINT "PK_GRNLineItems" PRIMARY KEY ("Id"),
            CONSTRAINT "FK_GRNLineItems_GRNs_GRNId"
                FOREIGN KEY ("GRNId") REFERENCES "GRNs" ("Id") ON DELETE RESTRICT,
            CONSTRAINT "FK_GRNLineItems_MaterialVariants_MaterialVariantId"
                FOREIGN KEY ("MaterialVariantId") REFERENCES "MaterialVariants" ("Id") ON DELETE RESTRICT
        );
        RAISE NOTICE 'Created GRNLineItems table';
    END IF;
END $$;

-- Create indexes for GRNs and GRNLineItems
DO $$
BEGIN
    IF NOT EXISTS (SELECT 1 FROM pg_indexes WHERE indexname = 'IX_GRNs_VendorPurchaseOrderId') THEN
        CREATE INDEX "IX_GRNs_VendorPurchaseOrderId" ON "GRNs" ("VendorPurchaseOrderId");
        RAISE NOTICE 'Created index IX_GRNs_VendorPurchaseOrderId';
    END IF;
    
    IF NOT EXISTS (SELECT 1 FROM pg_indexes WHERE indexname = 'IX_GRNLineItems_GRNId') THEN
        CREATE INDEX "IX_GRNLineItems_GRNId" ON "GRNLineItems" ("GRNId");
        RAISE NOTICE 'Created index IX_GRNLineItems_GRNId';
    END IF;
    
    IF NOT EXISTS (SELECT 1 FROM pg_indexes WHERE indexname = 'IX_GRNLineItems_MaterialVariantId') THEN
        CREATE INDEX "IX_GRNLineItems_MaterialVariantId" ON "GRNLineItems" ("MaterialVariantId");
        RAISE NOTICE 'Created index IX_GRNLineItems_MaterialVariantId';
    END IF;
END $$;

-- Create StockBatches table
DO $$
BEGIN
    IF NOT EXISTS (SELECT 1 FROM information_schema.tables WHERE table_name = 'StockBatches') THEN
        CREATE TABLE "StockBatches" (
            "Id" uuid NOT NULL,
            "MaterialVariantId" uuid NOT NULL,
            "BatchNumber" text NOT NULL,
            "GRNLineItemId" uuid NOT NULL,
            "VendorId" uuid NOT NULL,
            "QuantityReceived" numeric NOT NULL,
            "QuantityAvailable" numeric NOT NULL,
            "QuantityReserved" numeric NOT NULL,
            "QuantityConsumed" numeric NOT NULL,
            "UnitPrice" numeric NOT NULL,
            "QualityStatus" text NOT NULL,
            "CreatedAt" timestamp with time zone NOT NULL,
            "UpdatedAt" timestamp with time zone NULL,
            "IsDeleted" boolean NOT NULL DEFAULT false,
            "RowVersion" bytea NOT NULL DEFAULT '\x',
            CONSTRAINT "PK_StockBatches" PRIMARY KEY ("Id"),
            CONSTRAINT "FK_StockBatches_MaterialVariants_MaterialVariantId"
                FOREIGN KEY ("MaterialVariantId") REFERENCES "MaterialVariants" ("Id") ON DELETE RESTRICT,
            CONSTRAINT "FK_StockBatches_GRNLineItems_GRNLineItemId"
                FOREIGN KEY ("GRNLineItemId") REFERENCES "GRNLineItems" ("Id") ON DELETE RESTRICT,
            CONSTRAINT "FK_StockBatches_Vendors_VendorId"
                FOREIGN KEY ("VendorId") REFERENCES "Vendors" ("Id") ON DELETE RESTRICT
        );
        RAISE NOTICE 'Created StockBatches table';
    END IF;
END $$;

-- Create indexes for StockBatches
DO $$
BEGIN
    IF NOT EXISTS (SELECT 1 FROM pg_indexes WHERE indexname = 'IX_StockBatches_MaterialVariantId_BatchNumber') THEN
        CREATE UNIQUE INDEX "IX_StockBatches_MaterialVariantId_BatchNumber" ON "StockBatches" ("MaterialVariantId", "BatchNumber");
        RAISE NOTICE 'Created index IX_StockBatches_MaterialVariantId_BatchNumber';
    END IF;
    
    IF NOT EXISTS (SELECT 1 FROM pg_indexes WHERE indexname = 'IX_StockBatches_GRNLineItemId') THEN
        CREATE UNIQUE INDEX "IX_StockBatches_GRNLineItemId" ON "StockBatches" ("GRNLineItemId");
        RAISE NOTICE 'Created index IX_StockBatches_GRNLineItemId';
    END IF;
    
    IF NOT EXISTS (SELECT 1 FROM pg_indexes WHERE indexname = 'IX_StockBatches_VendorId') THEN
        CREATE INDEX "IX_StockBatches_VendorId" ON "StockBatches" ("VendorId");
        RAISE NOTICE 'Created index IX_StockBatches_VendorId';
    END IF;
END $$;

-- Create SRSs table
DO $$
BEGIN
    IF NOT EXISTS (SELECT 1 FROM information_schema.tables WHERE table_name = 'SRSs') THEN
        CREATE TABLE "SRSs" (
            "Id" uuid NOT NULL,
            "WorkOrderId" uuid NOT NULL,
            "Status" text NOT NULL,
            "CreatedAt" timestamp with time zone NOT NULL,
            "UpdatedAt" timestamp with time zone NULL,
            "IsDeleted" boolean NOT NULL DEFAULT false,
            "RowVersion" bytea NOT NULL DEFAULT '\x',
            CONSTRAINT "PK_SRSs" PRIMARY KEY ("Id"),
            CONSTRAINT "FK_SRSs_WorkOrders_WorkOrderId"
                FOREIGN KEY ("WorkOrderId") REFERENCES "WorkOrders" ("Id") ON DELETE RESTRICT
        );
        RAISE NOTICE 'Created SRSs table';
    END IF;
END $$;

-- Create SRSLineItems table
DO $$
BEGIN
    IF NOT EXISTS (SELECT 1 FROM information_schema.tables WHERE table_name = 'SRSLineItems') THEN
        CREATE TABLE "SRSLineItems" (
            "Id" uuid NOT NULL,
            "SRSId" uuid NOT NULL,
            "MaterialVariantId" uuid NOT NULL,
            "RequiredQuantity" numeric NOT NULL,
            "CreatedAt" timestamp with time zone NOT NULL,
            "UpdatedAt" timestamp with time zone NULL,
            "IsDeleted" boolean NOT NULL DEFAULT false,
            "RowVersion" bytea NOT NULL DEFAULT '\x',
            CONSTRAINT "PK_SRSLineItems" PRIMARY KEY ("Id"),
            CONSTRAINT "FK_SRSLineItems_SRSs_SRSId"
                FOREIGN KEY ("SRSId") REFERENCES "SRSs" ("Id") ON DELETE RESTRICT,
            CONSTRAINT "FK_SRSLineItems_MaterialVariants_MaterialVariantId"
                FOREIGN KEY ("MaterialVariantId") REFERENCES "MaterialVariants" ("Id") ON DELETE RESTRICT
        );
        RAISE NOTICE 'Created SRSLineItems table';
    END IF;
END $$;

-- Create SRSBatchAllocations table
DO $$
BEGIN
    IF NOT EXISTS (SELECT 1 FROM information_schema.tables WHERE table_name = 'SRSBatchAllocations') THEN
        CREATE TABLE "SRSBatchAllocations" (
            "Id" uuid NOT NULL,
            "SRSLineItemId" uuid NOT NULL,
            "StockBatchId" uuid NOT NULL,
            "ReservedQuantity" numeric NOT NULL,
            "ConsumedQuantity" numeric NOT NULL,
            "CreatedAt" timestamp with time zone NOT NULL,
            "UpdatedAt" timestamp with time zone NULL,
            "IsDeleted" boolean NOT NULL DEFAULT false,
            "RowVersion" bytea NOT NULL DEFAULT '\x',
            CONSTRAINT "PK_SRSBatchAllocations" PRIMARY KEY ("Id"),
            CONSTRAINT "FK_SRSBatchAllocations_SRSLineItems_SRSLineItemId"
                FOREIGN KEY ("SRSLineItemId") REFERENCES "SRSLineItems" ("Id") ON DELETE RESTRICT,
            CONSTRAINT "FK_SRSBatchAllocations_StockBatches_StockBatchId"
                FOREIGN KEY ("StockBatchId") REFERENCES "StockBatches" ("Id") ON DELETE RESTRICT
        );
        RAISE NOTICE 'Created SRSBatchAllocations table';
    END IF;
END $$;

-- Create indexes for SRSs, SRSLineItems, and SRSBatchAllocations
DO $$
BEGIN
    IF NOT EXISTS (SELECT 1 FROM pg_indexes WHERE indexname = 'IX_SRSs_WorkOrderId') THEN
        CREATE INDEX "IX_SRSs_WorkOrderId" ON "SRSs" ("WorkOrderId");
        RAISE NOTICE 'Created index IX_SRSs_WorkOrderId';
    END IF;
    
    IF NOT EXISTS (SELECT 1 FROM pg_indexes WHERE indexname = 'IX_SRSLineItems_SRSId') THEN
        CREATE INDEX "IX_SRSLineItems_SRSId" ON "SRSLineItems" ("SRSId");
        RAISE NOTICE 'Created index IX_SRSLineItems_SRSId';
    END IF;
    
    IF NOT EXISTS (SELECT 1 FROM pg_indexes WHERE indexname = 'IX_SRSLineItems_MaterialVariantId') THEN
        CREATE INDEX "IX_SRSLineItems_MaterialVariantId" ON "SRSLineItems" ("MaterialVariantId");
        RAISE NOTICE 'Created index IX_SRSLineItems_MaterialVariantId';
    END IF;
    
    IF NOT EXISTS (SELECT 1 FROM pg_indexes WHERE indexname = 'IX_SRSBatchAllocations_SRSLineItemId') THEN
        CREATE INDEX "IX_SRSBatchAllocations_SRSLineItemId" ON "SRSBatchAllocations" ("SRSLineItemId");
        RAISE NOTICE 'Created index IX_SRSBatchAllocations_SRSLineItemId';
    END IF;
    
    IF NOT EXISTS (SELECT 1 FROM pg_indexes WHERE indexname = 'IX_SRSBatchAllocations_StockBatchId') THEN
        CREATE INDEX "IX_SRSBatchAllocations_StockBatchId" ON "SRSBatchAllocations" ("StockBatchId");
        RAISE NOTICE 'Created index IX_SRSBatchAllocations_StockBatchId';
    END IF;
END $$;

-- Create WorkOrderMaterialRequirements table
DO $$
BEGIN
    IF NOT EXISTS (SELECT 1 FROM information_schema.tables WHERE table_name = 'WorkOrderMaterialRequirements') THEN
        CREATE TABLE "WorkOrderMaterialRequirements" (
            "Id" uuid NOT NULL,
            "WorkOrderId" uuid NOT NULL,
            "MaterialVariantId" uuid NOT NULL,
            "RequiredQuantity" numeric NOT NULL,
            "ReservedQuantity" numeric NOT NULL,
            "ConsumedQuantity" numeric NOT NULL,
            "CreatedAt" timestamp with time zone NOT NULL,
            "UpdatedAt" timestamp with time zone NULL,
            "IsDeleted" boolean NOT NULL DEFAULT false,
            "RowVersion" bytea NOT NULL DEFAULT '\x',
            CONSTRAINT "PK_WorkOrderMaterialRequirements" PRIMARY KEY ("Id"),
            CONSTRAINT "FK_WorkOrderMaterialRequirements_MaterialVariants_MaterialVariantId"
                FOREIGN KEY ("MaterialVariantId") REFERENCES "MaterialVariants" ("Id") ON DELETE RESTRICT,
            CONSTRAINT "FK_WorkOrderMaterialRequirements_WorkOrders_WorkOrderId"
                FOREIGN KEY ("WorkOrderId") REFERENCES "WorkOrders" ("Id") ON DELETE RESTRICT
        );
        RAISE NOTICE 'Created WorkOrderMaterialRequirements table';
    END IF;
END $$;

-- Create indexes for WorkOrderMaterialRequirements
DO $$
BEGIN
    IF NOT EXISTS (SELECT 1 FROM pg_indexes WHERE indexname = 'IX_WorkOrderMaterialRequirements_WorkOrderId') THEN
        CREATE INDEX "IX_WorkOrderMaterialRequirements_WorkOrderId" ON "WorkOrderMaterialRequirements" ("WorkOrderId");
        RAISE NOTICE 'Created index IX_WorkOrderMaterialRequirements_WorkOrderId';
    END IF;
    
    IF NOT EXISTS (SELECT 1 FROM pg_indexes WHERE indexname = 'IX_WorkOrderMaterialRequirements_MaterialVariantId') THEN
        CREATE INDEX "IX_WorkOrderMaterialRequirements_MaterialVariantId" ON "WorkOrderMaterialRequirements" ("MaterialVariantId");
        RAISE NOTICE 'Created index IX_WorkOrderMaterialRequirements_MaterialVariantId';
    END IF;
END $$;

-- Create StockTransactions table
DO $$
BEGIN
    IF NOT EXISTS (SELECT 1 FROM information_schema.tables WHERE table_name = 'StockTransactions') THEN
        CREATE TABLE "StockTransactions" (
            "Id" uuid NOT NULL,
            "StockBatchId" uuid NOT NULL,
            "TransactionType" text NOT NULL,
            "Quantity" numeric NOT NULL,
            "LinkedWorkOrderId" uuid NULL,
            "LinkedSRSId" uuid NULL,
            "Notes" text NULL,
            "CreatedAt" timestamp with time zone NOT NULL,
            "UpdatedAt" timestamp with time zone NULL,
            "IsDeleted" boolean NOT NULL DEFAULT false,
            "RowVersion" bytea NOT NULL DEFAULT '\x',
            CONSTRAINT "PK_StockTransactions" PRIMARY KEY ("Id"),
            CONSTRAINT "FK_StockTransactions_StockBatches_StockBatchId"
                FOREIGN KEY ("StockBatchId") REFERENCES "StockBatches" ("Id") ON DELETE RESTRICT,
            CONSTRAINT "FK_StockTransactions_WorkOrders_LinkedWorkOrderId"
                FOREIGN KEY ("LinkedWorkOrderId") REFERENCES "WorkOrders" ("Id") ON DELETE RESTRICT,
            CONSTRAINT "FK_StockTransactions_SRSs_LinkedSRSId"
                FOREIGN KEY ("LinkedSRSId") REFERENCES "SRSs" ("Id") ON DELETE RESTRICT
        );
        RAISE NOTICE 'Created StockTransactions table';
    END IF;
END $$;

-- Create indexes for StockTransactions
DO $$
BEGIN
    IF NOT EXISTS (SELECT 1 FROM pg_indexes WHERE indexname = 'IX_StockTransactions_StockBatchId') THEN
        CREATE INDEX "IX_StockTransactions_StockBatchId" ON "StockTransactions" ("StockBatchId");
        RAISE NOTICE 'Created index IX_StockTransactions_StockBatchId';
    END IF;
    
    IF NOT EXISTS (SELECT 1 FROM pg_indexes WHERE indexname = 'IX_StockTransactions_LinkedWorkOrderId') THEN
        CREATE INDEX "IX_StockTransactions_LinkedWorkOrderId" ON "StockTransactions" ("LinkedWorkOrderId");
        RAISE NOTICE 'Created index IX_StockTransactions_LinkedWorkOrderId';
    END IF;
    
    IF NOT EXISTS (SELECT 1 FROM pg_indexes WHERE indexname = 'IX_StockTransactions_LinkedSRSId') THEN
        CREATE INDEX "IX_StockTransactions_LinkedSRSId" ON "StockTransactions" ("LinkedSRSId");
        RAISE NOTICE 'Created index IX_StockTransactions_LinkedSRSId';
    END IF;
END $$;

-- Record migration in EF history
DO $$
BEGIN
    IF NOT EXISTS (SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260221170000_AddInventoryProcurementModule') THEN
        INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
        VALUES ('20260221170000_AddInventoryProcurementModule', '10.0.0');
        RAISE NOTICE 'Added migration record: 20260221170000_AddInventoryProcurementModule';
    END IF;
END $$;

-- ============================================
-- Migration: 20260222120000_AddVendorPurchaseInvoicesAndGrnInvoiceLink
-- ============================================

-- Add VendorPurchaseInvoiceId column to GRNs
DO $$
BEGIN
    IF NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name = 'GRNs' AND column_name = 'VendorPurchaseInvoiceId') THEN
        ALTER TABLE "GRNs" ADD COLUMN "VendorPurchaseInvoiceId" uuid NULL;
        RAISE NOTICE 'Added VendorPurchaseInvoiceId column to GRNs';
    END IF;
END $$;

-- Create VendorPurchaseInvoices table
DO $$
BEGIN
    IF NOT EXISTS (SELECT 1 FROM information_schema.tables WHERE table_name = 'VendorPurchaseInvoices') THEN
        CREATE TABLE "VendorPurchaseInvoices" (
            "Id" uuid NOT NULL,
            "VendorPurchaseOrderId" uuid NOT NULL,
            "VendorId" uuid NOT NULL,
            "InvoiceNumber" text NOT NULL,
            "InvoiceDate" timestamp with time zone NOT NULL,
            "Status" text NOT NULL,
            "CreatedAt" timestamp with time zone NOT NULL,
            "UpdatedAt" timestamp with time zone NULL,
            "IsDeleted" boolean NOT NULL DEFAULT false,
            "RowVersion" bytea NOT NULL DEFAULT '\x',
            CONSTRAINT "PK_VendorPurchaseInvoices" PRIMARY KEY ("Id"),
            CONSTRAINT "FK_VendorPurchaseInvoices_VendorPurchaseOrders_VendorPurchaseOrderId"
                FOREIGN KEY ("VendorPurchaseOrderId") REFERENCES "VendorPurchaseOrders" ("Id") ON DELETE RESTRICT,
            CONSTRAINT "FK_VendorPurchaseInvoices_Vendors_VendorId"
                FOREIGN KEY ("VendorId") REFERENCES "Vendors" ("Id") ON DELETE RESTRICT
        );
        RAISE NOTICE 'Created VendorPurchaseInvoices table';
    END IF;
END $$;

-- Create VendorPurchaseInvoiceLineItems table
DO $$
BEGIN
    IF NOT EXISTS (SELECT 1 FROM information_schema.tables WHERE table_name = 'VendorPurchaseInvoiceLineItems') THEN
        CREATE TABLE "VendorPurchaseInvoiceLineItems" (
            "Id" uuid NOT NULL,
            "VendorPurchaseInvoiceId" uuid NOT NULL,
            "MaterialVariantId" uuid NOT NULL,
            "Quantity" numeric NOT NULL,
            "UnitPrice" numeric NOT NULL,
            "CreatedAt" timestamp with time zone NOT NULL,
            "UpdatedAt" timestamp with time zone NULL,
            "IsDeleted" boolean NOT NULL DEFAULT false,
            "RowVersion" bytea NOT NULL DEFAULT '\x',
            CONSTRAINT "PK_VendorPurchaseInvoiceLineItems" PRIMARY KEY ("Id"),
            CONSTRAINT "FK_VendorPurchaseInvoiceLineItems_VendorPurchaseInvoices_VendorPurchaseInvoiceId"
                FOREIGN KEY ("VendorPurchaseInvoiceId") REFERENCES "VendorPurchaseInvoices" ("Id") ON DELETE RESTRICT,
            CONSTRAINT "FK_VendorPurchaseInvoiceLineItems_MaterialVariants_MaterialVariantId"
                FOREIGN KEY ("MaterialVariantId") REFERENCES "MaterialVariants" ("Id") ON DELETE RESTRICT
        );
        RAISE NOTICE 'Created VendorPurchaseInvoiceLineItems table';
    END IF;
END $$;

-- Create indexes for VendorPurchaseInvoices and VendorPurchaseInvoiceLineItems
DO $$
BEGIN
    IF NOT EXISTS (SELECT 1 FROM pg_indexes WHERE indexname = 'IX_GRNs_VendorPurchaseInvoiceId') THEN
        CREATE INDEX "IX_GRNs_VendorPurchaseInvoiceId" ON "GRNs" ("VendorPurchaseInvoiceId");
        RAISE NOTICE 'Created index IX_GRNs_VendorPurchaseInvoiceId';
    END IF;
    
    IF NOT EXISTS (SELECT 1 FROM pg_indexes WHERE indexname = 'IX_VendorPurchaseInvoices_VendorPurchaseOrderId') THEN
        CREATE INDEX "IX_VendorPurchaseInvoices_VendorPurchaseOrderId" ON "VendorPurchaseInvoices" ("VendorPurchaseOrderId");
        RAISE NOTICE 'Created index IX_VendorPurchaseInvoices_VendorPurchaseOrderId';
    END IF;
    
    IF NOT EXISTS (SELECT 1 FROM pg_indexes WHERE indexname = 'IX_VendorPurchaseInvoices_VendorId') THEN
        CREATE INDEX "IX_VendorPurchaseInvoices_VendorId" ON "VendorPurchaseInvoices" ("VendorId");
        RAISE NOTICE 'Created index IX_VendorPurchaseInvoices_VendorId';
    END IF;
    
    IF NOT EXISTS (SELECT 1 FROM pg_indexes WHERE indexname = 'IX_VendorPurchaseInvoices_VendorId_InvoiceNumber') THEN
        CREATE UNIQUE INDEX "IX_VendorPurchaseInvoices_VendorId_InvoiceNumber" ON "VendorPurchaseInvoices" ("VendorId", "InvoiceNumber");
        RAISE NOTICE 'Created index IX_VendorPurchaseInvoices_VendorId_InvoiceNumber';
    END IF;
    
    IF NOT EXISTS (SELECT 1 FROM pg_indexes WHERE indexname = 'IX_VendorPurchaseInvoiceLineItems_VendorPurchaseInvoiceId') THEN
        CREATE INDEX "IX_VendorPurchaseInvoiceLineItems_VendorPurchaseInvoiceId" ON "VendorPurchaseInvoiceLineItems" ("VendorPurchaseInvoiceId");
        RAISE NOTICE 'Created index IX_VendorPurchaseInvoiceLineItems_VendorPurchaseInvoiceId';
    END IF;
    
    IF NOT EXISTS (SELECT 1 FROM pg_indexes WHERE indexname = 'IX_VendorPurchaseInvoiceLineItems_MaterialVariantId') THEN
        CREATE INDEX "IX_VendorPurchaseInvoiceLineItems_MaterialVariantId" ON "VendorPurchaseInvoiceLineItems" ("MaterialVariantId");
        RAISE NOTICE 'Created index IX_VendorPurchaseInvoiceLineItems_MaterialVariantId';
    END IF;
END $$;

-- Add foreign key for GRNs.VendorPurchaseInvoiceId
DO $$
BEGIN
    IF NOT EXISTS (
        SELECT 1 FROM information_schema.table_constraints 
        WHERE constraint_name = 'FK_GRNs_VendorPurchaseInvoices_VendorPurchaseInvoiceId'
    ) THEN
        ALTER TABLE "GRNs"
        ADD CONSTRAINT "FK_GRNs_VendorPurchaseInvoices_VendorPurchaseInvoiceId"
        FOREIGN KEY ("VendorPurchaseInvoiceId") REFERENCES "VendorPurchaseInvoices" ("Id") ON DELETE RESTRICT;
        RAISE NOTICE 'Added FK_GRNs_VendorPurchaseInvoices_VendorPurchaseInvoiceId';
    END IF;
END $$;

-- Record migration in EF history
DO $$
BEGIN
    IF NOT EXISTS (SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260222120000_AddVendorPurchaseInvoicesAndGrnInvoiceLink') THEN
        INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
        VALUES ('20260222120000_AddVendorPurchaseInvoicesAndGrnInvoiceLink', '10.0.0');
        RAISE NOTICE 'Added migration record: 20260222120000_AddVendorPurchaseInvoicesAndGrnInvoiceLink';
    END IF;
END $$;

-- ============================================
-- Migration: 20260222123000_AddQuotationPricingFields
-- ============================================

-- Add pricing fields to Quotations
DO $$
BEGIN
    IF NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name = 'Quotations' AND column_name = 'ManualPrice') THEN
        ALTER TABLE "Quotations" ADD COLUMN "ManualPrice" numeric NULL;
        RAISE NOTICE 'Added ManualPrice column to Quotations';
    END IF;
    
    IF NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name = 'Quotations' AND column_name = 'PriceBreakdown') THEN
        ALTER TABLE "Quotations" ADD COLUMN "PriceBreakdown" text NULL;
        RAISE NOTICE 'Added PriceBreakdown column to Quotations';
    END IF;
END $$;

-- Add pricing fields to QuotationRevisions
DO $$
BEGIN
    IF NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name = 'QuotationRevisions' AND column_name = 'SnapshotManualPrice') THEN
        ALTER TABLE "QuotationRevisions" ADD COLUMN "SnapshotManualPrice" numeric NULL;
        RAISE NOTICE 'Added SnapshotManualPrice column to QuotationRevisions';
    END IF;
    
    IF NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name = 'QuotationRevisions' AND column_name = 'SnapshotPriceBreakdown') THEN
        ALTER TABLE "QuotationRevisions" ADD COLUMN "SnapshotPriceBreakdown" text NULL;
        RAISE NOTICE 'Added SnapshotPriceBreakdown column to QuotationRevisions';
    END IF;
END $$;

-- Record migration in EF history
DO $$
BEGIN
    IF NOT EXISTS (SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260222123000_AddQuotationPricingFields') THEN
        INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
        VALUES ('20260222123000_AddQuotationPricingFields', '10.0.0');
        RAISE NOTICE 'Added migration record: 20260222123000_AddQuotationPricingFields';
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
