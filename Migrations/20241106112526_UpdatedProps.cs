using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LaundryDashAPI_2.Migrations
{
    /// <inheritdoc />
    public partial class UpdatedProps : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Address",
                table: "LaundryShops",
                newName: "City");

            migrationBuilder.AddColumn<string>(
                name: "Barangay",
                table: "LaundryShops",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "BrgyStreet",
                table: "LaundryShops",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "ContactNum",
                table: "LaundryShops",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "Friday",
                table: "LaundryShops",
                type: "bit",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "Monday",
                table: "LaundryShops",
                type: "bit",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "Saturday",
                table: "LaundryShops",
                type: "bit",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "Sunday",
                table: "LaundryShops",
                type: "bit",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "Thursday",
                table: "LaundryShops",
                type: "bit",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TimeClose",
                table: "LaundryShops",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TimeOpen",
                table: "LaundryShops",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "Tuesday",
                table: "LaundryShops",
                type: "bit",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "Wednesday",
                table: "LaundryShops",
                type: "bit",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Age",
                table: "AspNetUsers",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Barangay",
                table: "AspNetUsers",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "Birthday",
                table: "AspNetUsers",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "BrgyStreet",
                table: "AspNetUsers",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "City",
                table: "AspNetUsers",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Gender",
                table: "AspNetUsers",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Barangay",
                table: "LaundryShops");

            migrationBuilder.DropColumn(
                name: "BrgyStreet",
                table: "LaundryShops");

            migrationBuilder.DropColumn(
                name: "ContactNum",
                table: "LaundryShops");

            migrationBuilder.DropColumn(
                name: "Friday",
                table: "LaundryShops");

            migrationBuilder.DropColumn(
                name: "Monday",
                table: "LaundryShops");

            migrationBuilder.DropColumn(
                name: "Saturday",
                table: "LaundryShops");

            migrationBuilder.DropColumn(
                name: "Sunday",
                table: "LaundryShops");

            migrationBuilder.DropColumn(
                name: "Thursday",
                table: "LaundryShops");

            migrationBuilder.DropColumn(
                name: "TimeClose",
                table: "LaundryShops");

            migrationBuilder.DropColumn(
                name: "TimeOpen",
                table: "LaundryShops");

            migrationBuilder.DropColumn(
                name: "Tuesday",
                table: "LaundryShops");

            migrationBuilder.DropColumn(
                name: "Wednesday",
                table: "LaundryShops");

            migrationBuilder.DropColumn(
                name: "Age",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "Barangay",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "Birthday",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "BrgyStreet",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "City",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "Gender",
                table: "AspNetUsers");

            migrationBuilder.RenameColumn(
                name: "City",
                table: "LaundryShops",
                newName: "Address");
        }
    }
}
