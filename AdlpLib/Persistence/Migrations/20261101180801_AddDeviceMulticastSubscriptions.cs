using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AdlpLib.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddDeviceMulticastSubscriptions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "DeviceMulticastSubscriptions",
                columns: table => new
                {
                    DeviceId = table.Column<long>(type: "INTEGER", nullable: false),
                    Slot = table.Column<int>(type: "INTEGER", nullable: false),
                    MulticastAddress = table.Column<string>(type: "TEXT", maxLength: 15, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DeviceMulticastSubscriptions", x => new { x.DeviceId, x.Slot });
                    table.ForeignKey(
                        name: "FK_DeviceMulticastSubscriptions_Devices_DeviceId",
                        column: x => x.DeviceId,
                        principalTable: "Devices",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_DeviceMulticastSubscriptions_DeviceId_MulticastAddress",
                table: "DeviceMulticastSubscriptions",
                columns: new[] { "DeviceId", "MulticastAddress" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DeviceMulticastSubscriptions");
        }
    }
}
