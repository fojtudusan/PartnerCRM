using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PartnerCRM.Migrations
{
    /// <inheritdoc />
    public partial class AddCountryIsDefault : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsDefault",
                table: "Countries",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsDefault",
                table: "Countries");
        }
    }
}
