using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LaundryDashAPI_2.Migrations
{
    /// <inheritdoc />
    public partial class BookingLog : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "BookingLog",
                columns: table => new
                {
                    BookingLogId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    LaundryServiceLogId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PickupAddress = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    DeliveryAddress = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    IsAccepted = table.Column<bool>(type: "bit", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BookingLog", x => x.BookingLogId);
                    table.ForeignKey(
                        name: "FK_BookingLog_LaundryServiceLogs_LaundryServiceLogId",
                        column: x => x.LaundryServiceLogId,
                        principalTable: "LaundryServiceLogs",
                        principalColumn: "LaundryServiceLogId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_BookingLog_LaundryServiceLogId",
                table: "BookingLog",
                column: "LaundryServiceLogId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BookingLog");
        }
    }
}
