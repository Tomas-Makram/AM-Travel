using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AM_Travel.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Buses",
                columns: table => new
                {
                    BusId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    PlateNumber = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: true),
                    SeatsCount = table.Column<int>(type: "int", nullable: false),
                    LayoutRows = table.Column<int>(type: "int", nullable: false),
                    LayoutColumns = table.Column<int>(type: "int", nullable: false),
                    LayoutJson = table.Column<string>(type: "nvarchar(max)", maxLength: 8000, nullable: true),
                    FromLocation = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    ToLocation = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Buses", x => x.BusId);
                });

            migrationBuilder.CreateTable(
                name: "Companies",
                columns: table => new
                {
                    CompanyId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(160)", maxLength: 160, nullable: false),
                    PhoneNumber = table.Column<string>(type: "nvarchar(11)", maxLength: 11, nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Companies", x => x.CompanyId);
                });

            migrationBuilder.CreateTable(
                name: "TelegramSettings",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    BotToken = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    OtpExpireMinutes = table.Column<int>(type: "int", nullable: false),
                    ChatId = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Code = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    TimeErrorOTP = table.Column<int>(type: "int", nullable: false),
                    CreateAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    SendOTPAfterMinuts = table.Column<int>(type: "int", nullable: false),
                    LastSendOTP = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TelegramSettings", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Users",
                columns: table => new
                {
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FullName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    UserName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    PhoneNumber = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    NationalId = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    PasswordHash = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Type = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    TypeChipher = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    JoinDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    LastLogin = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Activate = table.Column<bool>(type: "bit", nullable: false),
                    Login = table.Column<bool>(type: "bit", nullable: false),
                    Blocked = table.Column<bool>(type: "bit", nullable: false),
                    FailedLoginAttempts = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Users", x => x.UserId);
                });

            migrationBuilder.CreateTable(
                name: "BusSeats",
                columns: table => new
                {
                    SeatId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    BusId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SeatNumber = table.Column<int>(type: "int", nullable: false),
                    RowNumber = table.Column<int>(type: "int", nullable: false),
                    ColumnNumber = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    SeatLabel = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    FromLocation = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ToLocation = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SeatType = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BusSeats", x => x.SeatId);
                    table.ForeignKey(
                        name: "FK_BusSeats_Buses_BusId",
                        column: x => x.BusId,
                        principalTable: "Buses",
                        principalColumn: "BusId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "BusTrips",
                columns: table => new
                {
                    TripId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    BusId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    LayoutRows = table.Column<int>(type: "int", nullable: false),
                    LayoutColumns = table.Column<int>(type: "int", nullable: false),
                    LayoutJson = table.Column<string>(type: "nvarchar(max)", maxLength: 8000, nullable: true),
                    BusNameSnapshot = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    PlateNumberSnapshot = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: true),
                    SeatsCountSnapshot = table.Column<int>(type: "int", nullable: false),
                    SeatsSnapshotJson = table.Column<string>(type: "nvarchar(max)", maxLength: 16000, nullable: true),
                    Direction = table.Column<int>(type: "int", nullable: false),
                    TripDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    FromLocation = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    ToLocation = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    IsLayoutCustomized = table.Column<bool>(type: "bit", nullable: false),
                    IsClosed = table.Column<bool>(type: "bit", nullable: false),
                    CompanyId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CompanyTripGroupId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CompanyTripPrice = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    CompanyTripPaidAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BusTrips", x => x.TripId);
                    table.ForeignKey(
                        name: "FK_BusTrips_Buses_BusId",
                        column: x => x.BusId,
                        principalTable: "Buses",
                        principalColumn: "BusId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_BusTrips_Companies_CompanyId",
                        column: x => x.CompanyId,
                        principalTable: "Companies",
                        principalColumn: "CompanyId",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "Bookings",
                columns: table => new
                {
                    BookingID = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Code = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ClientName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    HasHotel = table.Column<bool>(type: "bit", nullable: false),
                    HasTransportation = table.Column<bool>(type: "bit", nullable: false),
                    HotelName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CheckInDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CheckOutDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    NumberOfRooms = table.Column<int>(type: "int", nullable: false),
                    RoomType = table.Column<int>(type: "int", nullable: false),
                    ChildrenCountUntil6Years = table.Column<int>(type: "int", nullable: false),
                    ChildrenCountUntil12Years = table.Column<int>(type: "int", nullable: false),
                    TotalChildrenCount = table.Column<int>(type: "int", nullable: false),
                    HotelNightPrice = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    NightsCount = table.Column<int>(type: "int", nullable: false),
                    HotelTotal = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    SeatsCount = table.Column<int>(type: "int", nullable: false),
                    SeatPrice = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    TransportationTotal = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    PayType = table.Column<int>(type: "int", nullable: false),
                    Discount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    PaidAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    GrandTotal = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    RemainingAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeletedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Bookings", x => x.BookingID);
                    table.ForeignKey(
                        name: "FK_Bookings_Users_DeletedByUserId",
                        column: x => x.DeletedByUserId,
                        principalTable: "Users",
                        principalColumn: "UserId");
                    table.ForeignKey(
                        name: "FK_Bookings_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "UserSessions",
                columns: table => new
                {
                    SessionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RefreshTokenHash = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    RefreshTokenExpiresAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    LastActivityAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    DeviceName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IpAddress = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserSessions", x => x.SessionId);
                    table.ForeignKey(
                        name: "FK_UserSessions_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Works",
                columns: table => new
                {
                    WorkId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PhoneNumber = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    NameClient = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ClientType = table.Column<int>(type: "int", nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    DayCreated = table.Column<DateTime>(type: "datetime2", nullable: false),
                    DayUpdated = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Works", x => x.WorkId);
                    table.ForeignKey(
                        name: "FK_Works_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CompanySeatBookings",
                columns: table => new
                {
                    BookingId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CompanyId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TripId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    SeatId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ReturnTripId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ReturnSeatId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    SeatLabelSnapshot = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    SeatNumberSnapshot = table.Column<int>(type: "int", nullable: false),
                    SeatTypeSnapshot = table.Column<int>(type: "int", nullable: true),
                    ReturnSeatLabelSnapshot = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    ReturnSeatNumberSnapshot = table.Column<int>(type: "int", nullable: false),
                    ReturnSeatTypeSnapshot = table.Column<int>(type: "int", nullable: true),
                    TransferredSeatsJson = table.Column<string>(type: "nvarchar(max)", maxLength: 16000, nullable: true),
                    SeatsCount = table.Column<int>(type: "int", nullable: false),
                    TripDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ReturnTripDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    FromLocation = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    ToLocation = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    ReturnFromLocation = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    ReturnToLocation = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    PricePerSeat = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    BookingDirection = table.Column<int>(type: "int", nullable: false),
                    ClientTripType = table.Column<int>(type: "int", nullable: false),
                    ClientName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    ClientPhone = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    TransferredFromBookingId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    TransferredFromSeatId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CompanySeatBookings", x => x.BookingId);
                    table.ForeignKey(
                        name: "FK_CompanySeatBookings_BusSeats_ReturnSeatId",
                        column: x => x.ReturnSeatId,
                        principalTable: "BusSeats",
                        principalColumn: "SeatId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CompanySeatBookings_BusSeats_SeatId",
                        column: x => x.SeatId,
                        principalTable: "BusSeats",
                        principalColumn: "SeatId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CompanySeatBookings_BusTrips_ReturnTripId",
                        column: x => x.ReturnTripId,
                        principalTable: "BusTrips",
                        principalColumn: "TripId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CompanySeatBookings_BusTrips_TripId",
                        column: x => x.TripId,
                        principalTable: "BusTrips",
                        principalColumn: "TripId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CompanySeatBookings_Companies_CompanyId",
                        column: x => x.CompanyId,
                        principalTable: "Companies",
                        principalColumn: "CompanyId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "CompanySeatPayments",
                columns: table => new
                {
                    PaymentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CompanyId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TripId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Amount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    PaidAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CompanySeatPayments", x => x.PaymentId);
                    table.ForeignKey(
                        name: "FK_CompanySeatPayments_BusTrips_TripId",
                        column: x => x.TripId,
                        principalTable: "BusTrips",
                        principalColumn: "TripId",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_CompanySeatPayments_Companies_CompanyId",
                        column: x => x.CompanyId,
                        principalTable: "Companies",
                        principalColumn: "CompanyId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "BookingAudits",
                columns: table => new
                {
                    AuditId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    BookingId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ChangedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ChangedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ActionType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BookingAudits", x => x.AuditId);
                    table.ForeignKey(
                        name: "FK_BookingAudits_Bookings_BookingId",
                        column: x => x.BookingId,
                        principalTable: "Bookings",
                        principalColumn: "BookingID",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_BookingAudits_Users_ChangedByUserId",
                        column: x => x.ChangedByUserId,
                        principalTable: "Users",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_BookingAudits_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "UserId");
                });

            migrationBuilder.CreateTable(
                name: "BookingPayments",
                columns: table => new
                {
                    PaymentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    BookingId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    PayType = table.Column<int>(type: "int", nullable: false),
                    PaidAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BookingPayments", x => x.PaymentId);
                    table.ForeignKey(
                        name: "FK_BookingPayments_Bookings_BookingId",
                        column: x => x.BookingId,
                        principalTable: "Bookings",
                        principalColumn: "BookingID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "BookingRooms",
                columns: table => new
                {
                    BookingRoomId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    BookingId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RoomType = table.Column<int>(type: "int", nullable: false),
                    Count = table.Column<int>(type: "int", nullable: false),
                    NightPrice = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BookingRooms", x => x.BookingRoomId);
                    table.ForeignKey(
                        name: "FK_BookingRooms_Bookings_BookingId",
                        column: x => x.BookingId,
                        principalTable: "Bookings",
                        principalColumn: "BookingID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "BookingTransportationSeats",
                columns: table => new
                {
                    BookingSeatId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    BookingId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TripId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SeatId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Direction = table.Column<int>(type: "int", nullable: false),
                    FromLocation = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ToLocation = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    SeatPrice = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    ReservedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BookingTransportationSeats", x => x.BookingSeatId);
                    table.ForeignKey(
                        name: "FK_BookingTransportationSeats_Bookings_BookingId",
                        column: x => x.BookingId,
                        principalTable: "Bookings",
                        principalColumn: "BookingID",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_BookingTransportationSeats_BusTrips_TripId",
                        column: x => x.TripId,
                        principalTable: "BusTrips",
                        principalColumn: "TripId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Telephones",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PhoneNumber = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Prime = table.Column<bool>(type: "bit", nullable: false),
                    BookingID = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Telephones", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Telephones_Bookings_BookingID",
                        column: x => x.BookingID,
                        principalTable: "Bookings",
                        principalColumn: "BookingID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "BookingAuditDetails",
                columns: table => new
                {
                    DetailId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AuditId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FieldName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    OldValue = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    NewValue = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BookingAuditDetails", x => x.DetailId);
                    table.ForeignKey(
                        name: "FK_BookingAuditDetails_BookingAudits_AuditId",
                        column: x => x.AuditId,
                        principalTable: "BookingAudits",
                        principalColumn: "AuditId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_BookingAuditDetails_AuditId",
                table: "BookingAuditDetails",
                column: "AuditId");

            migrationBuilder.CreateIndex(
                name: "IX_BookingAudits_BookingId",
                table: "BookingAudits",
                column: "BookingId");

            migrationBuilder.CreateIndex(
                name: "IX_BookingAudits_ChangedByUserId",
                table: "BookingAudits",
                column: "ChangedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_BookingAudits_UserId",
                table: "BookingAudits",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_BookingPayments_BookingId",
                table: "BookingPayments",
                column: "BookingId");

            migrationBuilder.CreateIndex(
                name: "IX_BookingRooms_BookingId_RoomType",
                table: "BookingRooms",
                columns: new[] { "BookingId", "RoomType" });

            migrationBuilder.CreateIndex(
                name: "IX_Bookings_DeletedByUserId",
                table: "Bookings",
                column: "DeletedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_Bookings_UserId",
                table: "Bookings",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_BookingTransportationSeats_BookingId",
                table: "BookingTransportationSeats",
                column: "BookingId");

            migrationBuilder.CreateIndex(
                name: "IX_BookingTransportationSeats_TripId_SeatId",
                table: "BookingTransportationSeats",
                columns: new[] { "TripId", "SeatId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_BusSeats_BusId",
                table: "BusSeats",
                column: "BusId");

            migrationBuilder.CreateIndex(
                name: "IX_BusTrips_BusId",
                table: "BusTrips",
                column: "BusId");

            migrationBuilder.CreateIndex(
                name: "IX_BusTrips_CompanyId",
                table: "BusTrips",
                column: "CompanyId");

            migrationBuilder.CreateIndex(
                name: "IX_BusTrips_TripDate_Direction_BusId",
                table: "BusTrips",
                columns: new[] { "TripDate", "Direction", "BusId" });

            migrationBuilder.CreateIndex(
                name: "IX_CompanySeatBookings_CompanyId_TripId",
                table: "CompanySeatBookings",
                columns: new[] { "CompanyId", "TripId" });

            migrationBuilder.CreateIndex(
                name: "IX_CompanySeatBookings_ReturnSeatId",
                table: "CompanySeatBookings",
                column: "ReturnSeatId");

            migrationBuilder.CreateIndex(
                name: "IX_CompanySeatBookings_ReturnTripId_ReturnSeatId",
                table: "CompanySeatBookings",
                columns: new[] { "ReturnTripId", "ReturnSeatId" },
                unique: true,
                filter: "[ReturnTripId] IS NOT NULL AND [ReturnSeatId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_CompanySeatBookings_SeatId",
                table: "CompanySeatBookings",
                column: "SeatId");

            migrationBuilder.CreateIndex(
                name: "IX_CompanySeatBookings_TransferredFromBookingId",
                table: "CompanySeatBookings",
                column: "TransferredFromBookingId");

            migrationBuilder.CreateIndex(
                name: "IX_CompanySeatBookings_TripId_BookingDirection",
                table: "CompanySeatBookings",
                columns: new[] { "TripId", "BookingDirection" });

            migrationBuilder.CreateIndex(
                name: "IX_CompanySeatBookings_TripId_SeatId",
                table: "CompanySeatBookings",
                columns: new[] { "TripId", "SeatId" },
                unique: true,
                filter: "[TripId] IS NOT NULL AND [SeatId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_CompanySeatPayments_CompanyId",
                table: "CompanySeatPayments",
                column: "CompanyId");

            migrationBuilder.CreateIndex(
                name: "IX_CompanySeatPayments_TripId",
                table: "CompanySeatPayments",
                column: "TripId");

            migrationBuilder.CreateIndex(
                name: "IX_Telephones_BookingID",
                table: "Telephones",
                column: "BookingID");

            migrationBuilder.CreateIndex(
                name: "IX_UserSessions_UserId",
                table: "UserSessions",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_Works_UserId",
                table: "Works",
                column: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BookingAuditDetails");

            migrationBuilder.DropTable(
                name: "BookingPayments");

            migrationBuilder.DropTable(
                name: "BookingRooms");

            migrationBuilder.DropTable(
                name: "BookingTransportationSeats");

            migrationBuilder.DropTable(
                name: "CompanySeatBookings");

            migrationBuilder.DropTable(
                name: "CompanySeatPayments");

            migrationBuilder.DropTable(
                name: "TelegramSettings");

            migrationBuilder.DropTable(
                name: "Telephones");

            migrationBuilder.DropTable(
                name: "UserSessions");

            migrationBuilder.DropTable(
                name: "Works");

            migrationBuilder.DropTable(
                name: "BookingAudits");

            migrationBuilder.DropTable(
                name: "BusSeats");

            migrationBuilder.DropTable(
                name: "BusTrips");

            migrationBuilder.DropTable(
                name: "Bookings");

            migrationBuilder.DropTable(
                name: "Buses");

            migrationBuilder.DropTable(
                name: "Companies");

            migrationBuilder.DropTable(
                name: "Users");
        }
    }
}
