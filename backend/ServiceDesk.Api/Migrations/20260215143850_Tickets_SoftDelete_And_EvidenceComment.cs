using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ServiceDesk.Api.Migrations
{
    /// <inheritdoc />
    public partial class Tickets_SoftDelete_And_EvidenceComment : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "DeleteReason",
                table: "Tickets",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAtUtc",
                table: "Tickets",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DeletedBy",
                table: "Tickets",
                type: "nvarchar(120)",
                maxLength: 120,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "Tickets",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "Comment",
                table: "TicketEvidences",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "SortOrder",
                table: "TicketEvidences",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_Tickets_AssignedTo",
                table: "Tickets",
                column: "AssignedTo");

            migrationBuilder.CreateIndex(
                name: "IX_Tickets_CreatedBy",
                table: "Tickets",
                column: "CreatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_Tickets_DeletedAtUtc",
                table: "Tickets",
                column: "DeletedAtUtc");

            migrationBuilder.CreateIndex(
                name: "IX_Tickets_DeletedBy",
                table: "Tickets",
                column: "DeletedBy");

            migrationBuilder.CreateIndex(
                name: "IX_Tickets_IsDeleted",
                table: "Tickets",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_Tickets_UpdatedAtUtc",
                table: "Tickets",
                column: "UpdatedAtUtc");

            migrationBuilder.CreateIndex(
                name: "IX_TicketEvidences_UploadedAtUtc",
                table: "TicketEvidences",
                column: "UploadedAtUtc");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Tickets_AssignedTo",
                table: "Tickets");

            migrationBuilder.DropIndex(
                name: "IX_Tickets_CreatedBy",
                table: "Tickets");

            migrationBuilder.DropIndex(
                name: "IX_Tickets_DeletedAtUtc",
                table: "Tickets");

            migrationBuilder.DropIndex(
                name: "IX_Tickets_DeletedBy",
                table: "Tickets");

            migrationBuilder.DropIndex(
                name: "IX_Tickets_IsDeleted",
                table: "Tickets");

            migrationBuilder.DropIndex(
                name: "IX_Tickets_UpdatedAtUtc",
                table: "Tickets");

            migrationBuilder.DropIndex(
                name: "IX_TicketEvidences_UploadedAtUtc",
                table: "TicketEvidences");

            migrationBuilder.DropColumn(
                name: "DeleteReason",
                table: "Tickets");

            migrationBuilder.DropColumn(
                name: "DeletedAtUtc",
                table: "Tickets");

            migrationBuilder.DropColumn(
                name: "DeletedBy",
                table: "Tickets");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "Tickets");

            migrationBuilder.DropColumn(
                name: "Comment",
                table: "TicketEvidences");

            migrationBuilder.DropColumn(
                name: "SortOrder",
                table: "TicketEvidences");
        }
    }
}
