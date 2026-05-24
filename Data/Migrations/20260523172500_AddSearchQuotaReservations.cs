using System;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TrueCompare.Data.Migrations
{
    /// <inheritdoc />
    [DbContext(typeof(ApplicationDbContext))]
    [Migration("20260523172500_AddSearchQuotaReservations")]
    public partial class AddSearchQuotaReservations : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "CommittedUtc",
                table: "SearchRequests",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "FailureReason",
                table: "SearchRequests",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "IdempotencyKey",
                table: "SearchRequests",
                type: "nvarchar(160)",
                maxLength: 160,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "RefundedUtc",
                table: "SearchRequests",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ResultCount",
                table: "SearchRequests",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "Status",
                table: "SearchRequests",
                type: "nvarchar(40)",
                maxLength: 40,
                nullable: false,
                defaultValue: "Committed");

            migrationBuilder.AddColumn<bool>(
                name: "UsedFreeSearch",
                table: "SearchRequests",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.Sql("UPDATE SearchRequests SET Status = 'Committed', CommittedUtc = CreatedUtc, UsedFreeSearch = CASE WHEN UsedPaidCredit = 0 THEN 1 ELSE 0 END WHERE Status IS NULL OR Status = 'Committed'");

            migrationBuilder.CreateIndex(
                name: "IX_SearchRequests_UserId_IdempotencyKey",
                table: "SearchRequests",
                columns: new[] { "UserId", "IdempotencyKey" },
                unique: true,
                filter: "[IdempotencyKey] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_SearchRequests_UserId_Status_CreatedUtc",
                table: "SearchRequests",
                columns: new[] { "UserId", "Status", "CreatedUtc" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_SearchRequests_UserId_IdempotencyKey",
                table: "SearchRequests");

            migrationBuilder.DropIndex(
                name: "IX_SearchRequests_UserId_Status_CreatedUtc",
                table: "SearchRequests");

            migrationBuilder.DropColumn(
                name: "CommittedUtc",
                table: "SearchRequests");

            migrationBuilder.DropColumn(
                name: "FailureReason",
                table: "SearchRequests");

            migrationBuilder.DropColumn(
                name: "IdempotencyKey",
                table: "SearchRequests");

            migrationBuilder.DropColumn(
                name: "RefundedUtc",
                table: "SearchRequests");

            migrationBuilder.DropColumn(
                name: "ResultCount",
                table: "SearchRequests");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "SearchRequests");

            migrationBuilder.DropColumn(
                name: "UsedFreeSearch",
                table: "SearchRequests");
        }
    }
}
