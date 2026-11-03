using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AdlpLib.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class MoveMulticastIdentityToMac : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            bool isMySql = ActiveProvider == "Pomelo.EntityFrameworkCore.MySql";

            string macType = isMySql ? "VARCHAR(17)" : "TEXT";
            string ownAddressType = isMySql ? "VARCHAR(15)" : "TEXT";
            string subscriptionAddressType = isMySql ? "VARCHAR(15)" : "TEXT";

            string macSql = NormalizeMacSql("d.Mac");
            string compactMacSql = CompactMacSql("d.Mac");

            // Убираем промежуточные таблицы от возможного неудачного запуска.
            migrationBuilder.Sql(
                "DROP TABLE IF EXISTS _DeviceMulticastSubscriptions_New;");

            migrationBuilder.Sql(
                "DROP TABLE IF EXISTS _DeviceMulticastAddresses_New;");

            migrationBuilder.Sql(
                "DROP TABLE IF EXISTS MulticastDevices;");

            // ---------------------------------------------------------
            // 1. Создаём таблицу устройств, идентифицируемых по MAC.
            // ---------------------------------------------------------

            migrationBuilder.CreateTable(
                name: "MulticastDevices",
                columns: table => new
                {
                    Mac = table.Column<string>(
                        type: macType,
                        maxLength: 17,
                        nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MulticastDevices", x => x.Mac);
                });

            // ---------------------------------------------------------
            // 2. Создаём временную таблицу собственного адреса.
            // ---------------------------------------------------------

            migrationBuilder.CreateTable(
                name: "_DeviceMulticastAddresses_New",
                columns: table => new
                {
                    DeviceMac = table.Column<string>(
                        type: macType,
                        maxLength: 17,
                        nullable: false),

                    MultiCastAdress = table.Column<string>(
                        type: ownAddressType,
                        maxLength: 15,
                        nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey(
                        "PK_DeviceMulticastAddresses",
                        x => x.DeviceMac);

                    table.ForeignKey(
                        name: "FK_DeviceMulticastAddresses_MulticastDevices_DeviceMac",
                        column: x => x.DeviceMac,
                        principalTable: "MulticastDevices",
                        principalColumn: "Mac",
                        onDelete: ReferentialAction.Cascade);
                });

            // ---------------------------------------------------------
            // 3. Создаём временную таблицу подписок.
            // ---------------------------------------------------------

            migrationBuilder.CreateTable(
                name: "_DeviceMulticastSubscriptions_New",
                columns: table => new
                {
                    DeviceMac = table.Column<string>(
                        type: macType,
                        maxLength: 17,
                        nullable: false),

                    Slot = table.Column<int>(
                        type: "INTEGER",
                        nullable: false),

                    MulticastAddress = table.Column<string>(
                        type: subscriptionAddressType,
                        maxLength: 15,
                        nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey(
                        "PK_DeviceMulticastSubscriptions",
                        x => new { x.DeviceMac, x.Slot });

                    table.CheckConstraint(
                        "CK_DeviceMulticastSubscriptions_MacSlot",
                        "Slot >= 1 AND Slot <= 32");

                    table.ForeignKey(
                        name: "FK_DeviceMulticastSubscriptions_MulticastDevices_DeviceMac",
                        column: x => x.DeviceMac,
                        principalTable: "MulticastDevices",
                        principalColumn: "Mac",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_DeviceMulticastSubscriptions_DeviceMac_MulticastAddress",
                table: "_DeviceMulticastSubscriptions_New",
                columns: new[] { "DeviceMac", "MulticastAddress" },
                unique: true);

            // ---------------------------------------------------------
            // 4. Переносим MAC из Devices.
            // Учитываем только устройства, связанные с multicast.
            // ---------------------------------------------------------

            migrationBuilder.Sql($@"
                INSERT INTO MulticastDevices (Mac)
                SELECT DISTINCT {macSql}
                FROM Devices d
                WHERE d.Id IN
                (
                    SELECT DeviceId FROM DeviceMulticastAddresses

                    UNION

                    SELECT DeviceId FROM DeviceMulticastSubscriptions
                )
                AND LENGTH({compactMacSql}) = 12;
            ");

                    // ---------------------------------------------------------
                    // 5. Переносим собственные multicast-адреса.
                    // ---------------------------------------------------------

                    migrationBuilder.Sql($@"
                INSERT INTO _DeviceMulticastAddresses_New
                    (DeviceMac, MultiCastAdress)
                SELECT DISTINCT
                    {macSql},
                    a.MultiCastAdress
                FROM DeviceMulticastAddresses a
                INNER JOIN Devices d ON d.Id = a.DeviceId;
            ");

                    // ---------------------------------------------------------
                    // 6. Переносим IGMP-подписки.
                    // Сохраняем номера Slot.
                    // ---------------------------------------------------------

                    migrationBuilder.Sql($@"
                INSERT INTO _DeviceMulticastSubscriptions_New
                    (DeviceMac, Slot, MulticastAddress)
                SELECT DISTINCT
                    {macSql},
                    s.Slot,
                    s.MulticastAddress
                FROM DeviceMulticastSubscriptions s
                INNER JOIN Devices d ON d.Id = s.DeviceId;
            ");

            // ---------------------------------------------------------
            // 7. Удаляем старые таблицы.
            // Данные уже скопированы во временные таблицы.
            // ---------------------------------------------------------

            migrationBuilder.DropTable(
                name: "DeviceMulticastSubscriptions");

            migrationBuilder.DropTable(
                name: "DeviceMulticastAddresses");

            // ---------------------------------------------------------
            // 8. Переносим временные таблицы на постоянные имена.
            // ---------------------------------------------------------

            migrationBuilder.RenameTable(
                name: "_DeviceMulticastAddresses_New",
                newName: "DeviceMulticastAddresses");

            migrationBuilder.RenameTable(
                name: "_DeviceMulticastSubscriptions_New",
                newName: "DeviceMulticastSubscriptions");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            bool isMySql = ActiveProvider == "Pomelo.EntityFrameworkCore.MySql";

            string macType = isMySql ? "VARCHAR(17)" : "TEXT";
            string ownAddressType = isMySql ? "VARCHAR(64)" : "TEXT";
            string subscriptionAddressType = isMySql ? "VARCHAR(15)" : "TEXT";

            string macSql = NormalizeMacSql("d.Mac");

            migrationBuilder.Sql(
                "DROP TABLE IF EXISTS _DeviceMulticastRollbackMap;");

            migrationBuilder.Sql(
                "DROP TABLE IF EXISTS _DeviceMulticastAddresses_Old;");

            migrationBuilder.Sql(
                "DROP TABLE IF EXISTS _DeviceMulticastSubscriptions_Old;");

            // ---------------------------------------------------------
            // 1. Таблица соответствия MAC -> DeviceId.
            // DeviceId должен существовать и соответствовать ровно
            // одной записи в Devices.
            // ---------------------------------------------------------

            migrationBuilder.CreateTable(
                name: "_DeviceMulticastRollbackMap",
                columns: table => new
                {
                    Mac = table.Column<string>(
                        type: macType,
                        maxLength: 17,
                        nullable: false),

                    DeviceId = table.Column<long>(
                        type: "INTEGER",
                        nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey(
                        "PK_DeviceMulticastRollbackMap",
                        x => x.Mac);

                    table.CheckConstraint(
                        "CK_DeviceMulticastRollbackMap_DeviceId",
                        "DeviceId > 0");

                    table.ForeignKey(
                        name: "FK_DeviceMulticastRollbackMap_Devices_DeviceId",
                        column: x => x.DeviceId,
                        principalTable: "Devices",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            // ---------------------------------------------------------
            // 2. Создаём временные старые таблицы.
            // ---------------------------------------------------------

            migrationBuilder.CreateTable(
                name: "_DeviceMulticastAddresses_Old",
                columns: table => new
                {
                    DeviceId = table.Column<long>(
                        type: "INTEGER",
                        nullable: false),

                    MultiCastAdress = table.Column<string>(
                        type: ownAddressType,
                        maxLength: 64,
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

            migrationBuilder.CreateTable(
                name: "_DeviceMulticastSubscriptions_Old",
                columns: table => new
                {
                    DeviceId = table.Column<long>(
                        type: "INTEGER",
                        nullable: false),

                    Slot = table.Column<int>(
                        type: "INTEGER",
                        nullable: false),

                    MulticastAddress = table.Column<string>(
                        type: subscriptionAddressType,
                        maxLength: 15,
                        nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey(
                        "PK_DeviceMulticastSubscriptions",
                        x => new { x.DeviceId, x.Slot });

                    table.CheckConstraint(
                        "CK_DeviceMulticastSubscriptions_Slot",
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
                table: "_DeviceMulticastSubscriptions_Old",
                columns: new[] { "DeviceId", "MulticastAddress" },
                unique: true);

            // ---------------------------------------------------------
            // 3. Проверяем соответствие каждого MAC устройству.
            //
            // COUNT = 1  -> допустимо.
            // COUNT = 0  -> нет устройства.
            // COUNT > 1  -> неоднозначное соответствие.
            //
            // В двух последних случаях DeviceId = NULL,
            // что вызовет ошибку NOT NULL до удаления новых таблиц.
            // ---------------------------------------------------------

            migrationBuilder.Sql($@"
                INSERT INTO _DeviceMulticastRollbackMap (Mac, DeviceId)
                SELECT
                    m.Mac,
                    CASE
                        WHEN COUNT(d.Id) = 1 THEN MIN(d.Id)
                        ELSE NULL
                    END
                FROM MulticastDevices m
                LEFT JOIN Devices d
                    ON {StringEqualsSql(macSql, "m.Mac")}
                GROUP BY m.Mac;
            ");

                    // ---------------------------------------------------------
                    // 4. Восстанавливаем собственные адреса.
                    // ---------------------------------------------------------

                    migrationBuilder.Sql($@"
                INSERT INTO _DeviceMulticastAddresses_Old
                    (DeviceId, MultiCastAdress)
                SELECT
                    map.DeviceId,
                    a.MultiCastAdress
                FROM DeviceMulticastAddresses a
                INNER JOIN _DeviceMulticastRollbackMap map
                    ON {StringEqualsSql("map.Mac", "a.DeviceMac")}
            ");

                    // ---------------------------------------------------------
                    // 5. Восстанавливаем подписки.
                    // ---------------------------------------------------------

                    migrationBuilder.Sql(@$"
                INSERT INTO _DeviceMulticastSubscriptions_Old
                    (DeviceId, Slot, MulticastAddress)
                SELECT
                    map.DeviceId,
                    s.Slot,
                    s.MulticastAddress
                FROM DeviceMulticastSubscriptions s
                INNER JOIN _DeviceMulticastRollbackMap map
                    ON {StringEqualsSql("map.Mac", "s.DeviceMac")}
            ");

            // ---------------------------------------------------------
            // 6. Удаляем новые таблицы.
            // ---------------------------------------------------------

            migrationBuilder.DropTable(
                name: "DeviceMulticastSubscriptions");

            migrationBuilder.DropTable(
                name: "DeviceMulticastAddresses");

            migrationBuilder.DropTable(
                name: "MulticastDevices");

            migrationBuilder.DropTable(
                name: "_DeviceMulticastRollbackMap");

            // ---------------------------------------------------------
            // 7. Восстанавливаем старые имена таблиц.
            // ---------------------------------------------------------

            migrationBuilder.RenameTable(
                name: "_DeviceMulticastAddresses_Old",
                newName: "DeviceMulticastAddresses");

            migrationBuilder.RenameTable(
                name: "_DeviceMulticastSubscriptions_Old",
                newName: "DeviceMulticastSubscriptions");
        }

        private string CompactMacSql(string column)
        {
            return $"REPLACE(REPLACE(REPLACE(TRIM({column}), ':', ''), '-', ''), '.', '')";
        }

        private string NormalizeMacSql(string column)
        {
            string compact = CompactMacSql(column);

            if(ActiveProvider == "Pomelo.EntityFrameworkCore.MySql")
            {
                return $@"
            UPPER(CONCAT(
                SUBSTRING({compact}, 1, 2), ':',
                SUBSTRING({compact}, 3, 2), ':',
                SUBSTRING({compact}, 5, 2), ':',
                SUBSTRING({compact}, 7, 2), ':',
                SUBSTRING({compact}, 9, 2), ':',
                SUBSTRING({compact}, 11, 2)
            ))";
            }

            return $@"
        UPPER(
            SUBSTR({compact}, 1, 2) || ':' ||
            SUBSTR({compact}, 3, 2) || ':' ||
            SUBSTR({compact}, 5, 2) || ':' ||
            SUBSTR({compact}, 7, 2) || ':' ||
            SUBSTR({compact}, 9, 2) || ':' ||
            SUBSTR({compact}, 11, 2)
        )";
        }

        private string StringEqualsSql(string left, string right)
        {
            if(ActiveProvider == "Pomelo.EntityFrameworkCore.MySql")
                return $"BINARY ({left}) = BINARY ({right})";

            return $"({left}) = ({right})";
        }
    }
}
