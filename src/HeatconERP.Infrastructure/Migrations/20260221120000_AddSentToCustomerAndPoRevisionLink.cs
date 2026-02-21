using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HeatconERP.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddSentToCustomerAndPoRevisionLink : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "SentToCustomerAt",
                table: "QuotationRevisions",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SentToCustomerBy",
                table: "QuotationRevisions",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "QuotationRevisionId",
                table: "PurchaseOrders",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseOrders_QuotationRevisionId",
                table: "PurchaseOrders",
                column: "QuotationRevisionId");

            migrationBuilder.AddForeignKey(
                name: "FK_PurchaseOrders_QuotationRevisions_QuotationRevisionId",
                table: "PurchaseOrders",
                column: "QuotationRevisionId",
                principalTable: "QuotationRevisions",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_PurchaseOrders_QuotationRevisions_QuotationRevisionId",
                table: "PurchaseOrders");

            migrationBuilder.DropIndex(
                name: "IX_PurchaseOrders_QuotationRevisionId",
                table: "PurchaseOrders");

            migrationBuilder.DropColumn(
                name: "QuotationRevisionId",
                table: "PurchaseOrders");

            migrationBuilder.DropColumn(
                name: "SentToCustomerAt",
                table: "QuotationRevisions");

            migrationBuilder.DropColumn(
                name: "SentToCustomerBy",
                table: "QuotationRevisions");
        }
    }
}
