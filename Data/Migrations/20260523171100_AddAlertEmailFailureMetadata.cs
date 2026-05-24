using System;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TrueCompare.Data.Migrations
{
    /// <inheritdoc />
    [DbContext(typeof(ApplicationDbContext))]
    [Migration("20260523171100_AddAlertEmailFailureMetadata")]
    public partial class AddAlertEmailFailureMetadata : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "EmailFailureCount",
                table: "TargetPriceAlerts",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTime>(
                name: "LastEmailAttemptUtc",
                table: "TargetPriceAlerts",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LastEmailError",
                table: "TargetPriceAlerts",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "EmailFailureCount",
                table: "TargetPriceAlerts");

            migrationBuilder.DropColumn(
                name: "LastEmailAttemptUtc",
                table: "TargetPriceAlerts");

            migrationBuilder.DropColumn(
                name: "LastEmailError",
                table: "TargetPriceAlerts");
        }
    }
}
