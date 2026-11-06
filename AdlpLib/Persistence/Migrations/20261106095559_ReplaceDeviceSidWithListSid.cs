using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AdlpLib.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class ReplaceDeviceSidWithListSid : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "SID",
                table: "Devices");

            migrationBuilder.AddColumn<string>(
                name: "PKlistSID",
                table: "Devices",
                type: ActiveProvider == "Pomelo.EntityFrameworkCore.MySql"
                    ? "char(8)"
                    : "TEXT",
                fixedLength: true,
                maxLength: 8,
                nullable: true);

            // Для MySQL приводим кодировку к существующему listSID.PKlistSID.
            if(ActiveProvider == "Pomelo.EntityFrameworkCore.MySql")
            {
                migrationBuilder.Sql(@"
            ALTER TABLE `Devices`
            MODIFY COLUMN `PKlistSID`
            CHAR(8)
            CHARACTER SET utf8
            COLLATE utf8_general_ci
            NULL;
        ");
            }

            migrationBuilder.CreateIndex(
                name: "IX_Devices_PKlistSID",
                table: "Devices",
                column: "PKlistSID");

            migrationBuilder.AddForeignKey(
                name: "FK_Devices_listSID_PKlistSID",
                table: "Devices",
                column: "PKlistSID",
                principalTable: "listSID",
                principalColumn: "PKlistSID",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Devices_listSID_PKlistSID",
                table: "Devices");

            migrationBuilder.DropIndex(
                name: "IX_Devices_PKlistSID",
                table: "Devices");

            migrationBuilder.DropColumn(
                name: "PKlistSID",
                table: "Devices");

            if(ActiveProvider == "Pomelo.EntityFrameworkCore.MySql")
            {
                migrationBuilder.AddColumn<string>(
                    name: "SID",
                    table: "Devices",
                    type: "varchar(64)",
                    maxLength: 64,
                    nullable: true);
            }
            else
            {
                migrationBuilder.AddColumn<int>(
                    name: "SID",
                    table: "Devices",
                    type: "INTEGER",
                    nullable: true);
            }
        }
    }
}
