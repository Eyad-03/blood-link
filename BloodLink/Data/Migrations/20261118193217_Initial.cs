using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace BloodLink.Data.Migrations
{
    /// <inheritdoc />
    public partial class Initial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Halls",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    Governorate = table.Column<string>(type: "nvarchar(60)", maxLength: 60, nullable: false),
                    EntityType = table.Column<string>(type: "nvarchar(60)", maxLength: 60, nullable: true),
                    Location = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    Capacity = table.Column<int>(type: "int", nullable: false),
                    Price = table.Column<decimal>(type: "decimal(10,2)", precision: 10, scale: 2, nullable: false),
                    Rating = table.Column<decimal>(type: "decimal(10,2)", precision: 10, scale: 2, nullable: false),
                    Tag = table.Column<string>(type: "nvarchar(60)", maxLength: 60, nullable: true),
                    ImageUrl = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Description = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    SupportedEvents = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    Features = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Halls", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Organizations",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    Type = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    City = table.Column<string>(type: "nvarchar(60)", maxLength: 60, nullable: false),
                    Address = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    LicenseNumber = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    Email = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    Phone = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: true),
                    ContactPerson = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    OpenTime = table.Column<TimeOnly>(type: "time", nullable: false),
                    CloseTime = table.Column<TimeOnly>(type: "time", nullable: false),
                    SlotCapacity = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    RejectionReason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    RegisteredAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    DecidedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Organizations", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Advertisements",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    OrganizationId = table.Column<int>(type: "int", nullable: false),
                    Title = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    Department = table.Column<string>(type: "nvarchar(60)", maxLength: 60, nullable: false),
                    Category = table.Column<string>(type: "nvarchar(60)", maxLength: 60, nullable: false),
                    Badge = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: true),
                    Description = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    EventDate = table.Column<DateOnly>(type: "date", nullable: false),
                    StartTime = table.Column<TimeOnly>(type: "time", nullable: true),
                    EndTime = table.Column<TimeOnly>(type: "time", nullable: true),
                    Location = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    Phone = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: true),
                    BookingRequired = table.Column<bool>(type: "bit", nullable: false),
                    Plan = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    StartDate = table.Column<DateOnly>(type: "date", nullable: false),
                    EndDate = table.Column<DateOnly>(type: "date", nullable: false),
                    Fee = table.Column<decimal>(type: "decimal(10,2)", precision: 10, scale: 2, nullable: false),
                    Vat = table.Column<decimal>(type: "decimal(10,2)", precision: 10, scale: 2, nullable: false),
                    TotalPaid = table.Column<decimal>(type: "decimal(10,2)", precision: 10, scale: 2, nullable: false),
                    Views = table.Column<int>(type: "int", nullable: false),
                    IsCancelled = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Advertisements", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Advertisements_Organizations_OrganizationId",
                        column: x => x.OrganizationId,
                        principalTable: "Organizations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "BloodInventory",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    OrganizationId = table.Column<int>(type: "int", nullable: false),
                    BloodType = table.Column<string>(type: "nvarchar(3)", maxLength: 3, nullable: false),
                    Units = table.Column<int>(type: "int", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BloodInventory", x => x.Id);
                    table.CheckConstraint("CK_BloodInventory_Units", "[Units] >= 0");
                    table.ForeignKey(
                        name: "FK_BloodInventory_Organizations_OrganizationId",
                        column: x => x.OrganizationId,
                        principalTable: "Organizations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "OrganizationDocuments",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    OrganizationId = table.Column<int>(type: "int", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    FilePath = table.Column<string>(type: "nvarchar(260)", maxLength: 260, nullable: false),
                    FileSizeBytes = table.Column<long>(type: "bigint", nullable: true),
                    IsChecked = table.Column<bool>(type: "bit", nullable: false),
                    UploadedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OrganizationDocuments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OrganizationDocuments_Organizations_OrganizationId",
                        column: x => x.OrganizationId,
                        principalTable: "Organizations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Users",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    FullName = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    Email = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    PasswordHash = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    Phone = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: true),
                    Role = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    BloodType = table.Column<string>(type: "nvarchar(3)", maxLength: 3, nullable: true),
                    City = table.Column<string>(type: "nvarchar(60)", maxLength: 60, nullable: true),
                    Area = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: true),
                    Latitude = table.Column<double>(type: "float", nullable: true),
                    Longitude = table.Column<double>(type: "float", nullable: true),
                    ShareLocation = table.Column<bool>(type: "bit", nullable: false),
                    IsAvailable = table.Column<bool>(type: "bit", nullable: false),
                    PreferredContactMethod = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    LastDonationDate = table.Column<DateOnly>(type: "date", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    LastActiveAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    OrganizationId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Users", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Users_Organizations_OrganizationId",
                        column: x => x.OrganizationId,
                        principalTable: "Organizations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "AdminActivityLogs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    AdminId = table.Column<int>(type: "int", nullable: true),
                    Text = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                    Icon = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    Color = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AdminActivityLogs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AdminActivityLogs_Users_AdminId",
                        column: x => x.AdminId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "BloodRequests",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RecipientId = table.Column<int>(type: "int", nullable: false),
                    DonorId = table.Column<int>(type: "int", nullable: false),
                    HospitalId = table.Column<int>(type: "int", nullable: true),
                    BloodType = table.Column<string>(type: "nvarchar(3)", maxLength: 3, nullable: false),
                    Units = table.Column<int>(type: "int", nullable: false),
                    Priority = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Location = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    Message = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    RespondedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CompletedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BloodRequests", x => x.Id);
                    table.CheckConstraint("CK_BloodRequests_Units", "[Units] > 0");
                    table.ForeignKey(
                        name: "FK_BloodRequests_Organizations_HospitalId",
                        column: x => x.HospitalId,
                        principalTable: "Organizations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_BloodRequests_Users_DonorId",
                        column: x => x.DonorId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_BloodRequests_Users_RecipientId",
                        column: x => x.RecipientId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Certificates",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Code = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    DonationCount = table.Column<int>(type: "int", nullable: false),
                    SentToEmail = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    SentAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Certificates", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Certificates_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "DonationAppointments",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    DonorId = table.Column<int>(type: "int", nullable: false),
                    OrganizationId = table.Column<int>(type: "int", nullable: false),
                    DonorName = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    DonorPhone = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    DonorEmail = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    BloodType = table.Column<string>(type: "nvarchar(3)", maxLength: 3, nullable: false),
                    RequestedDate = table.Column<DateOnly>(type: "date", nullable: false),
                    RequestedTime = table.Column<TimeOnly>(type: "time", nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Age = table.Column<int>(type: "int", nullable: false),
                    WeightKg = table.Column<decimal>(type: "decimal(5,1)", precision: 5, scale: 1, nullable: false),
                    LastDonationDate = table.Column<DateOnly>(type: "date", nullable: true),
                    FeelingWell = table.Column<bool>(type: "bit", nullable: false),
                    TakesMedication = table.Column<bool>(type: "bit", nullable: false),
                    MedicationDetail = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    TravelledAbroad = table.Column<bool>(type: "bit", nullable: false),
                    TravelDetail = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    RecentProcedure = table.Column<bool>(type: "bit", nullable: false),
                    EligibilityState = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    EligibilityFlags = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    ConfirmedDate = table.Column<DateOnly>(type: "date", nullable: true),
                    ConfirmedTime = table.Column<TimeOnly>(type: "time", nullable: true),
                    HospitalNote = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    DeclineReason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    DonorNotified = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DonationAppointments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DonationAppointments_Organizations_OrganizationId",
                        column: x => x.OrganizationId,
                        principalTable: "Organizations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_DonationAppointments_Users_DonorId",
                        column: x => x.DonorId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "HallBookings",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    HallId = table.Column<int>(type: "int", nullable: false),
                    RequestedById = table.Column<int>(type: "int", nullable: true),
                    OrganizationName = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    ContactName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Email = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    Phone = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    EventType = table.Column<string>(type: "nvarchar(60)", maxLength: 60, nullable: false),
                    EventDate = table.Column<DateOnly>(type: "date", nullable: false),
                    EventTime = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: true),
                    Attendees = table.Column<int>(type: "int", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    Price = table.Column<decimal>(type: "decimal(10,2)", precision: 10, scale: 2, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    AdminNote = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    PaymentMethod = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: true),
                    PaidAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HallBookings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_HallBookings_Halls_HallId",
                        column: x => x.HallId,
                        principalTable: "Halls",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_HallBookings_Users_RequestedById",
                        column: x => x.RequestedById,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "Reports",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ReporterId = table.Column<int>(type: "int", nullable: true),
                    FromName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    ReporterRole = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    Email = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    Category = table.Column<string>(type: "nvarchar(60)", maxLength: 60, nullable: false),
                    Related = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    Title = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    Body = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: false),
                    Priority = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    IsRead = table.Column<bool>(type: "bit", nullable: false),
                    Response = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ResolvedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Reports", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Reports_Users_ReporterId",
                        column: x => x.ReporterId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "ChatMessages",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    BloodRequestId = table.Column<int>(type: "int", nullable: false),
                    SenderId = table.Column<int>(type: "int", nullable: true),
                    SenderType = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Text = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    SentAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ChatMessages", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ChatMessages_BloodRequests_BloodRequestId",
                        column: x => x.BloodRequestId,
                        principalTable: "BloodRequests",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ChatMessages_Users_SenderId",
                        column: x => x.SenderId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ChatReadStates",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    BloodRequestId = table.Column<int>(type: "int", nullable: false),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    LastReadAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ChatReadStates", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ChatReadStates_BloodRequests_BloodRequestId",
                        column: x => x.BloodRequestId,
                        principalTable: "BloodRequests",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ChatReadStates_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "DonationRecords",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    OrganizationId = table.Column<int>(type: "int", nullable: false),
                    DonorId = table.Column<int>(type: "int", nullable: true),
                    DonorName = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    RecipientName = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    BloodType = table.Column<string>(type: "nvarchar(3)", maxLength: 3, nullable: false),
                    Units = table.Column<int>(type: "int", nullable: false),
                    DonationDate = table.Column<DateOnly>(type: "date", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    AppointmentId = table.Column<int>(type: "int", nullable: true),
                    BloodRequestId = table.Column<int>(type: "int", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DonationRecords", x => x.Id);
                    table.CheckConstraint("CK_DonationRecords_Units", "[Units] > 0");
                    table.ForeignKey(
                        name: "FK_DonationRecords_BloodRequests_BloodRequestId",
                        column: x => x.BloodRequestId,
                        principalTable: "BloodRequests",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_DonationRecords_DonationAppointments_AppointmentId",
                        column: x => x.AppointmentId,
                        principalTable: "DonationAppointments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_DonationRecords_Organizations_OrganizationId",
                        column: x => x.OrganizationId,
                        principalTable: "Organizations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_DonationRecords_Users_DonorId",
                        column: x => x.DonorId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "HallBookingDocuments",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    HallBookingId = table.Column<int>(type: "int", nullable: false),
                    Label = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    FilePath = table.Column<string>(type: "nvarchar(260)", maxLength: 260, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HallBookingDocuments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_HallBookingDocuments_HallBookings_HallBookingId",
                        column: x => x.HallBookingId,
                        principalTable: "HallBookings",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ReportAttachments",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ReportId = table.Column<int>(type: "int", nullable: false),
                    FilePath = table.Column<string>(type: "nvarchar(260)", maxLength: 260, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReportAttachments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ReportAttachments_Reports_ReportId",
                        column: x => x.ReportId,
                        principalTable: "Reports",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                table: "Halls",
                columns: new[] { "Id", "Capacity", "Description", "EntityType", "Features", "Governorate", "ImageUrl", "IsActive", "Location", "Name", "Price", "Rating", "SupportedEvents", "Tag" },
                values: new object[,]
                {
                    { 1, 250, "A modern venue equipped for conferences, seminars, and academic events.", "University", "Presentation screen,Sound system,Parking", "Amman", "https://images.unsplash.com/photo-1517457373958-b7bdd4587205?auto=format&fit=crop&w=900&q=80", true, "Jubeiha, Amman", "University of Jordan Conference Hall", 180m, 4.9m, "Conference,Workshop,Training", "Popular choice" },
                    { 2, 120, "A flexible space for community activities, training, and workshops.", "Community center", "Wi-Fi,Equipment,Parking", "Amman", "https://images.unsplash.com/photo-1497366754035-f200968a6e72?auto=format&fit=crop&w=900&q=80", true, "Khalda, Amman", "Zaha Cultural Center", 95m, 4.8m, "Initiative,Workshop,Training", "Great for initiatives" },
                    { 3, 400, "A spacious hall with lighting and facilities for large events.", "Private institution", "Stage,Professional audio,Parking", "Aqaba", "https://images.unsplash.com/photo-1505373877841-8d25f7d46678?auto=format&fit=crop&w=900&q=80", true, "Aqaba city center", "Aqaba Grand Hall", 260m, 4.7m, "Conference,Initiative", "Large capacity" },
                    { 4, 90, "A practical venue for youth initiatives and small community gatherings.", "Government institution", "Wi-Fi,Whiteboard,Parking", "Irbid", "https://images.unsplash.com/photo-1517502884422-41eaead166d4?auto=format&fit=crop&w=900&q=80", true, "University Street, Irbid", "Irbid Innovation Center", 70m, 4.8m, "Initiative,Workshop,Training", "Budget friendly" },
                    { 5, 180, "A multi-purpose hall for public meetings and local events.", "Government institution", "Presentation screen,Sound system,Seating", "Irbid", "https://images.unsplash.com/photo-1497366811353-6870744d04b2?auto=format&fit=crop&w=900&q=80", true, "Downtown, Irbid", "Greater Irbid Municipality Hall", 130m, 4.6m, "Conference,Initiative", "Available to book" },
                    { 6, 70, "A comfortable venue for short courses, workshops, and volunteer activities.", "Community center", "Wi-Fi,Presentation setup,Seating", "Zarqa", "https://images.unsplash.com/photo-1517245386807-bb43f82c33c4?auto=format&fit=crop&w=900&q=80", true, "New Zarqa, Zarqa", "King Abdullah II Community Center", 55m, 4.5m, "Workshop,Training,Initiative", "Affordable" }
                });

            migrationBuilder.InsertData(
                table: "Organizations",
                columns: new[] { "Id", "Address", "City", "CloseTime", "ContactPerson", "DecidedAt", "Email", "LicenseNumber", "Name", "OpenTime", "Phone", "RegisteredAt", "RejectionReason", "SlotCapacity", "Status", "Type" },
                values: new object[,]
                {
                    { 1, "University Street, Irbid", "Irbid", new TimeOnly(16, 0, 0), "Hospital Admin", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "bloodbank@alhayat.example", "MOH-IRB-40017", "Al Hayat Hospital", new TimeOnly(8, 0, 0), "+962 2 700 0000", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, 3, "Active", "Hospital" },
                    { 2, "Amman–Jerash Road, Jerash", "Jerash", new TimeOnly(15, 0, 0), "Dr. Hiba Khatib", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "info@jerashfamily.jo", "MOH-JRS-20981", "Jerash Family Hospital", new TimeOnly(8, 0, 0), "+962 2 635 5500", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, 3, "Active", "Hospital" },
                    { 3, "Aqaba city center", "Aqaba", new TimeOnly(15, 0, 0), "Fadi Amro", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "aqaba@redcrescent.jo", "MOH-AQB-77310", "Aqaba Red Crescent Center", new TimeOnly(9, 0, 0), "+962 3 201 7788", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, 3, "Active", "BloodDonationCenter" }
                });

            migrationBuilder.InsertData(
                table: "BloodInventory",
                columns: new[] { "Id", "BloodType", "OrganizationId", "Units", "UpdatedAt" },
                values: new object[,]
                {
                    { 1, "O+", 1, 52, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { 2, "O-", 1, 12, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { 3, "A+", 1, 44, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { 4, "A-", 1, 8, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { 5, "B+", 1, 36, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { 6, "B-", 1, 6, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { 7, "AB+", 1, 61, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { 8, "AB-", 1, 29, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) }
                });

            migrationBuilder.CreateIndex(
                name: "IX_AdminActivityLogs_AdminId",
                table: "AdminActivityLogs",
                column: "AdminId");

            migrationBuilder.CreateIndex(
                name: "IX_Advertisements_OrganizationId",
                table: "Advertisements",
                column: "OrganizationId");

            migrationBuilder.CreateIndex(
                name: "IX_Advertisements_StartDate_EndDate",
                table: "Advertisements",
                columns: new[] { "StartDate", "EndDate" });

            migrationBuilder.CreateIndex(
                name: "IX_BloodInventory_OrganizationId_BloodType",
                table: "BloodInventory",
                columns: new[] { "OrganizationId", "BloodType" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_BloodRequests_DonorId_Status",
                table: "BloodRequests",
                columns: new[] { "DonorId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_BloodRequests_HospitalId",
                table: "BloodRequests",
                column: "HospitalId");

            migrationBuilder.CreateIndex(
                name: "IX_BloodRequests_RecipientId_Status",
                table: "BloodRequests",
                columns: new[] { "RecipientId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_Certificates_Code",
                table: "Certificates",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Certificates_UserId",
                table: "Certificates",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_ChatMessages_BloodRequestId_SentAt",
                table: "ChatMessages",
                columns: new[] { "BloodRequestId", "SentAt" });

            migrationBuilder.CreateIndex(
                name: "IX_ChatMessages_SenderId",
                table: "ChatMessages",
                column: "SenderId");

            migrationBuilder.CreateIndex(
                name: "IX_ChatReadStates_BloodRequestId_UserId",
                table: "ChatReadStates",
                columns: new[] { "BloodRequestId", "UserId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ChatReadStates_UserId",
                table: "ChatReadStates",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_DonationAppointments_DonorId",
                table: "DonationAppointments",
                column: "DonorId");

            migrationBuilder.CreateIndex(
                name: "IX_DonationAppointments_OrganizationId_Status_RequestedDate",
                table: "DonationAppointments",
                columns: new[] { "OrganizationId", "Status", "RequestedDate" });

            migrationBuilder.CreateIndex(
                name: "IX_DonationRecords_AppointmentId",
                table: "DonationRecords",
                column: "AppointmentId");

            migrationBuilder.CreateIndex(
                name: "IX_DonationRecords_BloodRequestId",
                table: "DonationRecords",
                column: "BloodRequestId");

            migrationBuilder.CreateIndex(
                name: "IX_DonationRecords_DonorId",
                table: "DonationRecords",
                column: "DonorId");

            migrationBuilder.CreateIndex(
                name: "IX_DonationRecords_OrganizationId_DonationDate",
                table: "DonationRecords",
                columns: new[] { "OrganizationId", "DonationDate" });

            migrationBuilder.CreateIndex(
                name: "IX_HallBookingDocuments_HallBookingId",
                table: "HallBookingDocuments",
                column: "HallBookingId");

            migrationBuilder.CreateIndex(
                name: "IX_HallBookings_HallId",
                table: "HallBookings",
                column: "HallId");

            migrationBuilder.CreateIndex(
                name: "IX_HallBookings_RequestedById",
                table: "HallBookings",
                column: "RequestedById");

            migrationBuilder.CreateIndex(
                name: "IX_HallBookings_Status",
                table: "HallBookings",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_OrganizationDocuments_OrganizationId",
                table: "OrganizationDocuments",
                column: "OrganizationId");

            migrationBuilder.CreateIndex(
                name: "IX_Organizations_Name",
                table: "Organizations",
                column: "Name");

            migrationBuilder.CreateIndex(
                name: "IX_Organizations_Status",
                table: "Organizations",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_ReportAttachments_ReportId",
                table: "ReportAttachments",
                column: "ReportId");

            migrationBuilder.CreateIndex(
                name: "IX_Reports_ReporterId",
                table: "Reports",
                column: "ReporterId");

            migrationBuilder.CreateIndex(
                name: "IX_Reports_Status_CreatedAt",
                table: "Reports",
                columns: new[] { "Status", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_Users_Email",
                table: "Users",
                column: "Email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Users_OrganizationId",
                table: "Users",
                column: "OrganizationId");

            migrationBuilder.CreateIndex(
                name: "IX_Users_Role_BloodType_City",
                table: "Users",
                columns: new[] { "Role", "BloodType", "City" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AdminActivityLogs");

            migrationBuilder.DropTable(
                name: "Advertisements");

            migrationBuilder.DropTable(
                name: "BloodInventory");

            migrationBuilder.DropTable(
                name: "Certificates");

            migrationBuilder.DropTable(
                name: "ChatMessages");

            migrationBuilder.DropTable(
                name: "ChatReadStates");

            migrationBuilder.DropTable(
                name: "DonationRecords");

            migrationBuilder.DropTable(
                name: "HallBookingDocuments");

            migrationBuilder.DropTable(
                name: "OrganizationDocuments");

            migrationBuilder.DropTable(
                name: "ReportAttachments");

            migrationBuilder.DropTable(
                name: "BloodRequests");

            migrationBuilder.DropTable(
                name: "DonationAppointments");

            migrationBuilder.DropTable(
                name: "HallBookings");

            migrationBuilder.DropTable(
                name: "Reports");

            migrationBuilder.DropTable(
                name: "Halls");

            migrationBuilder.DropTable(
                name: "Users");

            migrationBuilder.DropTable(
                name: "Organizations");
        }
    }
}
