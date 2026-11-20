using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BloodLink.Data.Migrations
{
    /// <inheritdoc />
    public partial class DonationRecordEmails : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "DonorEmail",
                table: "DonationRecords",
                type: "nvarchar(256)",
                maxLength: 256,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RecipientEmail",
                table: "DonationRecords",
                type: "nvarchar(256)",
                maxLength: 256,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DonorEmail",
                table: "DonationRecords");

            migrationBuilder.DropColumn(
                name: "RecipientEmail",
                table: "DonationRecords");
        }
    }
}
