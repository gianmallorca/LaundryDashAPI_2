using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LaundryDashAPI_2.Migrations
{
    /// <inheritdoc />
    public partial class ClientId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_BookingLog_LaundryServiceLogs_LaundryServiceLogId",
                table: "BookingLog");

            migrationBuilder.DropPrimaryKey(
                name: "PK_BookingLog",
                table: "BookingLog");

            migrationBuilder.RenameTable(
                name: "BookingLog",
                newName: "BookingLogs");

            migrationBuilder.RenameIndex(
                name: "IX_BookingLog_LaundryServiceLogId",
                table: "BookingLogs",
                newName: "IX_BookingLogs_LaundryServiceLogId");

            migrationBuilder.AddColumn<string>(
                name: "ClientId",
                table: "BookingLogs",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddPrimaryKey(
                name: "PK_BookingLogs",
                table: "BookingLogs",
                column: "BookingLogId");

            migrationBuilder.AddForeignKey(
                name: "FK_BookingLogs_LaundryServiceLogs_LaundryServiceLogId",
                table: "BookingLogs",
                column: "LaundryServiceLogId",
                principalTable: "LaundryServiceLogs",
                principalColumn: "LaundryServiceLogId",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_BookingLogs_LaundryServiceLogs_LaundryServiceLogId",
                table: "BookingLogs");

            migrationBuilder.DropPrimaryKey(
                name: "PK_BookingLogs",
                table: "BookingLogs");

            migrationBuilder.DropColumn(
                name: "ClientId",
                table: "BookingLogs");

            migrationBuilder.RenameTable(
                name: "BookingLogs",
                newName: "BookingLog");

            migrationBuilder.RenameIndex(
                name: "IX_BookingLogs_LaundryServiceLogId",
                table: "BookingLog",
                newName: "IX_BookingLog_LaundryServiceLogId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_BookingLog",
                table: "BookingLog",
                column: "BookingLogId");

            migrationBuilder.AddForeignKey(
                name: "FK_BookingLog_LaundryServiceLogs_LaundryServiceLogId",
                table: "BookingLog",
                column: "LaundryServiceLogId",
                principalTable: "LaundryServiceLogs",
                principalColumn: "LaundryServiceLogId",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
