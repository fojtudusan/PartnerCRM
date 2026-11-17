using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PartnerCRM.Migrations
{
    /// <inheritdoc />
    public partial class AddCompanyHierarchy : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ParentCompanyId",
                table: "Companies",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Companies_ParentCompanyId",
                table: "Companies",
                column: "ParentCompanyId");

            migrationBuilder.AddForeignKey(
                name: "FK_Companies_Companies_ParentCompanyId",
                table: "Companies",
                column: "ParentCompanyId",
                principalTable: "Companies",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Companies_Companies_ParentCompanyId",
                table: "Companies");

            migrationBuilder.DropIndex(
                name: "IX_Companies_ParentCompanyId",
                table: "Companies");

            migrationBuilder.DropColumn(
                name: "ParentCompanyId",
                table: "Companies");
        }
    }
}
