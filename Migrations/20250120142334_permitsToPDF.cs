using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LaundryDashAPI_2.Migrations
{
    /// <inheritdoc />
    public partial class permitsToPDF : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
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

            migrationBuilder.RenameColumn(
                name: "TaxIdentificationNumber",
                table: "LaundryShops",
                newName: "BusinessPermitsPDF");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "BusinessPermitsPDF",
                table: "LaundryShops",
                newName: "TaxIdentificationNumber");

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
        }
    }
}
