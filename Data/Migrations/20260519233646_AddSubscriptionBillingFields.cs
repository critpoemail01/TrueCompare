using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TrueCompare.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddSubscriptionBillingFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "BillingKind",
                table: "CreditPurchases",
                type: "nvarchar(40)",
                maxLength: 40,
                nullable: false,
                defaultValue: "one_time");

            migrationBuilder.AddColumn<string>(
                name: "BillingPeriod",
                table: "CreditPurchases",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PlanId",
                table: "CreditPurchases",
                type: "nvarchar(80)",
                maxLength: 80,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "StripeSubscriptionId",
                table: "CreditPurchases",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "BillingKind",
                table: "CreditPurchases");

            migrationBuilder.DropColumn(
                name: "BillingPeriod",
                table: "CreditPurchases");

            migrationBuilder.DropColumn(
                name: "PlanId",
                table: "CreditPurchases");

            migrationBuilder.DropColumn(
                name: "StripeSubscriptionId",
                table: "CreditPurchases");
        }
    }
}
