using HeatconERP.Infrastructure.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HeatconERP.Infrastructure.Migrations;

[DbContext(typeof(HeatconDbContext))]
[Migration("20260222123000_AddQuotationPricingFields")]
public partial class AddQuotationPricingFields : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<decimal>(name: "ManualPrice", table: "Quotations", type: "numeric", nullable: true);
        migrationBuilder.AddColumn<string>(name: "PriceBreakdown", table: "Quotations", type: "text", nullable: true);

        migrationBuilder.AddColumn<decimal>(name: "SnapshotManualPrice", table: "QuotationRevisions", type: "numeric", nullable: true);
        migrationBuilder.AddColumn<string>(name: "SnapshotPriceBreakdown", table: "QuotationRevisions", type: "text", nullable: true);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(name: "ManualPrice", table: "Quotations");
        migrationBuilder.DropColumn(name: "PriceBreakdown", table: "Quotations");
        migrationBuilder.DropColumn(name: "SnapshotManualPrice", table: "QuotationRevisions");
        migrationBuilder.DropColumn(name: "SnapshotPriceBreakdown", table: "QuotationRevisions");
    }
}


