using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LaundryDashAPI_2.Migrations
{
    /// <inheritdoc />
    public partial class removedProps : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ClientName",
                table: "BookingLogs");

            migrationBuilder.DropColumn(
                name: "LaundryShopName",
                table: "BookingLogs");

            migrationBuilder.DropColumn(
                name: "ServiceName",
                table: "BookingLogs");

            migrationBuilder.AddColumn<Guid>(
                name: "ServiceId",
                table: "LaundryServiceLogs",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.CreateIndex(
                name: "IX_LaundryServiceLogs_ServiceId",
                table: "LaundryServiceLogs",
                column: "ServiceId");

            migrationBuilder.AddForeignKey(
                name: "FK_LaundryServiceLogs_Services_ServiceId",
                table: "LaundryServiceLogs",
                column: "ServiceId",
                principalTable: "Services",
                principalColumn: "ServiceId",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_LaundryServiceLogs_Services_ServiceId",
                table: "LaundryServiceLogs");

            migrationBuilder.DropIndex(
                name: "IX_LaundryServiceLogs_ServiceId",
                table: "LaundryServiceLogs");

            migrationBuilder.DropColumn(
                name: "ServiceId",
                table: "LaundryServiceLogs");

            migrationBuilder.AddColumn<string>(
                name: "ClientName",
                table: "BookingLogs",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "LaundryShopName",
                table: "BookingLogs",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "ServiceName",
                table: "BookingLogs",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");
        }
    }
}
