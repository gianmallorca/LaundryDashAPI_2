using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LaundryDashAPI_2.Migrations
{
    /// <inheritdoc />
    public partial class nullableMaps : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_LaundryServiceLogs_Services_ServiceId",
                table: "LaundryServiceLogs");

            migrationBuilder.AlterColumn<Guid>(
                name: "ServiceId",
                table: "LaundryServiceLogs",
                type: "uniqueidentifier",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier");

            migrationBuilder.AddForeignKey(
                name: "FK_LaundryServiceLogs_Services_ServiceId",
                table: "LaundryServiceLogs",
                column: "ServiceId",
                principalTable: "Services",
                principalColumn: "ServiceId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_LaundryServiceLogs_Services_ServiceId",
                table: "LaundryServiceLogs");

            migrationBuilder.AlterColumn<Guid>(
                name: "ServiceId",
                table: "LaundryServiceLogs",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier",
                oldNullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_LaundryServiceLogs_Services_ServiceId",
                table: "LaundryServiceLogs",
                column: "ServiceId",
                principalTable: "Services",
                principalColumn: "ServiceId",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
