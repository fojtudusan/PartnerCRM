using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PartnerCRM.Migrations
{
    /// <inheritdoc />
    public partial class SplitCompanyAddress : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Address",
                table: "Companies");

            migrationBuilder.AddColumn<string>(
                name: "HouseNumber",
                table: "Companies",
                type: "TEXT",
                maxLength: 30,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Street",
                table: "Companies",
                type: "TEXT",
                maxLength: 150,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "HouseNumber",
                table: "Companies");

            migrationBuilder.DropColumn(
                name: "Street",
                table: "Companies");

            migrationBuilder.AddColumn<string>(
                name: "Address",
                table: "Companies",
                type: "TEXT",
                maxLength: 300,
                nullable: true);
        }
    }
}
