using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AdlpLib.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class RestoreMulticastIdentityToDeviceId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            bool isMySql = ActiveProvider == "Pomelo.EntityFrameworkCore.MySql";

            string addressType = isMySql ? "VARCHAR(15)" : "TEXT";

            // В текущей MySQL-базе Devices.Id имеет тип INT.
            // INTEGER в миграции Pomelo соответствует этому типу.

            // В SQLite старые таблицы ещё имеют FK на MulticastDevices,
            // поэтому удаляем их перед переименованием.
            // В текущей MySQL-базе эти FK уже были удалены предыдущей
            // неудачной попыткой миграции.
            if(!isMySql)
            {
                migrationBuilder.DropForeignKey(
                    name: "FK_DeviceMulticastAddresses_MulticastDevices_DeviceMac",
                    table: "DeviceMulticastAddresses");

                migrationBuilder.DropForeignKey(
                    name: "FK_DeviceMulticastSubscriptions_MulticastDevices_DeviceMac",
                    table: "DeviceMulticastSubscriptions");
            }

            // Сохраняем старые таблицы целиком.
            // В них находится настройка 239.235.1.10,
            // для которой пока невозможно определить DeviceId.
            migrationBuilder.RenameTable(
                name: "DeviceMulticastAddresses",
                newName: "DeviceMulticastAddresses_LegacyMac");

            migrationBuilder.RenameTable(
                name: "DeviceMulticastSubscriptions",
                newName: "DeviceMulticastSubscriptions_LegacyMac");

            // Старый реестр больше не нужен.
            // IF EXISTS — таблица в текущей базе уже отсутствует.
            migrationBuilder.Sql("DROP TABLE IF EXISTS MulticastDevices;");

            // Создаём новую таблицу собственного адреса.
            migrationBuilder.CreateTable(
                name: "DeviceMulticastAddresses",
                columns: table => new
                {
                    DeviceId = table.Column<long>(
                        type: "INTEGER",
                        nullable: false),

                    MultiCastAdress = table.Column<string>(
                        type: addressType,
                        maxLength: 15,
                        nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey(
                        "PK_DeviceMulticastAddresses",
                        x => x.DeviceId);

                    table.ForeignKey(
                        name: "FK_DeviceMulticastAddresses_Devices_DeviceId",
                        column: x => x.DeviceId,
                        principalTable: "Devices",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            // Создаём новую таблицу подписок.
            migrationBuilder.CreateTable(
                name: "DeviceMulticastSubscriptions",
                columns: table => new
                {
                    DeviceId = table.Column<long>(
                        type: "INTEGER",
                        nullable: false),

                    Slot = table.Column<int>(
                        type: "INTEGER",
                        nullable: false),

                    MulticastAddress = table.Column<string>(
                        type: addressType,
                        maxLength: 15,
                        nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey(
                        "PK_DeviceMulticastSubscriptions",
                        x => new { x.DeviceId, x.Slot });

                    table.CheckConstraint(
                        "CK_DeviceMulticastSubscriptions_DeviceIdSlot",
                        "Slot >= 1 AND Slot <= 32");

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
            migrationBuilder.DropForeignKey(
                name: "FK_DeviceMulticastAddresses_Devices_DeviceId",
                table: "DeviceMulticastAddresses");

            migrationBuilder.DropForeignKey(
                name: "FK_DeviceMulticastSubscriptions_Devices_DeviceId",
                table: "DeviceMulticastSubscriptions");

            migrationBuilder.DropPrimaryKey(
                name: "PK_DeviceMulticastSubscriptions",
                table: "DeviceMulticastSubscriptions");

            migrationBuilder.DropIndex(
                name: "IX_DeviceMulticastSubscriptions_DeviceId_MulticastAddress",
                table: "DeviceMulticastSubscriptions");

            migrationBuilder.DropCheckConstraint(
                name: "CK_DeviceMulticastSubscriptions_DeviceIdSlot",
                table: "DeviceMulticastSubscriptions");

            migrationBuilder.DropPrimaryKey(
                name: "PK_DeviceMulticastAddresses",
                table: "DeviceMulticastAddresses");

            migrationBuilder.DropColumn(
                name: "DeviceId",
                table: "DeviceMulticastSubscriptions");

            migrationBuilder.DropColumn(
                name: "DeviceId",
                table: "DeviceMulticastAddresses");

            migrationBuilder.AddColumn<string>(
                name: "DeviceMac",
                table: "DeviceMulticastSubscriptions",
                type: "TEXT",
                maxLength: 17,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "DeviceMac",
                table: "DeviceMulticastAddresses",
                type: "TEXT",
                maxLength: 17,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddPrimaryKey(
                name: "PK_DeviceMulticastSubscriptions",
                table: "DeviceMulticastSubscriptions",
                columns: new[] { "DeviceMac", "Slot" });

            migrationBuilder.AddPrimaryKey(
                name: "PK_DeviceMulticastAddresses",
                table: "DeviceMulticastAddresses",
                column: "DeviceMac");

            migrationBuilder.CreateTable(
                name: "MulticastDevices",
                columns: table => new
                {
                    Mac = table.Column<string>(type: "TEXT", maxLength: 17, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MulticastDevices", x => x.Mac);
                });

            migrationBuilder.CreateIndex(
                name: "IX_DeviceMulticastSubscriptions_DeviceMac_MulticastAddress",
                table: "DeviceMulticastSubscriptions",
                columns: new[] { "DeviceMac", "MulticastAddress" },
                unique: true);

            migrationBuilder.AddCheckConstraint(
                name: "CK_DeviceMulticastSubscriptions_Slot",
                table: "DeviceMulticastSubscriptions",
                sql: "Slot >= 1 AND Slot <= 32");

            migrationBuilder.AddForeignKey(
                name: "FK_DeviceMulticastAddresses_MulticastDevices_DeviceMac",
                table: "DeviceMulticastAddresses",
                column: "DeviceMac",
                principalTable: "MulticastDevices",
                principalColumn: "Mac",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_DeviceMulticastSubscriptions_MulticastDevices_DeviceMac",
                table: "DeviceMulticastSubscriptions",
                column: "DeviceMac",
                principalTable: "MulticastDevices",
                principalColumn: "Mac",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
