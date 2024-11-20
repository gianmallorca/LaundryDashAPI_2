using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LaundryDashAPI_2.Migrations
{
    /// <inheritdoc />
    public partial class updatedFieldsBookingLogsAgain : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "RiderId",
                table: "BookingLogs",
                newName: "PickupRiderId");

            migrationBuilder.RenameColumn(
                name: "PickedUpFromShop",
                table: "BookingLogs",
                newName: "PickUpFromShop");

            migrationBuilder.RenameColumn(
                name: "PickedUpFromClient",
                table: "BookingLogs",
                newName: "PickUpFromClient");

            migrationBuilder.AddColumn<string>(
                name: "DeliveryRiderId",
                table: "BookingLogs",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsOutForDelivery",
                table: "BookingLogs",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsReadyForDelivery",
                table: "BookingLogs",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DeliveryRiderId",
                table: "BookingLogs");

            migrationBuilder.DropColumn(
                name: "IsOutForDelivery",
                table: "BookingLogs");

            migrationBuilder.DropColumn(
                name: "IsReadyForDelivery",
                table: "BookingLogs");

            migrationBuilder.RenameColumn(
                name: "PickupRiderId",
                table: "BookingLogs",
                newName: "RiderId");

            migrationBuilder.RenameColumn(
                name: "PickUpFromShop",
                table: "BookingLogs",
                newName: "PickedUpFromShop");

            migrationBuilder.RenameColumn(
                name: "PickUpFromClient",
                table: "BookingLogs",
                newName: "PickedUpFromClient");
        }
    }
}
