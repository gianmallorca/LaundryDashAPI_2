using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LaundryDashAPI_2.Migrations
{
    /// <inheritdoc />
    public partial class hahaha : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "LaundryShopName",
                table: "LaundryShops",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(120)",
                oldMaxLength: 120);

            migrationBuilder.AddColumn<string>(
                name: "LaundryShopPicture",
                table: "LaundryShops",
                type: "nvarchar(120)",
                maxLength: 120,
                nullable: false,
                defaultValue: "");

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

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_BookingLogs_AspNetUsers_AppUserId",
                table: "BookingLogs");

            migrationBuilder.DropIndex(
                name: "IX_BookingLogs_AppUserId",
                table: "BookingLogs");

            migrationBuilder.DropColumn(
                name: "LaundryShopPicture",
                table: "LaundryShops");

            migrationBuilder.DropColumn(
                name: "AppUserId",
                table: "BookingLogs");

            migrationBuilder.AlterColumn<string>(
                name: "LaundryShopName",
                table: "LaundryShops",
                type: "nvarchar(120)",
                maxLength: 120,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");
        }
    }
}
