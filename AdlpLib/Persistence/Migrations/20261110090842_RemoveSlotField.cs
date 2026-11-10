using Microsoft.EntityFrameworkCore.Migrations;
using System;

#nullable disable

namespace AdlpLib.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class RemoveSlotField : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            if(ActiveProvider == "Pomelo.EntityFrameworkCore.MySql")
            {
                migrationBuilder.Sql(@"
                    SET @has_ck := (SELECT COUNT(*) FROM information_schema.TABLE_CONSTRAINTS
                        WHERE CONSTRAINT_SCHEMA = DATABASE()
                          AND TABLE_NAME = 'DeviceMulticastSubscriptions'
                          AND CONSTRAINT_NAME = 'CK_DeviceMulticastSubscriptions_DeviceIdSlot'
                          AND CONSTRAINT_TYPE = 'CHECK');
                    SET @sql := IF(@has_ck > 0,
                        'ALTER TABLE `DeviceMulticastSubscriptions` DROP CHECK `CK_DeviceMulticastSubscriptions_DeviceIdSlot`',
                        'DO 0');
                    PREPARE s FROM @sql; EXECUTE s; DEALLOCATE PREPARE s;");

                migrationBuilder.Sql(@"
                    ALTER TABLE `DeviceMulticastSubscriptions`
                        DROP PRIMARY KEY,
                        DROP INDEX `IX_DeviceMulticastSubscriptions_DeviceId_MulticastAddress`,
                        DROP COLUMN `Slot`,
                        ADD PRIMARY KEY (`DeviceId`, `MulticastAddress`);");
            }
            else
            {
                migrationBuilder.DropPrimaryKey("PK_DeviceMulticastSubscriptions", "DeviceMulticastSubscriptions");
                migrationBuilder.DropIndex("IX_DeviceMulticastSubscriptions_DeviceId_MulticastAddress", "DeviceMulticastSubscriptions");
                migrationBuilder.DropCheckConstraint("CK_DeviceMulticastSubscriptions_DeviceIdSlot", "DeviceMulticastSubscriptions");
                migrationBuilder.DropColumn("Slot", "DeviceMulticastSubscriptions");
                migrationBuilder.AddPrimaryKey("PK_DeviceMulticastSubscriptions", "DeviceMulticastSubscriptions",
                    new[] { "DeviceId", "MulticastAddress" });
            }
        }

        protected override void Down(MigrationBuilder migrationBuilder) =>
            throw new NotSupportedException("Slot нельзя восстановить: порядок подписок не хранился.");
    }
}
