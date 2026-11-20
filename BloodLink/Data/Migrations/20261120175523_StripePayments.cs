using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BloodLink.Data.Migrations
{
    /// <inheritdoc />
    public partial class StripePayments : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "PaymentIntentId",
                table: "HallBookings",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "AwaitingPayment",
                table: "Advertisements",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "PaymentIntentId",
                table: "Advertisements",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PaymentIntentId",
                table: "HallBookings");

            migrationBuilder.DropColumn(
                name: "AwaitingPayment",
                table: "Advertisements");

            migrationBuilder.DropColumn(
                name: "PaymentIntentId",
                table: "Advertisements");
        }
    }
}
