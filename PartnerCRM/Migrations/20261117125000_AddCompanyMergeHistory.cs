using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PartnerCRM.Migrations
{
    /// <inheritdoc />
    public partial class AddCompanyMergeHistory : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsMerged",
                table: "Companies",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "MergedAt",
                table: "Companies",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "MergedIntoCompanyId",
                table: "Companies",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Companies_MergedIntoCompanyId",
                table: "Companies",
                column: "MergedIntoCompanyId");

            migrationBuilder.AddForeignKey(
                name: "FK_Companies_Companies_MergedIntoCompanyId",
                table: "Companies",
                column: "MergedIntoCompanyId",
                principalTable: "Companies",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Companies_Companies_MergedIntoCompanyId",
                table: "Companies");

            migrationBuilder.DropIndex(
                name: "IX_Companies_MergedIntoCompanyId",
                table: "Companies");

            migrationBuilder.DropColumn(
                name: "IsMerged",
                table: "Companies");

            migrationBuilder.DropColumn(
                name: "MergedAt",
                table: "Companies");

            migrationBuilder.DropColumn(
                name: "MergedIntoCompanyId",
                table: "Companies");
        }
    }
}
