using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PartnerCRM.Migrations
{
    /// <inheritdoc />
    public partial class AddCompanyNda : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "NdaDocumentUrl",
                table: "Companies",
                type: "TEXT",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "NdaSigned",
                table: "Companies",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "NdaValidFrom",
                table: "Companies",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "NdaValidTo",
                table: "Companies",
                type: "TEXT",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "NdaDocumentUrl",
                table: "Companies");

            migrationBuilder.DropColumn(
                name: "NdaSigned",
                table: "Companies");

            migrationBuilder.DropColumn(
                name: "NdaValidFrom",
                table: "Companies");

            migrationBuilder.DropColumn(
                name: "NdaValidTo",
                table: "Companies");
        }
    }
}
