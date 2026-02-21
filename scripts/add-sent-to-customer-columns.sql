-- Add SentToCustomer columns to QuotationRevisions and QuotationRevisionId to PurchaseOrders
-- Run: psql -U postgres -d heatconerp -f scripts/add-sent-to-customer-columns.sql

-- Add SentToCustomerAt and SentToCustomerBy to QuotationRevisions
DO $$
BEGIN
    IF NOT EXISTS (
        SELECT 1 FROM information_schema.columns 
        WHERE table_name = 'QuotationRevisions' AND column_name = 'SentToCustomerAt'
    ) THEN
        ALTER TABLE "QuotationRevisions" 
        ADD COLUMN "SentToCustomerAt" timestamp with time zone NULL;
        RAISE NOTICE 'Added SentToCustomerAt column';
    ELSE
        RAISE NOTICE 'SentToCustomerAt column already exists';
    END IF;
END $$;

DO $$
BEGIN
    IF NOT EXISTS (
        SELECT 1 FROM information_schema.columns 
        WHERE table_name = 'QuotationRevisions' AND column_name = 'SentToCustomerBy'
    ) THEN
        ALTER TABLE "QuotationRevisions" 
        ADD COLUMN "SentToCustomerBy" text NULL;
        RAISE NOTICE 'Added SentToCustomerBy column';
    ELSE
        RAISE NOTICE 'SentToCustomerBy column already exists';
    END IF;
END $$;

-- Add QuotationRevisionId to PurchaseOrders
DO $$
BEGIN
    IF NOT EXISTS (
        SELECT 1 FROM information_schema.columns 
        WHERE table_name = 'PurchaseOrders' AND column_name = 'QuotationRevisionId'
    ) THEN
        ALTER TABLE "PurchaseOrders" 
        ADD COLUMN "QuotationRevisionId" uuid NULL;
        RAISE NOTICE 'Added QuotationRevisionId column';
    ELSE
        RAISE NOTICE 'QuotationRevisionId column already exists';
    END IF;
END $$;

-- Create index if it doesn't exist
DO $$
BEGIN
    IF NOT EXISTS (
        SELECT 1 FROM pg_indexes 
        WHERE indexname = 'IX_PurchaseOrders_QuotationRevisionId'
    ) THEN
        CREATE INDEX "IX_PurchaseOrders_QuotationRevisionId" 
        ON "PurchaseOrders" ("QuotationRevisionId");
        RAISE NOTICE 'Created index IX_PurchaseOrders_QuotationRevisionId';
    ELSE
        RAISE NOTICE 'Index IX_PurchaseOrders_QuotationRevisionId already exists';
    END IF;
END $$;

-- Add foreign key if it doesn't exist
DO $$
BEGIN
    IF NOT EXISTS (
        SELECT 1 FROM information_schema.table_constraints 
        WHERE constraint_name = 'FK_PurchaseOrders_QuotationRevisions_QuotationRevisionId'
    ) THEN
        ALTER TABLE "PurchaseOrders"
        ADD CONSTRAINT "FK_PurchaseOrders_QuotationRevisions_QuotationRevisionId"
        FOREIGN KEY ("QuotationRevisionId")
        REFERENCES "QuotationRevisions" ("Id")
        ON DELETE SET NULL;
        RAISE NOTICE 'Added foreign key FK_PurchaseOrders_QuotationRevisions_QuotationRevisionId';
    ELSE
        RAISE NOTICE 'Foreign key FK_PurchaseOrders_QuotationRevisions_QuotationRevisionId already exists';
    END IF;
END $$;

-- Insert migration record if missing (so EF thinks it's applied)
DO $$
BEGIN
    IF NOT EXISTS (
        SELECT 1 FROM "__EFMigrationsHistory" 
        WHERE "MigrationId" = '20260221120000_AddSentToCustomerAndPoRevisionLink'
    ) THEN
        INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
        VALUES ('20260221120000_AddSentToCustomerAndPoRevisionLink', '10.0.0');
        RAISE NOTICE 'Added migration record to history';
    ELSE
        RAISE NOTICE 'Migration record already exists';
    END IF;
END $$;

SELECT 'Migration columns added successfully!' AS result;
