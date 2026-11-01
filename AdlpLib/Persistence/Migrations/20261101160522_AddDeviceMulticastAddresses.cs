using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AdlpLib.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddDeviceMulticastAddresses : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "Local",
                table: "PuPictures",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "Local",
                table: "PuGroups",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "ChannelAdlpIndex",
                table: "PuGroupChannels",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "Local",
                table: "PuChannels",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "Local",
                table: "PuButtons",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "ChannelLocal",
                table: "PuButtonChannels",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "Local",
                table: "NetGatePictures",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "Local",
                table: "NetGateConveyors",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "Local",
                table: "NetGateChannels",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "StreamIndex",
                table: "ExecStreams",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "StreamIndex",
                table: "ExecChannelStreams",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "Local",
                table: "ExecChannels",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedAt",
                table: "Devices",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "SID",
                table: "Devices",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedAt",
                table: "Devices",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedAt",
                table: "DeviceGroups",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedAt",
                table: "DeviceGroups",
                type: "TEXT",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "DeviceMulticastAddresses",
                columns: table => new
                {
                    DeviceId = table.Column<long>(type: "INTEGER", nullable: false),
                    MultiCastAdress = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DeviceMulticastAddresses", x => x.DeviceId);
                    table.ForeignKey(
                        name: "FK_DeviceMulticastAddresses_Devices_DeviceId",
                        column: x => x.DeviceId,
                        principalTable: "Devices",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PuDevicePictures_PictureId",
                table: "PuDevicePictures",
                column: "PictureId");

            migrationBuilder.CreateIndex(
                name: "IX_PuDeviceGroups_GroupId",
                table: "PuDeviceGroups",
                column: "GroupId");

            migrationBuilder.CreateIndex(
                name: "IX_PuDeviceChannels_ChannelId",
                table: "PuDeviceChannels",
                column: "ChannelId");

            migrationBuilder.CreateIndex(
                name: "IX_PuDeviceButtons_ButtonId",
                table: "PuDeviceButtons",
                column: "ButtonId");

            migrationBuilder.CreateIndex(
                name: "IX_NetGatePictures_DeviceId",
                table: "NetGatePictures",
                column: "DeviceId");

            migrationBuilder.CreateIndex(
                name: "IX_NetGateConveyors_DeviceId",
                table: "NetGateConveyors",
                column: "DeviceId");

            migrationBuilder.CreateIndex(
                name: "IX_NetGateChannels_DeviceId",
                table: "NetGateChannels",
                column: "DeviceId");

            migrationBuilder.CreateIndex(
                name: "IX_ExecDeviceStreams_StreamId",
                table: "ExecDeviceStreams",
                column: "StreamId");

            migrationBuilder.CreateIndex(
                name: "IX_ExecDeviceChannels_ChannelId",
                table: "ExecDeviceChannels",
                column: "ChannelId");

            migrationBuilder.CreateIndex(
                name: "IX_DeviceGroupDevices_DeviceId",
                table: "DeviceGroupDevices",
                column: "DeviceId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DeviceMulticastAddresses");

            migrationBuilder.DropIndex(
                name: "IX_PuDevicePictures_PictureId",
                table: "PuDevicePictures");

            migrationBuilder.DropIndex(
                name: "IX_PuDeviceGroups_GroupId",
                table: "PuDeviceGroups");

            migrationBuilder.DropIndex(
                name: "IX_PuDeviceChannels_ChannelId",
                table: "PuDeviceChannels");

            migrationBuilder.DropIndex(
                name: "IX_PuDeviceButtons_ButtonId",
                table: "PuDeviceButtons");

            migrationBuilder.DropIndex(
                name: "IX_NetGatePictures_DeviceId",
                table: "NetGatePictures");

            migrationBuilder.DropIndex(
                name: "IX_NetGateConveyors_DeviceId",
                table: "NetGateConveyors");

            migrationBuilder.DropIndex(
                name: "IX_NetGateChannels_DeviceId",
                table: "NetGateChannels");

            migrationBuilder.DropIndex(
                name: "IX_ExecDeviceStreams_StreamId",
                table: "ExecDeviceStreams");

            migrationBuilder.DropIndex(
                name: "IX_ExecDeviceChannels_ChannelId",
                table: "ExecDeviceChannels");

            migrationBuilder.DropIndex(
                name: "IX_DeviceGroupDevices_DeviceId",
                table: "DeviceGroupDevices");

            migrationBuilder.DropColumn(
                name: "Local",
                table: "PuPictures");

            migrationBuilder.DropColumn(
                name: "Local",
                table: "PuGroups");

            migrationBuilder.DropColumn(
                name: "ChannelAdlpIndex",
                table: "PuGroupChannels");

            migrationBuilder.DropColumn(
                name: "Local",
                table: "PuChannels");

            migrationBuilder.DropColumn(
                name: "Local",
                table: "PuButtons");

            migrationBuilder.DropColumn(
                name: "ChannelLocal",
                table: "PuButtonChannels");

            migrationBuilder.DropColumn(
                name: "Local",
                table: "NetGatePictures");

            migrationBuilder.DropColumn(
                name: "Local",
                table: "NetGateConveyors");

            migrationBuilder.DropColumn(
                name: "Local",
                table: "NetGateChannels");

            migrationBuilder.DropColumn(
                name: "StreamIndex",
                table: "ExecStreams");

            migrationBuilder.DropColumn(
                name: "StreamIndex",
                table: "ExecChannelStreams");

            migrationBuilder.DropColumn(
                name: "Local",
                table: "ExecChannels");

            migrationBuilder.DropColumn(
                name: "CreatedAt",
                table: "Devices");

            migrationBuilder.DropColumn(
                name: "SID",
                table: "Devices");

            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                table: "Devices");

            migrationBuilder.DropColumn(
                name: "CreatedAt",
                table: "DeviceGroups");

            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                table: "DeviceGroups");
        }
    }
}
