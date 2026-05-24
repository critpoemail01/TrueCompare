using System;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TrueCompare.Data.Migrations
{
    /// <inheritdoc />
    [DbContext(typeof(ApplicationDbContext))]
    [Migration("20260523164000_AddAlertValidationMetadata")]
    public partial class AddAlertValidationMetadata : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "LastValidationState",
                table: "TargetPriceAlerts",
                type: "nvarchar(40)",
                maxLength: 40,
                nullable: false,
                defaultValue: "CatalogKnown");

            migrationBuilder.AddColumn<DateTime>(
                name: "LastValidatedUtc",
                table: "TargetPriceAlerts",
                type: "datetime2",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "LastValidationState",
                table: "TargetPriceAlerts");

            migrationBuilder.DropColumn(
                name: "LastValidatedUtc",
                table: "TargetPriceAlerts");
        }
    }
}
