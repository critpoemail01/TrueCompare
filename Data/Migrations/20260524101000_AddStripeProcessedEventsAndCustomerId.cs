using System;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TrueCompare.Data.Migrations
{
    /// <inheritdoc />
    [DbContext(typeof(ApplicationDbContext))]
    [Migration("20260524101000_AddStripeProcessedEventsAndCustomerId")]
    public partial class AddStripeProcessedEventsAndCustomerId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "StripeCustomerId",
                table: "AspNetUsers",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "StripeProcessedEvents",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    StripeEventId = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    EventType = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    Error = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CreatedUtc = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()"),
                    ProcessedUtc = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StripeProcessedEvents", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUsers_StripeCustomerId",
                table: "AspNetUsers",
                column: "StripeCustomerId");

            migrationBuilder.CreateIndex(
                name: "IX_StripeProcessedEvents_Status_CreatedUtc",
                table: "StripeProcessedEvents",
                columns: new[] { "Status", "CreatedUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_StripeProcessedEvents_StripeEventId",
                table: "StripeProcessedEvents",
                column: "StripeEventId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "StripeProcessedEvents");

            migrationBuilder.DropIndex(
                name: "IX_AspNetUsers_StripeCustomerId",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "StripeCustomerId",
                table: "AspNetUsers");
        }
    }
}
