using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LaundryDashAPI_2.Migrations
{
    /// <inheritdoc />
    public partial class userPdfAttachment : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "BusinessPermitNumber",
                table: "AspNetUsers");

            migrationBuilder.RenameColumn(
                name: "TaxIdentificationNumber",
                table: "AspNetUsers",
                newName: "DriversLicense");

            migrationBuilder.RenameColumn(
                name: "DriversLicenseNumber",
                table: "AspNetUsers",
                newName: "BusinessPermitsOfOwner");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "DriversLicense",
                table: "AspNetUsers",
                newName: "TaxIdentificationNumber");

            migrationBuilder.RenameColumn(
                name: "BusinessPermitsOfOwner",
                table: "AspNetUsers",
                newName: "DriversLicenseNumber");

            migrationBuilder.AddColumn<string>(
                name: "BusinessPermitNumber",
                table: "AspNetUsers",
                type: "nvarchar(max)",
                nullable: true);
        }
    }
}
