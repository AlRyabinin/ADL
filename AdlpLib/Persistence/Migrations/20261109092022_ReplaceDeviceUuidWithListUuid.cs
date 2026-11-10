using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AdlpLib.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class ReplaceDeviceUuidWithListUuid : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "PKlistUUID",
                table: "Devices",
                type: "char(32)",
                fixedLength: true,
                maxLength: 32,
                nullable: true,
                collation: "utf8_general_ci");

            if(ActiveProvider == "Pomelo.EntityFrameworkCore.MySql")
            {
                migrationBuilder.Sql(@"
            ALTER TABLE `Devices`
            MODIFY COLUMN `PKlistUUID`
            CHAR(32)
            CHARACTER SET utf8
            COLLATE utf8_general_ci
            NULL;
        ");
            }

            migrationBuilder.CreateIndex(
                name: "IX_Devices_PKlistUUID_Block",
                table: "Devices",
                columns: new[] { "PKlistUUID", "Block" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Devices_listUUID_PKlistUUID",
                table: "Devices",
                column: "PKlistUUID",
                principalTable: "listUUID",
                principalColumn: "PKlistUUID",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Devices_listUUID_PKlistUUID",
                table: "Devices");

            migrationBuilder.DropIndex(
                name: "IX_Devices_PKlistUUID_Block",
                table: "Devices");

            migrationBuilder.DropColumn(
                name: "PKlistUUID",
                table: "Devices");

            migrationBuilder.AddColumn<Guid>(
                name: "Uuid",
                table: "Devices",
                type: "TEXT",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Devices_Uuid_Block",
                table: "Devices",
                columns: new[] { "Uuid", "Block" },
                unique: true);
        }
    }
}
