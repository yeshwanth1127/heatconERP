using System;
using HeatconERP.Infrastructure.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HeatconERP.Infrastructure.Migrations
{
    /// <inheritdoc />
    [DbContext(typeof(HeatconDbContext))]
    [Migration("20260219250000_ExpandEnquirySchema")]
    public partial class ExpandEnquirySchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Add new columns (nullable first for data migration)
            // Use raw SQL to safely handle columns that might already exist
            migrationBuilder.Sql(@"
                DO $$
                BEGIN
                    IF NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name = 'Enquiries' AND column_name = 'EnquiryNumber') THEN
                        ALTER TABLE ""Enquiries"" ADD COLUMN ""EnquiryNumber"" text;
                    END IF;
                    IF NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name = 'Enquiries' AND column_name = 'DateReceived') THEN
                        ALTER TABLE ""Enquiries"" ADD COLUMN ""DateReceived"" timestamp with time zone;
                    END IF;
                    IF NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name = 'Enquiries' AND column_name = 'Source') THEN
                        ALTER TABLE ""Enquiries"" ADD COLUMN ""Source"" text;
                    END IF;
                    IF NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name = 'Enquiries' AND column_name = 'AssignedTo') THEN
                        ALTER TABLE ""Enquiries"" ADD COLUMN ""AssignedTo"" text;
                    END IF;
                    IF NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name = 'Enquiries' AND column_name = 'CompanyName') THEN
                        ALTER TABLE ""Enquiries"" ADD COLUMN ""CompanyName"" text;
                    END IF;
                    IF NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name = 'Enquiries' AND column_name = 'ContactPerson') THEN
                        ALTER TABLE ""Enquiries"" ADD COLUMN ""ContactPerson"" text;
                    END IF;
                    IF NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name = 'Enquiries' AND column_name = 'Email') THEN
                        ALTER TABLE ""Enquiries"" ADD COLUMN ""Email"" text;
                    END IF;
                    IF NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name = 'Enquiries' AND column_name = 'Phone') THEN
                        ALTER TABLE ""Enquiries"" ADD COLUMN ""Phone"" text;
                    END IF;
                    IF NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name = 'Enquiries' AND column_name = 'Gst') THEN
                        ALTER TABLE ""Enquiries"" ADD COLUMN ""Gst"" text;
                    END IF;
                    IF NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name = 'Enquiries' AND column_name = 'Address') THEN
                        ALTER TABLE ""Enquiries"" ADD COLUMN ""Address"" text;
                    END IF;
                    IF NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name = 'Enquiries' AND column_name = 'ProductDescription') THEN
                        ALTER TABLE ""Enquiries"" ADD COLUMN ""ProductDescription"" text;
                    END IF;
                    IF NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name = 'Enquiries' AND column_name = 'Quantity') THEN
                        ALTER TABLE ""Enquiries"" ADD COLUMN ""Quantity"" integer;
                    END IF;
                    IF NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name = 'Enquiries' AND column_name = 'ExpectedDeliveryDate') THEN
                        ALTER TABLE ""Enquiries"" ADD COLUMN ""ExpectedDeliveryDate"" timestamp with time zone;
                    END IF;
                    IF NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name = 'Enquiries' AND column_name = 'AttachmentPath') THEN
                        ALTER TABLE ""Enquiries"" ADD COLUMN ""AttachmentPath"" text;
                    END IF;
                    IF NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name = 'Enquiries' AND column_name = 'IsAerospace') THEN
                        ALTER TABLE ""Enquiries"" ADD COLUMN ""IsAerospace"" boolean DEFAULT false;
                    END IF;
                    IF NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name = 'Enquiries' AND column_name = 'Priority') THEN
                        ALTER TABLE ""Enquiries"" ADD COLUMN ""Priority"" text;
                    END IF;
                    IF NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name = 'Enquiries' AND column_name = 'FeasibilityStatus') THEN
                        ALTER TABLE ""Enquiries"" ADD COLUMN ""FeasibilityStatus"" text;
                    END IF;
                    IF NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name = 'Enquiries' AND column_name = 'FeasibilityNotes') THEN
                        ALTER TABLE ""Enquiries"" ADD COLUMN ""FeasibilityNotes"" text;
                    END IF;
                    IF NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name = 'Enquiries' AND column_name = 'CreatedBy') THEN
                        ALTER TABLE ""Enquiries"" ADD COLUMN ""CreatedBy"" text;
                    END IF;
                    IF NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name = 'Enquiries' AND column_name = 'UpdatedAt') THEN
                        ALTER TABLE ""Enquiries"" ADD COLUMN ""UpdatedAt"" timestamp with time zone;
                    END IF;
                    IF NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name = 'Enquiries' AND column_name = 'IsDeleted') THEN
                        ALTER TABLE ""Enquiries"" ADD COLUMN ""IsDeleted"" boolean DEFAULT false;
                    END IF;
                END $$;
            ");

            // Migrate data from old columns - handle idempotency
            migrationBuilder.Sql(@"
                DO $$
                BEGIN
                    IF EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name = 'Enquiries' AND column_name = 'ReferenceNumber') THEN
                        UPDATE ""Enquiries"" SET ""EnquiryNumber"" = COALESCE(""ReferenceNumber"", '') WHERE ""EnquiryNumber"" IS NULL;
                    END IF;
                    IF EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name = 'Enquiries' AND column_name = 'CustomerName') THEN
                        UPDATE ""Enquiries"" SET ""CompanyName"" = COALESCE(""CustomerName"", '') WHERE ""CompanyName"" IS NULL;
                    END IF;
                    IF EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name = 'Enquiries' AND column_name = 'Subject') THEN
                        UPDATE ""Enquiries"" SET ""ProductDescription"" = COALESCE(""Subject"", '') WHERE ""ProductDescription"" IS NULL;
                    END IF;
                    UPDATE ""Enquiries"" SET ""DateReceived"" = COALESCE(""DateReceived"", ""CreatedAt"") WHERE ""DateReceived"" IS NULL;
                    UPDATE ""Enquiries"" SET ""Source"" = COALESCE(""Source"", 'Manual') WHERE ""Source"" IS NULL;
                    UPDATE ""Enquiries"" SET ""Status"" = CASE WHEN ""Status"" = 'Open' THEN 'Under Review' WHEN ""Status"" = 'Closed' THEN 'Closed' ELSE COALESCE(""Status"", 'New') END;
                    UPDATE ""Enquiries"" SET ""Priority"" = COALESCE(""Priority"", 'Medium') WHERE ""Priority"" IS NULL;
                    UPDATE ""Enquiries"" SET ""FeasibilityStatus"" = COALESCE(""FeasibilityStatus"", 'Pending') WHERE ""FeasibilityStatus"" IS NULL;
                    UPDATE ""Enquiries"" SET ""EnquiryNumber"" = '' WHERE ""EnquiryNumber"" IS NULL;
                    UPDATE ""Enquiries"" SET ""CompanyName"" = '' WHERE ""CompanyName"" IS NULL;
                END $$;
            ");

            // Make required columns NOT NULL and set defaults (PostgreSQL) - handle idempotency
            migrationBuilder.Sql(@"
                DO $$
                BEGIN
                    BEGIN
                        ALTER TABLE ""Enquiries"" ALTER COLUMN ""EnquiryNumber"" SET NOT NULL;
                    EXCEPTION WHEN OTHERS THEN
                        NULL;
                    END;
                    BEGIN
                        ALTER TABLE ""Enquiries"" ALTER COLUMN ""EnquiryNumber"" SET DEFAULT '';
                    EXCEPTION WHEN OTHERS THEN
                        NULL;
                    END;
                    BEGIN
                        ALTER TABLE ""Enquiries"" ALTER COLUMN ""DateReceived"" SET NOT NULL;
                    EXCEPTION WHEN OTHERS THEN
                        NULL;
                    END;
                    BEGIN
                        ALTER TABLE ""Enquiries"" ALTER COLUMN ""Source"" SET NOT NULL;
                    EXCEPTION WHEN OTHERS THEN
                        NULL;
                    END;
                    BEGIN
                        ALTER TABLE ""Enquiries"" ALTER COLUMN ""Source"" SET DEFAULT 'Manual';
                    EXCEPTION WHEN OTHERS THEN
                        NULL;
                    END;
                    BEGIN
                        ALTER TABLE ""Enquiries"" ALTER COLUMN ""CompanyName"" SET NOT NULL;
                    EXCEPTION WHEN OTHERS THEN
                        NULL;
                    END;
                    BEGIN
                        ALTER TABLE ""Enquiries"" ALTER COLUMN ""CompanyName"" SET DEFAULT '';
                    EXCEPTION WHEN OTHERS THEN
                        NULL;
                    END;
                    BEGIN
                        ALTER TABLE ""Enquiries"" ALTER COLUMN ""Priority"" SET NOT NULL;
                    EXCEPTION WHEN OTHERS THEN
                        NULL;
                    END;
                    BEGIN
                        ALTER TABLE ""Enquiries"" ALTER COLUMN ""Priority"" SET DEFAULT 'Medium';
                    EXCEPTION WHEN OTHERS THEN
                        NULL;
                    END;
                    BEGIN
                        ALTER TABLE ""Enquiries"" ALTER COLUMN ""FeasibilityStatus"" SET NOT NULL;
                    EXCEPTION WHEN OTHERS THEN
                        NULL;
                    END;
                    BEGIN
                        ALTER TABLE ""Enquiries"" ALTER COLUMN ""FeasibilityStatus"" SET DEFAULT 'Pending';
                    EXCEPTION WHEN OTHERS THEN
                        NULL;
                    END;
                END $$;
            ");

            // Drop old columns if they exist
            migrationBuilder.Sql(@"
                DO $$
                BEGIN
                    IF EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name = 'Enquiries' AND column_name = 'ReferenceNumber') THEN
                        ALTER TABLE ""Enquiries"" DROP COLUMN ""ReferenceNumber"";
                    END IF;
                    IF EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name = 'Enquiries' AND column_name = 'CustomerName') THEN
                        ALTER TABLE ""Enquiries"" DROP COLUMN ""CustomerName"";
                    END IF;
                    IF EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name = 'Enquiries' AND column_name = 'Subject') THEN
                        ALTER TABLE ""Enquiries"" DROP COLUMN ""Subject"";
                    END IF;
                    IF EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name = 'Enquiries' AND column_name = 'AssignedToUserId') THEN
                        ALTER TABLE ""Enquiries"" DROP COLUMN ""AssignedToUserId"";
                    END IF;
                END $$;
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Add back old columns if they don't exist
            migrationBuilder.Sql(@"
                DO $$
                BEGIN
                    IF NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name = 'Enquiries' AND column_name = 'ReferenceNumber') THEN
                        ALTER TABLE ""Enquiries"" ADD COLUMN ""ReferenceNumber"" text DEFAULT '';
                    END IF;
                    IF NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name = 'Enquiries' AND column_name = 'CustomerName') THEN
                        ALTER TABLE ""Enquiries"" ADD COLUMN ""CustomerName"" text DEFAULT '';
                    END IF;
                    IF NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name = 'Enquiries' AND column_name = 'Subject') THEN
                        ALTER TABLE ""Enquiries"" ADD COLUMN ""Subject"" text DEFAULT '';
                    END IF;
                    IF NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name = 'Enquiries' AND column_name = 'AssignedToUserId') THEN
                        ALTER TABLE ""Enquiries"" ADD COLUMN ""AssignedToUserId"" uuid;
                    END IF;
                END $$;
            ");

            migrationBuilder.Sql(@"
                DO $$
                BEGIN
                    IF EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name = 'Enquiries' AND column_name = 'EnquiryNumber') THEN
                        UPDATE ""Enquiries"" SET ""ReferenceNumber"" = COALESCE(""EnquiryNumber"", ''), ""CustomerName"" = COALESCE(""CompanyName"", ''), ""Subject"" = COALESCE(""ProductDescription"", '');
                    END IF;
                END $$;
            ");

            // Drop new columns if they exist
            migrationBuilder.Sql(@"
                DO $$
                BEGIN
                    IF EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name = 'Enquiries' AND column_name = 'EnquiryNumber') THEN
                        ALTER TABLE ""Enquiries"" DROP COLUMN ""EnquiryNumber"";
                    END IF;
                    IF EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name = 'Enquiries' AND column_name = 'DateReceived') THEN
                        ALTER TABLE ""Enquiries"" DROP COLUMN ""DateReceived"";
                    END IF;
                    IF EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name = 'Enquiries' AND column_name = 'Source') THEN
                        ALTER TABLE ""Enquiries"" DROP COLUMN ""Source"";
                    END IF;
                    IF EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name = 'Enquiries' AND column_name = 'AssignedTo') THEN
                        ALTER TABLE ""Enquiries"" DROP COLUMN ""AssignedTo"";
                    END IF;
                    IF EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name = 'Enquiries' AND column_name = 'CompanyName') THEN
                        ALTER TABLE ""Enquiries"" DROP COLUMN ""CompanyName"";
                    END IF;
                    IF EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name = 'Enquiries' AND column_name = 'ContactPerson') THEN
                        ALTER TABLE ""Enquiries"" DROP COLUMN ""ContactPerson"";
                    END IF;
                    IF EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name = 'Enquiries' AND column_name = 'Email') THEN
                        ALTER TABLE ""Enquiries"" DROP COLUMN ""Email"";
                    END IF;
                    IF EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name = 'Enquiries' AND column_name = 'Phone') THEN
                        ALTER TABLE ""Enquiries"" DROP COLUMN ""Phone"";
                    END IF;
                    IF EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name = 'Enquiries' AND column_name = 'Gst') THEN
                        ALTER TABLE ""Enquiries"" DROP COLUMN ""Gst"";
                    END IF;
                    IF EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name = 'Enquiries' AND column_name = 'Address') THEN
                        ALTER TABLE ""Enquiries"" DROP COLUMN ""Address"";
                    END IF;
                    IF EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name = 'Enquiries' AND column_name = 'ProductDescription') THEN
                        ALTER TABLE ""Enquiries"" DROP COLUMN ""ProductDescription"";
                    END IF;
                    IF EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name = 'Enquiries' AND column_name = 'Quantity') THEN
                        ALTER TABLE ""Enquiries"" DROP COLUMN ""Quantity"";
                    END IF;
                    IF EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name = 'Enquiries' AND column_name = 'ExpectedDeliveryDate') THEN
                        ALTER TABLE ""Enquiries"" DROP COLUMN ""ExpectedDeliveryDate"";
                    END IF;
                    IF EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name = 'Enquiries' AND column_name = 'AttachmentPath') THEN
                        ALTER TABLE ""Enquiries"" DROP COLUMN ""AttachmentPath"";
                    END IF;
                    IF EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name = 'Enquiries' AND column_name = 'IsAerospace') THEN
                        ALTER TABLE ""Enquiries"" DROP COLUMN ""IsAerospace"";
                    END IF;
                    IF EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name = 'Enquiries' AND column_name = 'Priority') THEN
                        ALTER TABLE ""Enquiries"" DROP COLUMN ""Priority"";
                    END IF;
                    IF EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name = 'Enquiries' AND column_name = 'FeasibilityStatus') THEN
                        ALTER TABLE ""Enquiries"" DROP COLUMN ""FeasibilityStatus"";
                    END IF;
                    IF EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name = 'Enquiries' AND column_name = 'FeasibilityNotes') THEN
                        ALTER TABLE ""Enquiries"" DROP COLUMN ""FeasibilityNotes"";
                    END IF;
                    IF EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name = 'Enquiries' AND column_name = 'CreatedBy') THEN
                        ALTER TABLE ""Enquiries"" DROP COLUMN ""CreatedBy"";
                    END IF;
                    IF EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name = 'Enquiries' AND column_name = 'UpdatedAt') THEN
                        ALTER TABLE ""Enquiries"" DROP COLUMN ""UpdatedAt"";
                    END IF;
                    IF EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name = 'Enquiries' AND column_name = 'IsDeleted') THEN
                        ALTER TABLE ""Enquiries"" DROP COLUMN ""IsDeleted"";
                    END IF;
                END $$;
            ");
        }
    }
}
