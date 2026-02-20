using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HeatconERP.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class ExpandEnquirySchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Add new columns (nullable first for data migration)
            migrationBuilder.AddColumn<string>(name: "EnquiryNumber", table: "Enquiries", type: "text", nullable: true);
            migrationBuilder.AddColumn<DateTime>(name: "DateReceived", table: "Enquiries", type: "timestamp with time zone", nullable: true);
            migrationBuilder.AddColumn<string>(name: "Source", table: "Enquiries", type: "text", nullable: true);
            migrationBuilder.AddColumn<string>(name: "AssignedTo", table: "Enquiries", type: "text", nullable: true);
            migrationBuilder.AddColumn<string>(name: "CompanyName", table: "Enquiries", type: "text", nullable: true);
            migrationBuilder.AddColumn<string>(name: "ContactPerson", table: "Enquiries", type: "text", nullable: true);
            migrationBuilder.AddColumn<string>(name: "Email", table: "Enquiries", type: "text", nullable: true);
            migrationBuilder.AddColumn<string>(name: "Phone", table: "Enquiries", type: "text", nullable: true);
            migrationBuilder.AddColumn<string>(name: "Gst", table: "Enquiries", type: "text", nullable: true);
            migrationBuilder.AddColumn<string>(name: "Address", table: "Enquiries", type: "text", nullable: true);
            migrationBuilder.AddColumn<string>(name: "ProductDescription", table: "Enquiries", type: "text", nullable: true);
            migrationBuilder.AddColumn<int>(name: "Quantity", table: "Enquiries", type: "integer", nullable: true);
            migrationBuilder.AddColumn<DateTime>(name: "ExpectedDeliveryDate", table: "Enquiries", type: "timestamp with time zone", nullable: true);
            migrationBuilder.AddColumn<string>(name: "AttachmentPath", table: "Enquiries", type: "text", nullable: true);
            migrationBuilder.AddColumn<bool>(name: "IsAerospace", table: "Enquiries", type: "boolean", nullable: false, defaultValue: false);
            migrationBuilder.AddColumn<string>(name: "Priority", table: "Enquiries", type: "text", nullable: true);
            migrationBuilder.AddColumn<string>(name: "FeasibilityStatus", table: "Enquiries", type: "text", nullable: true);
            migrationBuilder.AddColumn<string>(name: "FeasibilityNotes", table: "Enquiries", type: "text", nullable: true);
            migrationBuilder.AddColumn<string>(name: "CreatedBy", table: "Enquiries", type: "text", nullable: true);
            migrationBuilder.AddColumn<DateTime?>(name: "UpdatedAt", table: "Enquiries", type: "timestamp with time zone", nullable: true);
            migrationBuilder.AddColumn<bool>(name: "IsDeleted", table: "Enquiries", type: "boolean", nullable: false, defaultValue: false);

            // Migrate data from old columns
            migrationBuilder.Sql(@"
                UPDATE ""Enquiries"" SET ""EnquiryNumber"" = COALESCE(""ReferenceNumber"", '') WHERE ""EnquiryNumber"" IS NULL;
                UPDATE ""Enquiries"" SET ""CompanyName"" = COALESCE(""CustomerName"", '') WHERE ""CompanyName"" IS NULL;
                UPDATE ""Enquiries"" SET ""ProductDescription"" = COALESCE(""Subject"", '') WHERE ""ProductDescription"" IS NULL;
                UPDATE ""Enquiries"" SET ""DateReceived"" = ""CreatedAt"" WHERE ""DateReceived"" IS NULL;
                UPDATE ""Enquiries"" SET ""Source"" = 'Manual' WHERE ""Source"" IS NULL;
                UPDATE ""Enquiries"" SET ""Status"" = CASE WHEN ""Status"" = 'Open' THEN 'Under Review' WHEN ""Status"" = 'Closed' THEN 'Closed' ELSE COALESCE(""Status"", 'New') END;
                UPDATE ""Enquiries"" SET ""Priority"" = 'Medium' WHERE ""Priority"" IS NULL;
                UPDATE ""Enquiries"" SET ""FeasibilityStatus"" = 'Pending' WHERE ""FeasibilityStatus"" IS NULL;
                UPDATE ""Enquiries"" SET ""EnquiryNumber"" = '' WHERE ""EnquiryNumber"" IS NULL;
                UPDATE ""Enquiries"" SET ""CompanyName"" = '' WHERE ""CompanyName"" IS NULL;
                UPDATE ""Enquiries"" SET ""DateReceived"" = ""CreatedAt"" WHERE ""DateReceived"" IS NULL;
            ");

            // Make required columns NOT NULL (PostgreSQL)
            migrationBuilder.Sql(@"
                ALTER TABLE ""Enquiries"" ALTER COLUMN ""EnquiryNumber"" SET NOT NULL;
                ALTER TABLE ""Enquiries"" ALTER COLUMN ""EnquiryNumber"" SET DEFAULT '';
                ALTER TABLE ""Enquiries"" ALTER COLUMN ""DateReceived"" SET NOT NULL;
                ALTER TABLE ""Enquiries"" ALTER COLUMN ""Source"" SET NOT NULL;
                ALTER TABLE ""Enquiries"" ALTER COLUMN ""Source"" SET DEFAULT 'Manual';
                ALTER TABLE ""Enquiries"" ALTER COLUMN ""CompanyName"" SET NOT NULL;
                ALTER TABLE ""Enquiries"" ALTER COLUMN ""CompanyName"" SET DEFAULT '';
                ALTER TABLE ""Enquiries"" ALTER COLUMN ""Priority"" SET NOT NULL;
                ALTER TABLE ""Enquiries"" ALTER COLUMN ""Priority"" SET DEFAULT 'Medium';
                ALTER TABLE ""Enquiries"" ALTER COLUMN ""FeasibilityStatus"" SET NOT NULL;
                ALTER TABLE ""Enquiries"" ALTER COLUMN ""FeasibilityStatus"" SET DEFAULT 'Pending';
            ");

            // Drop old columns
            migrationBuilder.DropColumn(name: "ReferenceNumber", table: "Enquiries");
            migrationBuilder.DropColumn(name: "CustomerName", table: "Enquiries");
            migrationBuilder.DropColumn(name: "Subject", table: "Enquiries");
            migrationBuilder.DropColumn(name: "AssignedToUserId", table: "Enquiries");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(name: "ReferenceNumber", table: "Enquiries", type: "text", nullable: false, defaultValue: "");
            migrationBuilder.AddColumn<string>(name: "CustomerName", table: "Enquiries", type: "text", nullable: false, defaultValue: "");
            migrationBuilder.AddColumn<string>(name: "Subject", table: "Enquiries", type: "text", nullable: false, defaultValue: "");
            migrationBuilder.AddColumn<Guid?>(name: "AssignedToUserId", table: "Enquiries", type: "uuid", nullable: true);

            migrationBuilder.Sql(@"
                UPDATE ""Enquiries"" SET ""ReferenceNumber"" = ""EnquiryNumber"", ""CustomerName"" = ""CompanyName"", ""Subject"" = COALESCE(""ProductDescription"", '');
            ");

            migrationBuilder.DropColumn(name: "EnquiryNumber", table: "Enquiries");
            migrationBuilder.DropColumn(name: "DateReceived", table: "Enquiries");
            migrationBuilder.DropColumn(name: "Source", table: "Enquiries");
            migrationBuilder.DropColumn(name: "AssignedTo", table: "Enquiries");
            migrationBuilder.DropColumn(name: "CompanyName", table: "Enquiries");
            migrationBuilder.DropColumn(name: "ContactPerson", table: "Enquiries");
            migrationBuilder.DropColumn(name: "Email", table: "Enquiries");
            migrationBuilder.DropColumn(name: "Phone", table: "Enquiries");
            migrationBuilder.DropColumn(name: "Gst", table: "Enquiries");
            migrationBuilder.DropColumn(name: "Address", table: "Enquiries");
            migrationBuilder.DropColumn(name: "ProductDescription", table: "Enquiries");
            migrationBuilder.DropColumn(name: "Quantity", table: "Enquiries");
            migrationBuilder.DropColumn(name: "ExpectedDeliveryDate", table: "Enquiries");
            migrationBuilder.DropColumn(name: "AttachmentPath", table: "Enquiries");
            migrationBuilder.DropColumn(name: "IsAerospace", table: "Enquiries");
            migrationBuilder.DropColumn(name: "Priority", table: "Enquiries");
            migrationBuilder.DropColumn(name: "FeasibilityStatus", table: "Enquiries");
            migrationBuilder.DropColumn(name: "FeasibilityNotes", table: "Enquiries");
            migrationBuilder.DropColumn(name: "CreatedBy", table: "Enquiries");
            migrationBuilder.DropColumn(name: "UpdatedAt", table: "Enquiries");
            migrationBuilder.DropColumn(name: "IsDeleted", table: "Enquiries");
        }
    }
}
