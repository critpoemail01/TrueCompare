using System;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TrueCompare.Data.Migrations
{
    /// <inheritdoc />
    [DbContext(typeof(ApplicationDbContext))]
    [Migration("20260523184000_AddSearchResultSnapshotsAndAlertEmailRetry")]
    public partial class AddSearchResultSnapshotsAndAlertEmailRetry : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "EmailSuppressedUtc",
                table: "TargetPriceAlerts",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "NextEmailRetryUtc",
                table: "TargetPriceAlerts",
                type: "datetime2",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "SearchResultSnapshots",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    SearchRequestId = table.Column<int>(type: "int", nullable: true),
                    Query = table.Column<string>(type: "nvarchar(800)", maxLength: 800, nullable: false),
                    IdempotencyKey = table.Column<string>(type: "nvarchar(160)", maxLength: 160, nullable: false),
                    ProductsJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    OffersJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ProductOfferSummariesJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    AiSuggestionsJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ResultCount = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    CreatedUtc = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()"),
                    ExpiresUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SearchResultSnapshots", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SearchResultSnapshots_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_SearchResultSnapshots_SearchRequests_SearchRequestId",
                        column: x => x.SearchRequestId,
                        principalTable: "SearchRequests",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.NoAction);
                });

            migrationBuilder.CreateIndex(
                name: "IX_SearchResultSnapshots_ExpiresUtc",
                table: "SearchResultSnapshots",
                column: "ExpiresUtc");

            migrationBuilder.CreateIndex(
                name: "IX_SearchResultSnapshots_SearchRequestId",
                table: "SearchResultSnapshots",
                column: "SearchRequestId");

            migrationBuilder.CreateIndex(
                name: "IX_SearchResultSnapshots_UserId_IdempotencyKey",
                table: "SearchResultSnapshots",
                columns: new[] { "UserId", "IdempotencyKey" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SearchResultSnapshots_UserId_Query_CreatedUtc",
                table: "SearchResultSnapshots",
                columns: new[] { "UserId", "Query", "CreatedUtc" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SearchResultSnapshots");

            migrationBuilder.DropColumn(
                name: "EmailSuppressedUtc",
                table: "TargetPriceAlerts");

            migrationBuilder.DropColumn(
                name: "NextEmailRetryUtc",
                table: "TargetPriceAlerts");
        }
    }
}
