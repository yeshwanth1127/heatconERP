using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HeatconERP.Infrastructure.Migrations;

public partial class AddQuotationDescriptionAndAttachments : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>(name: "Description", table: "Quotations", type: "text", nullable: true);
        migrationBuilder.AddColumn<string>(name: "Attachments", table: "Quotations", type: "text", nullable: true);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(name: "Description", table: "Quotations");
        migrationBuilder.DropColumn(name: "Attachments", table: "Quotations");
    }
}
