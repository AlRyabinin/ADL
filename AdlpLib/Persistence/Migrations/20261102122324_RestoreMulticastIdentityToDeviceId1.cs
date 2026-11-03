using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AdlpLib.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class RestoreMulticastIdentityToDeviceId1 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            bool isMySql =
                ActiveProvider == "Pomelo.EntityFrameworkCore.MySql";

            migrationBuilder.AddColumn<Guid>(
                name: "Uuid",
                table: "Devices",
                type: isMySql ? "CHAR(36)" : "TEXT",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Devices_Uuid_Block",
                table: "Devices",
                columns: new[] { "Uuid", "Block" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Devices_Uuid_Block",
                table: "Devices");

            migrationBuilder.DropColumn(
                name: "Uuid",
                table: "Devices");
        }
    }
}
