using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PartnerCRM.Migrations
{
    /// <inheritdoc />
    public partial class ExtendCompanyDetails : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AccountNumber",
                table: "Companies",
                type: "TEXT",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "BankId",
                table: "Companies",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "CityId",
                table: "Companies",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "CountryId",
                table: "Companies",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Email",
                table: "Companies",
                type: "TEXT",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Fax",
                table: "Companies",
                type: "TEXT",
                maxLength: 30,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Phone",
                table: "Companies",
                type: "TEXT",
                maxLength: 30,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Companies_BankId",
                table: "Companies",
                column: "BankId");

            migrationBuilder.CreateIndex(
                name: "IX_Companies_CityId",
                table: "Companies",
                column: "CityId");

            migrationBuilder.CreateIndex(
                name: "IX_Companies_CountryId",
                table: "Companies",
                column: "CountryId");

            migrationBuilder.AddForeignKey(
                name: "FK_Companies_Banks_BankId",
                table: "Companies",
                column: "BankId",
                principalTable: "Banks",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Companies_Cities_CityId",
                table: "Companies",
                column: "CityId",
                principalTable: "Cities",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Companies_Countries_CountryId",
                table: "Companies",
                column: "CountryId",
                principalTable: "Countries",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Companies_Banks_BankId",
                table: "Companies");

            migrationBuilder.DropForeignKey(
                name: "FK_Companies_Cities_CityId",
                table: "Companies");

            migrationBuilder.DropForeignKey(
                name: "FK_Companies_Countries_CountryId",
                table: "Companies");

            migrationBuilder.DropIndex(
                name: "IX_Companies_BankId",
                table: "Companies");

            migrationBuilder.DropIndex(
                name: "IX_Companies_CityId",
                table: "Companies");

            migrationBuilder.DropIndex(
                name: "IX_Companies_CountryId",
                table: "Companies");

            migrationBuilder.DropColumn(
                name: "AccountNumber",
                table: "Companies");

            migrationBuilder.DropColumn(
                name: "BankId",
                table: "Companies");

            migrationBuilder.DropColumn(
                name: "CityId",
                table: "Companies");

            migrationBuilder.DropColumn(
                name: "CountryId",
                table: "Companies");

            migrationBuilder.DropColumn(
                name: "Email",
                table: "Companies");

            migrationBuilder.DropColumn(
                name: "Fax",
                table: "Companies");

            migrationBuilder.DropColumn(
                name: "Phone",
                table: "Companies");
        }
    }
}
