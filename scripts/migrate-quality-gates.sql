-- Phase 1 QC Gates + Immutable QC Checks + NCR (manual SQL migration)
-- Creates:
--   - WorkOrderQualityGates
--   - WorkOrderQualityChecks (append-only log)
--   - Ncrs
-- Migrates legacy QualityInspections -> WorkOrderQualityChecks (Stage=QC) and creates NCRs for latest FAILs.
--
-- Run:
--   psql -U postgres -d heatconerp -f scripts/migrate-quality-gates.sql

-- ============================================
-- Helpers
-- ============================================
-- Deterministic UUID for (workOrderId + stage) without requiring extensions.
-- Uses md5 -> uuid text formatting.
-- We inline the expression wherever needed:
--   (substr(m,1,8)||'-'||substr(m,9,4)||'-'||substr(m,13,4)||'-'||substr(m,17,4)||'-'||substr(m,21,12))::uuid

-- ============================================
-- 1) Create WorkOrderQualityGates
-- ============================================
DO $$
BEGIN
    IF NOT EXISTS (SELECT 1 FROM information_schema.tables WHERE table_name = 'WorkOrderQualityGates') THEN
        CREATE TABLE "WorkOrderQualityGates" (
            "Id" uuid NOT NULL,
            "WorkOrderId" uuid NOT NULL,
            "Stage" text NOT NULL,
            "GateStatus" text NOT NULL,
            "PassedAt" timestamp with time zone NULL,
            "PassedBy" text NULL,
            "FailedAt" timestamp with time zone NULL,
            "FailedBy" text NULL,
            "CreatedAt" timestamp with time zone NOT NULL,
            "UpdatedAt" timestamp with time zone NULL,
            "IsDeleted" boolean NOT NULL DEFAULT false,
            "RowVersion" bytea NOT NULL DEFAULT ''::bytea,
            CONSTRAINT "PK_WorkOrderQualityGates" PRIMARY KEY ("Id")
        );
        RAISE NOTICE 'Created WorkOrderQualityGates';
    END IF;
END $$;

DO $$
BEGIN
    IF NOT EXISTS (SELECT 1 FROM pg_indexes WHERE indexname = 'IX_WorkOrderQualityGates_WorkOrderId') THEN
        CREATE INDEX "IX_WorkOrderQualityGates_WorkOrderId" ON "WorkOrderQualityGates" ("WorkOrderId");
        RAISE NOTICE 'Created index IX_WorkOrderQualityGates_WorkOrderId';
    END IF;

    IF NOT EXISTS (SELECT 1 FROM pg_indexes WHERE indexname = 'UX_WorkOrderQualityGates_WorkOrderId_Stage') THEN
        CREATE UNIQUE INDEX "UX_WorkOrderQualityGates_WorkOrderId_Stage" ON "WorkOrderQualityGates" ("WorkOrderId", "Stage");
        RAISE NOTICE 'Created unique index UX_WorkOrderQualityGates_WorkOrderId_Stage';
    END IF;

    IF NOT EXISTS (SELECT 1 FROM information_schema.table_constraints WHERE constraint_name = 'FK_WorkOrderQualityGates_WorkOrders_WorkOrderId') THEN
        ALTER TABLE "WorkOrderQualityGates"
        ADD CONSTRAINT "FK_WorkOrderQualityGates_WorkOrders_WorkOrderId"
        FOREIGN KEY ("WorkOrderId") REFERENCES "WorkOrders" ("Id") ON DELETE RESTRICT;
        RAISE NOTICE 'Added FK_WorkOrderQualityGates_WorkOrders_WorkOrderId';
    END IF;
END $$;

-- Backfill gates for ALL existing work orders (idempotent).
DO $$
BEGIN
    IF EXISTS (SELECT 1 FROM information_schema.tables WHERE table_name = 'WorkOrders') THEN
        WITH stages AS (
            SELECT unnest(ARRAY['Planning','Material','Assembly','Testing','QC','Packing']) AS stage
        ),
        src AS (
            SELECT w."Id" AS workOrderId, s.stage,
                   md5(w."Id"::text || ':' || s.stage) AS m
            FROM "WorkOrders" w
            CROSS JOIN stages s
        )
        INSERT INTO "WorkOrderQualityGates"
        ("Id","WorkOrderId","Stage","GateStatus","PassedAt","PassedBy","FailedAt","FailedBy","CreatedAt","UpdatedAt","IsDeleted","RowVersion")
        SELECT
            (substr(m,1,8)||'-'||substr(m,9,4)||'-'||substr(m,13,4)||'-'||substr(m,17,4)||'-'||substr(m,21,12))::uuid,
            workOrderId,
            stage,
            'Pending',
            NULL,NULL,NULL,NULL,
            CURRENT_TIMESTAMP,
            NULL,
            false,
            ''::bytea
        FROM src
        WHERE NOT EXISTS (
            SELECT 1 FROM "WorkOrderQualityGates" g
            WHERE g."WorkOrderId" = src.workOrderId AND g."Stage" = src.stage
        );
        RAISE NOTICE 'Backfilled WorkOrderQualityGates for existing WorkOrders (if needed)';
    END IF;
END $$;

-- ============================================
-- 2) Create WorkOrderQualityChecks (immutable, append-only)
-- ============================================
DO $$
BEGIN
    IF NOT EXISTS (SELECT 1 FROM information_schema.tables WHERE table_name = 'WorkOrderQualityChecks') THEN
        CREATE TABLE "WorkOrderQualityChecks" (
            "Id" uuid NOT NULL,
            "WorkOrderId" uuid NOT NULL,
            "WorkOrderQualityGateId" uuid NOT NULL,
            "Stage" text NOT NULL,
            "Result" text NOT NULL,
            "Notes" text NULL,
            "CreatedBy" text NOT NULL,
            "CreatedAt" timestamp with time zone NOT NULL,
            "UpdatedAt" timestamp with time zone NULL,
            "IsDeleted" boolean NOT NULL DEFAULT false,
            "RowVersion" bytea NOT NULL DEFAULT ''::bytea,
            CONSTRAINT "PK_WorkOrderQualityChecks" PRIMARY KEY ("Id")
        );
        RAISE NOTICE 'Created WorkOrderQualityChecks';
    END IF;
END $$;

DO $$
BEGIN
    IF NOT EXISTS (SELECT 1 FROM pg_indexes WHERE indexname = 'IX_WorkOrderQualityChecks_WorkOrderId') THEN
        CREATE INDEX "IX_WorkOrderQualityChecks_WorkOrderId" ON "WorkOrderQualityChecks" ("WorkOrderId");
        RAISE NOTICE 'Created index IX_WorkOrderQualityChecks_WorkOrderId';
    END IF;

    IF NOT EXISTS (SELECT 1 FROM pg_indexes WHERE indexname = 'IX_WorkOrderQualityChecks_GateId') THEN
        CREATE INDEX "IX_WorkOrderQualityChecks_GateId" ON "WorkOrderQualityChecks" ("WorkOrderQualityGateId");
        RAISE NOTICE 'Created index IX_WorkOrderQualityChecks_GateId';
    END IF;

    IF NOT EXISTS (SELECT 1 FROM information_schema.table_constraints WHERE constraint_name = 'FK_WorkOrderQualityChecks_WorkOrders_WorkOrderId') THEN
        ALTER TABLE "WorkOrderQualityChecks"
        ADD CONSTRAINT "FK_WorkOrderQualityChecks_WorkOrders_WorkOrderId"
        FOREIGN KEY ("WorkOrderId") REFERENCES "WorkOrders" ("Id") ON DELETE RESTRICT;
        RAISE NOTICE 'Added FK_WorkOrderQualityChecks_WorkOrders_WorkOrderId';
    END IF;

    IF NOT EXISTS (SELECT 1 FROM information_schema.table_constraints WHERE constraint_name = 'FK_WorkOrderQualityChecks_WorkOrderQualityGates_WorkOrderQualityGateId') THEN
        ALTER TABLE "WorkOrderQualityChecks"
        ADD CONSTRAINT "FK_WorkOrderQualityChecks_WorkOrderQualityGates_WorkOrderQualityGateId"
        FOREIGN KEY ("WorkOrderQualityGateId") REFERENCES "WorkOrderQualityGates" ("Id") ON DELETE RESTRICT;
        RAISE NOTICE 'Added FK_WorkOrderQualityChecks_WorkOrderQualityGates_WorkOrderQualityGateId';
    END IF;
END $$;

-- ============================================
-- 3) Create Ncrs (simple Phase 1)
-- ============================================
DO $$
BEGIN
    IF NOT EXISTS (SELECT 1 FROM information_schema.tables WHERE table_name = 'Ncrs') THEN
        CREATE TABLE "Ncrs" (
            "Id" uuid NOT NULL,
            "WorkOrderId" uuid NOT NULL,
            "Stage" text NOT NULL,
            "Description" text NOT NULL,
            "Status" text NOT NULL,
            "Disposition" text NULL,
            "CreatedBy" text NOT NULL,
            "ClosedAt" timestamp with time zone NULL,
            "ClosedBy" text NULL,
            "ClosureNotes" text NULL,
            "CreatedAt" timestamp with time zone NOT NULL,
            "UpdatedAt" timestamp with time zone NULL,
            "IsDeleted" boolean NOT NULL DEFAULT false,
            "RowVersion" bytea NOT NULL DEFAULT ''::bytea,
            CONSTRAINT "PK_Ncrs" PRIMARY KEY ("Id")
        );
        RAISE NOTICE 'Created Ncrs';
    END IF;
END $$;

DO $$
BEGIN
    IF NOT EXISTS (SELECT 1 FROM pg_indexes WHERE indexname = 'IX_Ncrs_WorkOrderId') THEN
        CREATE INDEX "IX_Ncrs_WorkOrderId" ON "Ncrs" ("WorkOrderId");
        RAISE NOTICE 'Created index IX_Ncrs_WorkOrderId';
    END IF;

    IF NOT EXISTS (SELECT 1 FROM information_schema.table_constraints WHERE constraint_name = 'FK_Ncrs_WorkOrders_WorkOrderId') THEN
        ALTER TABLE "Ncrs"
        ADD CONSTRAINT "FK_Ncrs_WorkOrders_WorkOrderId"
        FOREIGN KEY ("WorkOrderId") REFERENCES "WorkOrders" ("Id") ON DELETE RESTRICT;
        RAISE NOTICE 'Added FK_Ncrs_WorkOrders_WorkOrderId';
    END IF;
END $$;

-- ============================================
-- 4) Migrate legacy QualityInspections -> WorkOrderQualityChecks (Stage=QC)
-- ============================================
DO $$
BEGIN
    IF EXISTS (SELECT 1 FROM information_schema.tables WHERE table_name = 'QualityInspections') THEN
        -- Insert immutable QC checks from legacy inspections (idempotent by Id).
        INSERT INTO "WorkOrderQualityChecks"
        ("Id","WorkOrderId","WorkOrderQualityGateId","Stage","Result","Notes","CreatedBy","CreatedAt","UpdatedAt","IsDeleted","RowVersion")
        SELECT
            qi."Id",
            qi."WorkOrderId",
            g."Id" AS gateId,
            'QC',
            CASE WHEN lower(qi."Result") = 'pass' THEN 'Pass' ELSE 'Fail' END,
            qi."Notes",
            qi."InspectedBy",
            qi."InspectedAt",
            NULL,
            false,
            ''::bytea
        FROM "QualityInspections" qi
        JOIN "WorkOrderQualityGates" g
          ON g."WorkOrderId" = qi."WorkOrderId" AND g."Stage" = 'QC'
        WHERE NOT EXISTS (SELECT 1 FROM "WorkOrderQualityChecks" c WHERE c."Id" = qi."Id");

        RAISE NOTICE 'Migrated legacy QualityInspections into WorkOrderQualityChecks (Stage=QC)';

        -- Update QC gate status based on latest legacy inspection (Pass/Fail)
        WITH latest AS (
            SELECT DISTINCT ON (qi."WorkOrderId")
                qi."WorkOrderId",
                qi."InspectedAt",
                qi."InspectedBy",
                qi."Result"
            FROM "QualityInspections" qi
            ORDER BY qi."WorkOrderId", qi."InspectedAt" DESC
        )
        UPDATE "WorkOrderQualityGates" g
        SET
            "GateStatus" = CASE WHEN lower(l."Result") = 'pass' THEN 'Passed' ELSE 'Failed' END,
            "PassedAt"   = CASE WHEN lower(l."Result") = 'pass' THEN l."InspectedAt" ELSE NULL END,
            "PassedBy"   = CASE WHEN lower(l."Result") = 'pass' THEN l."InspectedBy" ELSE NULL END,
            "FailedAt"   = CASE WHEN lower(l."Result") = 'fail' THEN l."InspectedAt" ELSE NULL END,
            "FailedBy"   = CASE WHEN lower(l."Result") = 'fail' THEN l."InspectedBy" ELSE NULL END
        FROM latest l
        WHERE g."WorkOrderId" = l."WorkOrderId" AND g."Stage" = 'QC';

        -- Create an OPEN NCR for QC stage where the latest legacy inspection was FAIL (idempotent).
        INSERT INTO "Ncrs"
        ("Id","WorkOrderId","Stage","Description","Status","Disposition","CreatedBy","ClosedAt","ClosedBy","ClosureNotes","CreatedAt","UpdatedAt","IsDeleted","RowVersion")
        SELECT
            (substr(m,1,8)||'-'||substr(m,9,4)||'-'||substr(m,13,4)||'-'||substr(m,17,4)||'-'||substr(m,21,12))::uuid,
            l."WorkOrderId",
            'QC',
            COALESCE(NULLIF(l."Notes", ''), 'QC failed (migrated from legacy inspections)'),
            'Open',
            NULL,
            l."InspectedBy",
            NULL,NULL,NULL,
            l."InspectedAt",
            NULL,
            false,
            ''::bytea
        FROM (
            SELECT DISTINCT ON (qi."WorkOrderId")
                qi."WorkOrderId",
                qi."InspectedAt",
                qi."InspectedBy",
                qi."Result",
                qi."Notes",
                md5(qi."WorkOrderId"::text || ':QC:NCR') AS m
            FROM "QualityInspections" qi
            ORDER BY qi."WorkOrderId", qi."InspectedAt" DESC
        ) l
        WHERE lower(l."Result") = 'fail'
          AND NOT EXISTS (
            SELECT 1 FROM "Ncrs" n
            WHERE n."WorkOrderId" = l."WorkOrderId" AND n."Stage" = 'QC' AND n."Status" = 'Open'
          );

        RAISE NOTICE 'Created Open NCRs for latest QC failures (legacy migration)';
    END IF;
END $$;

-- ============================================
-- 5) Drop legacy QualityInspections
-- ============================================
DO $$
BEGIN
    IF EXISTS (SELECT 1 FROM information_schema.tables WHERE table_name = 'QualityInspections') THEN
        DROP TABLE "QualityInspections";
        RAISE NOTICE 'Dropped legacy QualityInspections';
    END IF;
END $$;

SELECT 'Quality gates migration applied successfully!' AS result;


