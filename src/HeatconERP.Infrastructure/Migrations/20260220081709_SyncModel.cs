using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HeatconERP.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class SyncModel : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_Quotations_EnquiryId",
                table: "Quotations",
                column: "EnquiryId");

            migrationBuilder.AddForeignKey(
                name: "FK_Quotations_Enquiries_EnquiryId",
                table: "Quotations",
                column: "EnquiryId",
                principalTable: "Enquiries",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Quotations_Enquiries_EnquiryId",
                table: "Quotations");

            migrationBuilder.DropIndex(
                name: "IX_Quotations_EnquiryId",
                table: "Quotations");
        }
    }
}
