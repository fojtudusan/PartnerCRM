using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PartnerCRM.Migrations
{
    /// <inheritdoc />
    public partial class AddCompanyTypes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CompanyTypes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Name = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CompanyTypes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CompanyCompanyType",
                columns: table => new
                {
                    CompaniesId = table.Column<int>(type: "INTEGER", nullable: false),
                    CompanyTypesId = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CompanyCompanyType", x => new { x.CompaniesId, x.CompanyTypesId });
                    table.ForeignKey(
                        name: "FK_CompanyCompanyType_Companies_CompaniesId",
                        column: x => x.CompaniesId,
                        principalTable: "Companies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CompanyCompanyType_CompanyTypes_CompanyTypesId",
                        column: x => x.CompanyTypesId,
                        principalTable: "CompanyTypes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CompanyCompanyType_CompanyTypesId",
                table: "CompanyCompanyType",
                column: "CompanyTypesId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CompanyCompanyType");

            migrationBuilder.DropTable(
                name: "CompanyTypes");
        }
    }
}
