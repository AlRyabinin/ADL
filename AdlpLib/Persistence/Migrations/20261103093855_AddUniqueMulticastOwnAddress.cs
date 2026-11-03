using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AdlpLib.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddUniqueMulticastOwnAddress : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_DeviceMulticastAddresses_MultiCastAdress",
                table: "DeviceMulticastAddresses",
                column: "MultiCastAdress",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_DeviceMulticastAddresses_MultiCastAdress",
                table: "DeviceMulticastAddresses");
        }
    }
}
