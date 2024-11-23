using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LaundryDashAPI_2.Migrations
{
    /// <inheritdoc />
    public partial class hahahehe : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_BookingLogs_AspNetUsers_AppUserId",
                table: "BookingLogs");

            migrationBuilder.DropIndex(
                name: "IX_BookingLogs_AppUserId",
                table: "BookingLogs");

            migrationBuilder.DropColumn(
                name: "AppUserId",
                table: "BookingLogs");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AppUserId",
                table: "BookingLogs",
                type: "nvarchar(450)",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_BookingLogs_AppUserId",
                table: "BookingLogs",
                column: "AppUserId");

            migrationBuilder.AddForeignKey(
                name: "FK_BookingLogs_AspNetUsers_AppUserId",
                table: "BookingLogs",
                column: "AppUserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id");
        }
    }
}
