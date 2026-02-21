using System;
using HeatconERP.Infrastructure.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HeatconERP.Infrastructure.Migrations
{
    [DbContext(typeof(HeatconDbContext))]
    [Migration("20260221150000_AddWorkOrderProductionDispatch")]
    public partial class AddWorkOrderProductionDispatch : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "SentToProductionAt",
                table: "WorkOrders",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SentToProductionBy",
                table: "WorkOrders",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ProductionReceivedAt",
                table: "WorkOrders",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ProductionReceivedBy",
                table: "WorkOrders",
                type: "text",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_WorkOrders_SentToProductionAt",
                table: "WorkOrders",
                column: "SentToProductionAt");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_WorkOrders_SentToProductionAt",
                table: "WorkOrders");

            migrationBuilder.DropColumn(
                name: "SentToProductionAt",
                table: "WorkOrders");

            migrationBuilder.DropColumn(
                name: "SentToProductionBy",
                table: "WorkOrders");

            migrationBuilder.DropColumn(
                name: "ProductionReceivedAt",
                table: "WorkOrders");

            migrationBuilder.DropColumn(
                name: "ProductionReceivedBy",
                table: "WorkOrders");
        }
    }
}


