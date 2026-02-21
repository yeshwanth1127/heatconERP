using System;
using HeatconERP.Infrastructure.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HeatconERP.Infrastructure.Migrations
{
    /// <inheritdoc />
    [DbContext(typeof(HeatconDbContext))]
    [Migration("20260221130000_AddWorkOrderPurchaseInvoiceLink")]
    public partial class AddWorkOrderPurchaseInvoiceLink : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "PurchaseInvoiceId",
                table: "WorkOrders",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_WorkOrders_PurchaseInvoiceId",
                table: "WorkOrders",
                column: "PurchaseInvoiceId");

            migrationBuilder.AddForeignKey(
                name: "FK_WorkOrders_PurchaseInvoices_PurchaseInvoiceId",
                table: "WorkOrders",
                column: "PurchaseInvoiceId",
                principalTable: "PurchaseInvoices",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_WorkOrders_PurchaseInvoices_PurchaseInvoiceId",
                table: "WorkOrders");

            migrationBuilder.DropIndex(
                name: "IX_WorkOrders_PurchaseInvoiceId",
                table: "WorkOrders");

            migrationBuilder.DropColumn(
                name: "PurchaseInvoiceId",
                table: "WorkOrders");
        }
    }
}

