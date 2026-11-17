using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PartnerCRM.Migrations
{
    /// <inheritdoc />
    public partial class AddContactMergeHistory : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsMerged",
                table: "Contacts",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "MergedAt",
                table: "Contacts",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "MergedIntoContactId",
                table: "Contacts",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Contacts_MergedIntoContactId",
                table: "Contacts",
                column: "MergedIntoContactId");

            migrationBuilder.AddForeignKey(
                name: "FK_Contacts_Contacts_MergedIntoContactId",
                table: "Contacts",
                column: "MergedIntoContactId",
                principalTable: "Contacts",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Contacts_Contacts_MergedIntoContactId",
                table: "Contacts");

            migrationBuilder.DropIndex(
                name: "IX_Contacts_MergedIntoContactId",
                table: "Contacts");

            migrationBuilder.DropColumn(
                name: "IsMerged",
                table: "Contacts");

            migrationBuilder.DropColumn(
                name: "MergedAt",
                table: "Contacts");

            migrationBuilder.DropColumn(
                name: "MergedIntoContactId",
                table: "Contacts");
        }
    }
}
