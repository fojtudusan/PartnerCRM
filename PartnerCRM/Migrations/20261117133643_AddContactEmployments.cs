using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PartnerCRM.Migrations
{
    /// <inheritdoc />
    public partial class AddContactEmployments : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ContactEmployments",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ContactId = table.Column<int>(type: "INTEGER", nullable: false),
                    CompanyId = table.Column<int>(type: "INTEGER", nullable: false),
                    Position = table.Column<string>(type: "TEXT", maxLength: 150, nullable: true),
                    DateFrom = table.Column<DateTime>(type: "TEXT", nullable: false),
                    DateTo = table.Column<DateTime>(type: "TEXT", nullable: true),
                    IsCurrent = table.Column<bool>(type: "INTEGER", nullable: false),
                    Notes = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ContactEmployments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ContactEmployments_Companies_CompanyId",
                        column: x => x.CompanyId,
                        principalTable: "Companies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ContactEmployments_Contacts_ContactId",
                        column: x => x.ContactId,
                        principalTable: "Contacts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ContactEmployments_CompanyId",
                table: "ContactEmployments",
                column: "CompanyId");

            migrationBuilder.CreateIndex(
                name: "IX_ContactEmployments_ContactId",
                table: "ContactEmployments",
                column: "ContactId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ContactEmployments");
        }
    }
}
