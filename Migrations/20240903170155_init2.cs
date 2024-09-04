using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LaundryDashAPI_2.Migrations
{
    /// <inheritdoc />
    public partial class init2 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "LaundryServiceLogs",
                columns: table => new
                {
                    LaundryServiceLogId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    LaundryShopId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ServiceId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LaundryServiceLogs", x => x.LaundryServiceLogId);
                    table.ForeignKey(
                        name: "FK_LaundryServiceLogs_LaundryShops_LaundryShopId",
                        column: x => x.LaundryShopId,
                        principalTable: "LaundryShops",
                        principalColumn: "LaundryShopId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_LaundryServiceLogs_Services_ServiceId",
                        column: x => x.ServiceId,
                        principalTable: "Services",
                        principalColumn: "ServiceId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_LaundryServiceLogs_LaundryShopId",
                table: "LaundryServiceLogs",
                column: "LaundryShopId");

            migrationBuilder.CreateIndex(
                name: "IX_LaundryServiceLogs_ServiceId",
                table: "LaundryServiceLogs",
                column: "ServiceId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "LaundryServiceLogs");
        }
    }
}
