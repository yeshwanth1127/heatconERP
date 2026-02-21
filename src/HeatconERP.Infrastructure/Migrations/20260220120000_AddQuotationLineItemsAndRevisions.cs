using System;
using HeatconERP.Infrastructure.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HeatconERP.Infrastructure.Migrations
{
    [DbContext(typeof(HeatconDbContext))]
    [Migration("20260220120000_AddQuotationLineItemsAndRevisions")]
    public partial class AddQuotationLineItemsAndRevisions : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(name: "Version", table: "Quotations", type: "text", nullable: false, defaultValue: "v1.0");
            migrationBuilder.AddColumn<string>(name: "ClientName", table: "Quotations", type: "text", nullable: true);
            migrationBuilder.AddColumn<string>(name: "ProjectName", table: "Quotations", type: "text", nullable: true);
            migrationBuilder.AddColumn<string>(name: "CreatedByUserName", table: "Quotations", type: "text", nullable: true);

            migrationBuilder.CreateTable(
                name: "QuotationLineItems",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    QuotationId = table.Column<Guid>(type: "uuid", nullable: false),
                    SortOrder = table.Column<int>(type: "integer", nullable: false),
                    PartNumber = table.Column<string>(type: "text", nullable: false),
                    Description = table.Column<string>(type: "text", nullable: false),
                    Quantity = table.Column<int>(type: "integer", nullable: false),
                    UnitPrice = table.Column<decimal>(type: "numeric", nullable: false),
                    TaxPercent = table.Column<decimal>(type: "numeric", nullable: false),
                    AttachmentPath = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_QuotationLineItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_QuotationLineItems_Quotations_QuotationId",
                        column: x => x.QuotationId,
                        principalTable: "Quotations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "QuotationRevisions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    QuotationId = table.Column<Guid>(type: "uuid", nullable: false),
                    Version = table.Column<string>(type: "text", nullable: false),
                    Action = table.Column<string>(type: "text", nullable: false),
                    ChangedBy = table.Column<string>(type: "text", nullable: false),
                    ChangedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ChangeDetails = table.Column<string>(type: "text", nullable: true),
                    AttachmentPath = table.Column<string>(type: "text", nullable: true),
                    AttachmentFileName = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_QuotationRevisions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_QuotationRevisions_Quotations_QuotationId",
                        column: x => x.QuotationId,
                        principalTable: "Quotations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(name: "IX_QuotationLineItems_QuotationId", table: "QuotationLineItems", column: "QuotationId");
            migrationBuilder.CreateIndex(name: "IX_QuotationRevisions_QuotationId", table: "QuotationRevisions", column: "QuotationId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "QuotationLineItems");
            migrationBuilder.DropTable(name: "QuotationRevisions");
            migrationBuilder.DropColumn(name: "Version", table: "Quotations");
            migrationBuilder.DropColumn(name: "ClientName", table: "Quotations");
            migrationBuilder.DropColumn(name: "ProjectName", table: "Quotations");
            migrationBuilder.DropColumn(name: "CreatedByUserName", table: "Quotations");
        }
    }
}
