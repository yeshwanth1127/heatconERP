using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HeatconERP.Infrastructure.Migrations;

public partial class AddQuotationRevisionSnapshot : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>(name: "SnapshotClientName", table: "QuotationRevisions", type: "text", nullable: true);
        migrationBuilder.AddColumn<string>(name: "SnapshotProjectName", table: "QuotationRevisions", type: "text", nullable: true);
        migrationBuilder.AddColumn<string>(name: "SnapshotDescription", table: "QuotationRevisions", type: "text", nullable: true);
        migrationBuilder.AddColumn<string>(name: "SnapshotAttachments", table: "QuotationRevisions", type: "text", nullable: true);
        migrationBuilder.AddColumn<string>(name: "SnapshotStatus", table: "QuotationRevisions", type: "text", nullable: true);
        migrationBuilder.AddColumn<decimal>(name: "SnapshotAmount", table: "QuotationRevisions", type: "numeric", nullable: true);
        migrationBuilder.AddColumn<string>(name: "SnapshotLineItemsJson", table: "QuotationRevisions", type: "text", nullable: true);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(name: "SnapshotClientName", table: "QuotationRevisions");
        migrationBuilder.DropColumn(name: "SnapshotProjectName", table: "QuotationRevisions");
        migrationBuilder.DropColumn(name: "SnapshotDescription", table: "QuotationRevisions");
        migrationBuilder.DropColumn(name: "SnapshotAttachments", table: "QuotationRevisions");
        migrationBuilder.DropColumn(name: "SnapshotStatus", table: "QuotationRevisions");
        migrationBuilder.DropColumn(name: "SnapshotAmount", table: "QuotationRevisions");
        migrationBuilder.DropColumn(name: "SnapshotLineItemsJson", table: "QuotationRevisions");
    }
}
