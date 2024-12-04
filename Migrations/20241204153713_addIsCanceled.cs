using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LaundryDashAPI_2.Migrations
{
    /// <inheritdoc />
    public partial class addIsCanceled : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsCanceled",
                table: "BookingLogs",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsCanceled",
                table: "BookingLogs");
        }
    }
}
