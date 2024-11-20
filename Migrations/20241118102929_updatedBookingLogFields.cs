using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LaundryDashAPI_2.Migrations
{
    /// <inheritdoc />
    public partial class updatedBookingLogFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "IsAccepted",
                table: "BookingLogs",
                newName: "PickedUpFromShop");

            migrationBuilder.RenameColumn(
                name: "AcceptedByRider",
                table: "BookingLogs",
                newName: "PickedUpFromClient");

            migrationBuilder.AddColumn<bool>(
                name: "HasStartedYourLaundry",
                table: "BookingLogs",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsAcceptedByShop",
                table: "BookingLogs",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "HasStartedYourLaundry",
                table: "BookingLogs");

            migrationBuilder.DropColumn(
                name: "IsAcceptedByShop",
                table: "BookingLogs");

            migrationBuilder.RenameColumn(
                name: "PickedUpFromShop",
                table: "BookingLogs",
                newName: "IsAccepted");

            migrationBuilder.RenameColumn(
                name: "PickedUpFromClient",
                table: "BookingLogs",
                newName: "AcceptedByRider");
        }
    }
}
