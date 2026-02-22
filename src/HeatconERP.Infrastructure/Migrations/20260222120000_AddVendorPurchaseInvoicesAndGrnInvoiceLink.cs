using System;
using HeatconERP.Infrastructure.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HeatconERP.Infrastructure.Migrations
{
    [DbContext(typeof(HeatconDbContext))]
    [Migration("20260222120000_AddVendorPurchaseInvoicesAndGrnInvoiceLink")]
    public partial class AddVendorPurchaseInvoicesAndGrnInvoiceLink : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "VendorPurchaseInvoiceId",
                table: "GRNs",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "VendorPurchaseInvoices",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    VendorPurchaseOrderId = table.Column<Guid>(type: "uuid", nullable: false),
                    VendorId = table.Column<Guid>(type: "uuid", nullable: false),
                    InvoiceNumber = table.Column<string>(type: "text", nullable: false),
                    InvoiceDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Status = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    RowVersion = table.Column<byte[]>(type: "bytea", nullable: false, defaultValue: new byte[0])
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VendorPurchaseInvoices", x => x.Id);
                    table.ForeignKey(
                        name: "FK_VendorPurchaseInvoices_VendorPurchaseOrders_VendorPurchaseOrderId",
                        column: x => x.VendorPurchaseOrderId,
                        principalTable: "VendorPurchaseOrders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_VendorPurchaseInvoices_Vendors_VendorId",
                        column: x => x.VendorId,
                        principalTable: "Vendors",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_GRNs_VendorPurchaseInvoiceId",
                table: "GRNs",
                column: "VendorPurchaseInvoiceId");

            migrationBuilder.CreateIndex(
                name: "IX_VendorPurchaseInvoices_VendorPurchaseOrderId",
                table: "VendorPurchaseInvoices",
                column: "VendorPurchaseOrderId");

            migrationBuilder.CreateIndex(
                name: "IX_VendorPurchaseInvoices_VendorId",
                table: "VendorPurchaseInvoices",
                column: "VendorId");

            migrationBuilder.CreateIndex(
                name: "IX_VendorPurchaseInvoices_VendorId_InvoiceNumber",
                table: "VendorPurchaseInvoices",
                columns: new[] { "VendorId", "InvoiceNumber" },
                unique: true);

            migrationBuilder.CreateTable(
                name: "VendorPurchaseInvoiceLineItems",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    VendorPurchaseInvoiceId = table.Column<Guid>(type: "uuid", nullable: false),
                    MaterialVariantId = table.Column<Guid>(type: "uuid", nullable: false),
                    Quantity = table.Column<decimal>(type: "numeric", nullable: false),
                    UnitPrice = table.Column<decimal>(type: "numeric", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    RowVersion = table.Column<byte[]>(type: "bytea", nullable: false, defaultValue: new byte[0])
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VendorPurchaseInvoiceLineItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_VendorPurchaseInvoiceLineItems_VendorPurchaseInvoices_VendorPurchaseInvoiceId",
                        column: x => x.VendorPurchaseInvoiceId,
                        principalTable: "VendorPurchaseInvoices",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_VendorPurchaseInvoiceLineItems_MaterialVariants_MaterialVariantId",
                        column: x => x.MaterialVariantId,
                        principalTable: "MaterialVariants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_VendorPurchaseInvoiceLineItems_VendorPurchaseInvoiceId",
                table: "VendorPurchaseInvoiceLineItems",
                column: "VendorPurchaseInvoiceId");

            migrationBuilder.CreateIndex(
                name: "IX_VendorPurchaseInvoiceLineItems_MaterialVariantId",
                table: "VendorPurchaseInvoiceLineItems",
                column: "MaterialVariantId");

            migrationBuilder.AddForeignKey(
                name: "FK_GRNs_VendorPurchaseInvoices_VendorPurchaseInvoiceId",
                table: "GRNs",
                column: "VendorPurchaseInvoiceId",
                principalTable: "VendorPurchaseInvoices",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_GRNs_VendorPurchaseInvoices_VendorPurchaseInvoiceId",
                table: "GRNs");

            migrationBuilder.DropTable(
                name: "VendorPurchaseInvoiceLineItems");

            migrationBuilder.DropTable(
                name: "VendorPurchaseInvoices");

            migrationBuilder.DropIndex(
                name: "IX_GRNs_VendorPurchaseInvoiceId",
                table: "GRNs");

            migrationBuilder.DropColumn(
                name: "VendorPurchaseInvoiceId",
                table: "GRNs");
        }
    }
}


