-- One-shot, idempotent DB schema fixer + sanity checks
-- Run:
--   psql -U postgres -d heatconerp -v ON_ERROR_STOP=1 -f scripts/ensure-db-noissues.sql

-- Make sure EF history table exists before any script tries to insert into it.
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

-- Quotation schema patches (idempotent)
\i scripts/apply-quotation-migration.sql
\i scripts/add-revision-snapshot-columns.sql
\i scripts/add-sent-to-customer-columns.sql

-- Core schema fixes (includes QC gates migration via \i scripts/migrate-quality-gates.sql)
\i scripts/fix-all-migrations.sql

-- Enquiry schema patches (idempotent)
\i scripts/expand-enquiry-schema.sql

-- ============================================================
-- Sanity checks: hard fail if required objects are still missing
-- ============================================================
DO $$
DECLARE
    missing text := '';
BEGIN
    -- Tables the API/UI touches (high-signal set)
    IF NOT EXISTS (SELECT 1 FROM information_schema.tables WHERE table_name = 'Users') THEN
        missing := missing || E'\n- Users';
    END IF;
    IF NOT EXISTS (SELECT 1 FROM information_schema.tables WHERE table_name = 'WorkOrders') THEN
        missing := missing || E'\n- WorkOrders';
    END IF;
    IF NOT EXISTS (SELECT 1 FROM information_schema.tables WHERE table_name = 'WorkOrderLineItems') THEN
        missing := missing || E'\n- WorkOrderLineItems';
    END IF;
    IF NOT EXISTS (SELECT 1 FROM information_schema.tables WHERE table_name = 'PurchaseOrders') THEN
        missing := missing || E'\n- PurchaseOrders';
    END IF;
    IF NOT EXISTS (SELECT 1 FROM information_schema.tables WHERE table_name = 'PurchaseInvoices') THEN
        missing := missing || E'\n- PurchaseInvoices';
    END IF;
    IF NOT EXISTS (SELECT 1 FROM information_schema.tables WHERE table_name = 'MaterialCategories') THEN
        missing := missing || E'\n- MaterialCategories';
    END IF;
    IF NOT EXISTS (SELECT 1 FROM information_schema.tables WHERE table_name = 'MaterialVariants') THEN
        missing := missing || E'\n- MaterialVariants';
    END IF;
    IF NOT EXISTS (SELECT 1 FROM information_schema.tables WHERE table_name = 'StockBatches') THEN
        missing := missing || E'\n- StockBatches';
    END IF;
    IF NOT EXISTS (SELECT 1 FROM information_schema.tables WHERE table_name = 'StockTransactions') THEN
        missing := missing || E'\n- StockTransactions';
    END IF;
    IF NOT EXISTS (SELECT 1 FROM information_schema.tables WHERE table_name = 'SRSs') THEN
        missing := missing || E'\n- SRSs';
    END IF;
    IF NOT EXISTS (SELECT 1 FROM information_schema.tables WHERE table_name = 'SRSLineItems') THEN
        missing := missing || E'\n- SRSLineItems';
    END IF;
    IF NOT EXISTS (SELECT 1 FROM information_schema.tables WHERE table_name = 'SRSBatchAllocations') THEN
        missing := missing || E'\n- SRSBatchAllocations';
    END IF;

    -- QC module tables (added by migrate-quality-gates.sql)
    IF NOT EXISTS (SELECT 1 FROM information_schema.tables WHERE table_name = 'WorkOrderQualityGates') THEN
        missing := missing || E'\n- WorkOrderQualityGates';
    END IF;
    IF NOT EXISTS (SELECT 1 FROM information_schema.tables WHERE table_name = 'WorkOrderQualityChecks') THEN
        missing := missing || E'\n- WorkOrderQualityChecks';
    END IF;
    IF NOT EXISTS (SELECT 1 FROM information_schema.tables WHERE table_name = 'Ncrs') THEN
        missing := missing || E'\n- Ncrs';
    END IF;

    IF missing <> '' THEN
        RAISE EXCEPTION 'Schema validation failed. Missing tables:%', missing;
    END IF;

    -- Critical WorkOrders columns
    IF NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name='WorkOrders' AND column_name='SentToProductionAt') THEN
        RAISE EXCEPTION 'Schema validation failed. Missing WorkOrders.SentToProductionAt';
    END IF;
    IF NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name='WorkOrders' AND column_name='ProductionReceivedAt') THEN
        RAISE EXCEPTION 'Schema validation failed. Missing WorkOrders.ProductionReceivedAt';
    END IF;

    RAISE NOTICE 'Schema validation OK.';
END $$;

SELECT 'ensure-db-noissues.sql completed successfully.' AS result;


