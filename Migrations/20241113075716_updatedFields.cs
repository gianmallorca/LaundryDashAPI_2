using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LaundryDashAPI_2.Migrations
{
    /// <inheritdoc />
    public partial class updatedFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsActive",
                table: "Services",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "BusinessPermitId",
                table: "LaundryShops",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DTIPermitId",
                table: "LaundryShops",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "EnvironmentalPermit",
                table: "LaundryShops",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SanitaryPermit",
                table: "LaundryShops",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TaxIdentificationNumber",
                table: "LaundryShops",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsActive",
                table: "LaundryServiceLogs",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "ServiceDescription",
                table: "LaundryServiceLogs",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AlterColumn<bool>(
                name: "ReceivedByClient",
                table: "BookingLogs",
                type: "bit",
                nullable: false,
                defaultValue: false,
                oldClrType: typeof(bool),
                oldType: "bit",
                oldNullable: true);

            migrationBuilder.AlterColumn<bool>(
                name: "IsAccepted",
                table: "BookingLogs",
                type: "bit",
                nullable: false,
                defaultValue: false,
                oldClrType: typeof(bool),
                oldType: "bit",
                oldNullable: true);

            migrationBuilder.AlterColumn<bool>(
                name: "DepartedFromShop",
                table: "BookingLogs",
                type: "bit",
                nullable: false,
                defaultValue: false,
                oldClrType: typeof(bool),
                oldType: "bit",
                oldNullable: true);

            migrationBuilder.AlterColumn<bool>(
                name: "AcceptedByRider",
                table: "BookingLogs",
                type: "bit",
                nullable: false,
                defaultValue: false,
                oldClrType: typeof(bool),
                oldType: "bit",
                oldNullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "TransactionCompleted",
                table: "BookingLogs",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "BusinessPermitNumber",
                table: "AspNetUsers",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DriversLicenseNumber",
                table: "AspNetUsers",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TaxIdentificationNumber",
                table: "AspNetUsers",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "VehicleCapacity",
                table: "AspNetUsers",
                type: "decimal(18,2)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "VehicleType",
                table: "AspNetUsers",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsActive",
                table: "Services");

            migrationBuilder.DropColumn(
                name: "BusinessPermitId",
                table: "LaundryShops");

            migrationBuilder.DropColumn(
                name: "DTIPermitId",
                table: "LaundryShops");

            migrationBuilder.DropColumn(
                name: "EnvironmentalPermit",
                table: "LaundryShops");

            migrationBuilder.DropColumn(
                name: "SanitaryPermit",
                table: "LaundryShops");

            migrationBuilder.DropColumn(
                name: "TaxIdentificationNumber",
                table: "LaundryShops");

            migrationBuilder.DropColumn(
                name: "IsActive",
                table: "LaundryServiceLogs");

            migrationBuilder.DropColumn(
                name: "ServiceDescription",
                table: "LaundryServiceLogs");

            migrationBuilder.DropColumn(
                name: "TransactionCompleted",
                table: "BookingLogs");

            migrationBuilder.DropColumn(
                name: "BusinessPermitNumber",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "DriversLicenseNumber",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "TaxIdentificationNumber",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "VehicleCapacity",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "VehicleType",
                table: "AspNetUsers");

            migrationBuilder.AlterColumn<bool>(
                name: "ReceivedByClient",
                table: "BookingLogs",
                type: "bit",
                nullable: true,
                oldClrType: typeof(bool),
                oldType: "bit");

            migrationBuilder.AlterColumn<bool>(
                name: "IsAccepted",
                table: "BookingLogs",
                type: "bit",
                nullable: true,
                oldClrType: typeof(bool),
                oldType: "bit");

            migrationBuilder.AlterColumn<bool>(
                name: "DepartedFromShop",
                table: "BookingLogs",
                type: "bit",
                nullable: true,
                oldClrType: typeof(bool),
                oldType: "bit");

            migrationBuilder.AlterColumn<bool>(
                name: "AcceptedByRider",
                table: "BookingLogs",
                type: "bit",
                nullable: true,
                oldClrType: typeof(bool),
                oldType: "bit");
        }
    }
}
