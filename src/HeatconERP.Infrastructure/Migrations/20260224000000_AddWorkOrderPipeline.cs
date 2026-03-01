using System;
using HeatconERP.Infrastructure.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HeatconERP.Infrastructure.Migrations
{
    [DbContext(typeof(HeatconDbContext))]
    [Migration("20260224000000_AddWorkOrderPipeline")]
    public partial class AddWorkOrderPipeline : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "WorkStartedAt",
                table: "WorkOrders",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "WorkStartedBy",
                table: "WorkOrders",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "WorkCompletedAt",
                table: "WorkOrders",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "WorkCompletedBy",
                table: "WorkOrders",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "PlanningCompletedAt",
                table: "WorkOrders",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "MaterialCompletedAt",
                table: "WorkOrders",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "AssemblyCompletedAt",
                table: "WorkOrders",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "TestingCompletedAt",
                table: "WorkOrders",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "QcCompletedAt",
                table: "WorkOrders",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "PackingCompletedAt",
                table: "WorkOrders",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_WorkOrders_WorkStartedAt",
                table: "WorkOrders",
                column: "WorkStartedAt");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_WorkOrders_WorkStartedAt",
                table: "WorkOrders");

            migrationBuilder.DropColumn(
                name: "WorkStartedAt",
                table: "WorkOrders");

            migrationBuilder.DropColumn(
                name: "WorkStartedBy",
                table: "WorkOrders");

            migrationBuilder.DropColumn(
                name: "WorkCompletedAt",
                table: "WorkOrders");

            migrationBuilder.DropColumn(
                name: "WorkCompletedBy",
                table: "WorkOrders");

            migrationBuilder.DropColumn(
                name: "PlanningCompletedAt",
                table: "WorkOrders");

            migrationBuilder.DropColumn(
                name: "MaterialCompletedAt",
                table: "WorkOrders");

            migrationBuilder.DropColumn(
                name: "AssemblyCompletedAt",
                table: "WorkOrders");

            migrationBuilder.DropColumn(
                name: "TestingCompletedAt",
                table: "WorkOrders");

            migrationBuilder.DropColumn(
                name: "QcCompletedAt",
                table: "WorkOrders");

            migrationBuilder.DropColumn(
                name: "PackingCompletedAt",
                table: "WorkOrders");
        }
    }
}
