using AdlpLib.Core;
using AdlpLib.LiveConfig;
using AdlpLib.Models.Devices;
using AdlpLib.Models.Devices.Exec;
using AdlpLib.Models.Devices.PU;
using Microsoft.Data.Sqlite;
using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace AdlpLib.Configuration.Application
{
    /// <summary>
    /// Краткие сведения об устройстве для отображения в списке.
    /// </summary>
    public class DeviceHeader
    {
        public long Id { get; set; }
        public string ConfigType { get; set; } = "";
        public string Name { get; set; } = "";
        public string Mac { get; set; } = "";
        public string Ip { get; set; } = "";
        public string Mask { get; set; } = "";
        public string Gateway { get; set; } = "";
        public int Block { get; set; }

        public override string ToString() => $"[{ConfigType}] {Name} [{Mac}]";
    }

    /// <summary>
    /// Асинхронный репозиторий сохранения/загрузки конфигураций устройств (PU и EXEC).
    /// Работает и с SQLite, и с MySQL — в зависимости от <see cref="DatabaseType"/>.
    /// </summary>
    public class DeviceConfigRepository
    {
        private const string TypePu = "PU";
        private const string TypeExec = "EXEC";

        private readonly string _connectionString;
        private readonly DatabaseType _dbType;

        public DeviceConfigRepository(string connectionString, DatabaseType dbType)
        {
            _connectionString = connectionString;
            _dbType = dbType;
        }

        // =========================================================================
        //  СХЕМА
        // =========================================================================

        /// <summary>
        /// Создаёт таблицы (если их нет) и проверяет, что существующие соответствуют
        /// ожидаемой структуре.
        /// </summary>
        public async Task EnsureSchemaAsync(CancellationToken ct = default)
        {
            await using var conn = await OpenConnectionAsync(ct).ConfigureAwait(false);

            foreach(var ddl in GetDdlBatches())
            {
                await using var cmd = conn.CreateCommand();
                cmd.CommandText = ddl;
                await cmd.ExecuteNonQueryAsync(ct).ConfigureAwait(false);
            }

            await EnsureAuditColumnsAsync(conn, ct).ConfigureAwait(false);
            await EnsureAuditTriggersAsync(conn, ct).ConfigureAwait(false);

            await ValidateSchemaAsync(conn, ct).ConfigureAwait(false);
        }

        /// <summary>
        /// Добавляет в Devices колонки аудита, если их ещё нет (для баз, созданных
        /// до появления триггеров).
        /// </summary>
        private async Task EnsureAuditColumnsAsync(DbConnection conn, CancellationToken ct)
        {
            var needed = new (string Name, string SqliteType, string MySqlType, bool NotNull)[]
            {
                ("Name",      "TEXT",         "VARCHAR(255)",  true),
                ("CreatedAt", "TEXT",         "DATETIME",      false),
                ("CreatedBy", "TEXT",         "VARCHAR(100)",  false),
                ("UpdatedAt", "TEXT",         "DATETIME",      false),
                ("UpdatedBy", "TEXT",         "VARCHAR(100)",  false)
            };

            var existing = await GetTableColumnsAsync(conn, "Devices", ct).ConfigureAwait(false);

            foreach(var (name, sqliteType, mySqlType, notNull) in needed)
            {
                if(!existing.Contains(name, StringComparer.OrdinalIgnoreCase))
                {
                    await using var cmd = conn.CreateCommand();
                    string nullability = notNull ? "NOT NULL DEFAULT ''" : "NULL";
                    cmd.CommandText = _dbType == DatabaseType.SQLite
                        ? $"ALTER TABLE Devices ADD COLUMN {name} {sqliteType} {nullability};"
                        : $"ALTER TABLE Devices ADD COLUMN `{name}` {mySqlType} {nullability};";
                    await cmd.ExecuteNonQueryAsync(ct).ConfigureAwait(false);
                }
            }

            await ExecNoTxAsync(conn,
                "UPDATE Devices SET Name = '' WHERE Name IS NULL;", ct).ConfigureAwait(false);
        }

        /// <summary>
        /// Создаёт триггеры аудита. Пересоздаются при каждом запуске — это безопасно,
        /// потому что логика триггера определяется только кодом приложения.
        /// </summary>
        private async Task EnsureAuditTriggersAsync(DbConnection conn, CancellationToken ct)
        {
            if(_dbType == DatabaseType.MySQL)
            {
                await ExecNoTxAsync(conn,
                    "DROP TRIGGER IF EXISTS trg_devices_bi;", ct).ConfigureAwait(false);
                await ExecNoTxAsync(conn, @"
            CREATE TRIGGER trg_devices_bi
            BEFORE INSERT ON Devices
            FOR EACH ROW
            BEGIN
                SET NEW.CreatedAt = UTC_TIMESTAMP();
                SET NEW.UpdatedAt = UTC_TIMESTAMP();
                SET NEW.CreatedBy = COALESCE(@app_user, USER());
                SET NEW.UpdatedBy = COALESCE(@app_user, USER());
            END;", ct).ConfigureAwait(false);

                await ExecNoTxAsync(conn,
                    "DROP TRIGGER IF EXISTS trg_devices_bu;", ct).ConfigureAwait(false);
                await ExecNoTxAsync(conn, @"
            CREATE TRIGGER trg_devices_bu
            BEFORE UPDATE ON Devices
            FOR EACH ROW
            BEGIN
                SET NEW.UpdatedAt = UTC_TIMESTAMP();
                SET NEW.UpdatedBy = COALESCE(@app_user, USER());
                SET NEW.CreatedAt = OLD.CreatedAt;
                SET NEW.CreatedBy = OLD.CreatedBy;
            END;", ct).ConfigureAwait(false);
            }
            else // SQLite
            {
                await ExecNoTxAsync(conn,
                    "DROP TRIGGER IF EXISTS trg_devices_ai;", ct).ConfigureAwait(false);
                await ExecNoTxAsync(conn, @"
            CREATE TRIGGER trg_devices_ai
            AFTER INSERT ON Devices
            FOR EACH ROW
            BEGIN
                UPDATE Devices
                   SET CreatedAt = datetime('now'),
                       UpdatedAt = datetime('now'),
                       CreatedBy = COALESCE((SELECT value FROM _audit_context WHERE key = 'user'), ''),
                       UpdatedBy = COALESCE((SELECT value FROM _audit_context WHERE key = 'user'), '')
                 WHERE Id = NEW.Id;
            END;", ct).ConfigureAwait(false);

                await ExecNoTxAsync(conn,
                    "DROP TRIGGER IF EXISTS trg_devices_au;", ct).ConfigureAwait(false);
                await ExecNoTxAsync(conn, @"
            CREATE TRIGGER trg_devices_au
            AFTER UPDATE ON Devices
            FOR EACH ROW
            WHEN NEW.UpdatedAt IS OLD.UpdatedAt
            BEGIN
                UPDATE Devices
                   SET UpdatedAt = datetime('now'),
                       UpdatedBy = COALESCE((SELECT value FROM _audit_context WHERE key = 'user'), '')
                 WHERE Id = NEW.Id;
            END;", ct).ConfigureAwait(false);
            }
        }

        /// <summary>
        /// Выполняет одиночный SQL без транзакции — для DDL и команд управления.
        /// </summary>
        private static async Task ExecNoTxAsync(DbConnection conn, string sql, CancellationToken ct)
        {
            await using var cmd = conn.CreateCommand();
            cmd.CommandText = sql;
            await cmd.ExecuteNonQueryAsync(ct).ConfigureAwait(false);
        }

        /// <summary>
        /// Проверяет, что в существующих таблицах есть все ожидаемые колонки.
        /// Бросает исключение с понятным текстом, если схема устарела.
        /// </summary>
        private async Task ValidateSchemaAsync(DbConnection conn, CancellationToken ct)
        {
            var required = new Dictionary<string, string[]>
            {
                ["Devices"] = new[] { "Id", "Name", "ConfigType", "Mac", "Ip", "Mask", "Gateway", "Block" },
                ["PuDevices"] = new[] { "DeviceId", "Priority", "Brightness", "SleepTimeout" },
                ["ExecDevices"] = new[] { "DeviceId", "BaseChannel" },
            };

            foreach(var (table, columns) in required)
            {
                var actual = await GetTableColumnsAsync(conn, table, ct).ConfigureAwait(false);
                if(actual.Count == 0) continue;

                var missing = columns
                    .Where(c => !actual.Contains(c, StringComparer.OrdinalIgnoreCase))
                    .ToList();

                if(missing.Count > 0)
                {
                    throw new InvalidOperationException(
                        $"Схема БД устарела. В таблице '{table}' отсутствуют колонки: " +
                        $"{string.Join(", ", missing)}. Удалите таблицы и создайте заново.");
                }
            }
        }

        private async Task<List<string>> GetTableColumnsAsync(DbConnection conn,
                                                              string table,
                                                              CancellationToken ct)
        {
            await using var cmd = conn.CreateCommand();
            cmd.CommandText = _dbType == DatabaseType.SQLite
                ? $"PRAGMA table_info({table});"
                : $@"SELECT COLUMN_NAME FROM INFORMATION_SCHEMA.COLUMNS
                       WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = '{table}';";

            var list = new List<string>();
            await using var r = await cmd.ExecuteReaderAsync(ct).ConfigureAwait(false);
            while(await r.ReadAsync(ct).ConfigureAwait(false))
            {
                list.Add(_dbType == DatabaseType.SQLite ? r.GetString(1) : r.GetString(0));
            }
            return list;
        }

        private IReadOnlyList<string> GetDdlBatches() => _dbType switch
        {
            DatabaseType.SQLite => SqliteDdl,
            DatabaseType.MySQL => MySqlDdl,
            _ => throw new NotSupportedException($"Тип БД {_dbType} не поддержан.")
        };

        // =========================================================================
        //  СОХРАНЕНИЕ
        // =========================================================================

        /// <summary>
        /// Сохраняет устройство. Если <c>Id == 0</c> — вставка, иначе — полная замена
        /// зависимых строк (каналов, потоков, групп, картин, кнопок).
        /// </summary>
        /// <returns>Идентификатор сохранённого устройства.</returns>
        public async Task<long> SaveAsync(DeviceConfigDtoBase dto, CancellationToken ct = default)
        {
            await using var conn = await OpenConnectionAsync(ct).ConfigureAwait(false);
            var tx = await conn.BeginTransactionAsync(ct).ConfigureAwait(false);

            try
            {
                string configType = dto switch
                {
                    PuConfigDto => TypePu,
                    ExecConfigDto => TypeExec,
                    _ => throw new NotSupportedException(
                            $"Тип {dto.GetType().Name} не поддержан.")
                };

                long deviceId = await UpsertDeviceAsync(conn, tx, dto, configType, ct)
                    .ConfigureAwait(false);

                await DeleteDependentsAsync(conn, tx, deviceId, ct).ConfigureAwait(false);

                switch(dto)
                {
                    case PuConfigDto pu:
                        await SavePuDependentsAsync(conn, tx, deviceId, pu, ct).ConfigureAwait(false);
                        break;
                    case ExecConfigDto exec:
                        await SaveExecDependentsAsync(conn, tx, deviceId, exec, ct).ConfigureAwait(false);
                        break;
                }

                await tx.CommitAsync(ct).ConfigureAwait(false);
                dto.Id = deviceId;
                return deviceId;
            }
            catch
            {
                await tx.RollbackAsync(ct).ConfigureAwait(false);
                throw;
            }
        }

        private async Task<long> UpsertDeviceAsync(DbConnection conn, DbTransaction tx,
                                                    DeviceConfigDtoBase dto,
                                                    string configType, CancellationToken ct)
        {
            if(dto.Id == 0)
            {
                await ExecAsync(conn, tx,
                    @"INSERT INTO Devices (Name, ConfigType, Mac, Ip, Mask, Gateway, Block)
                        VALUES (@n, @t, @m, @ip, @mask, @gw, @b);",
                    ct,
                    ("@n", dto.Name ?? ""),
                    ("@t", configType),
                    ("@m", dto.Mac ?? ""),
                    ("@ip", dto.Ip ?? ""),
                    ("@mask", dto.Mask ?? ""),
                    ("@gw", dto.Gateway ?? ""),
                    ("@b", dto.Block)).ConfigureAwait(false);

                return await GetLastInsertIdAsync(conn, tx, ct).ConfigureAwait(false);
            }

            await ExecAsync(conn, tx,
                @"UPDATE Devices
                     SET Name = @n, ConfigType = @t, Mac = @m, Ip = @ip,
                         Mask = @mask, Gateway = @gw, Block = @b
                   WHERE Id = @id;",
                ct,
                ("@n", dto.Name ?? ""),
                ("@t", configType),
                ("@m", dto.Mac ?? ""),
                ("@ip", dto.Ip ?? ""),
                ("@mask", dto.Mask ?? ""),
                ("@gw", dto.Gateway ?? ""),
                ("@b", dto.Block),
                ("@id", dto.Id)).ConfigureAwait(false);

            return dto.Id;
        }

        // ---------- PU ----------

        private async Task SavePuDependentsAsync(DbConnection conn, DbTransaction tx,
                                                  long deviceId, PuConfigDto pu,
                                                  CancellationToken ct)
        {
            // 1. Расширение PuDevices (1-к-1)
            await ExecAsync(conn, tx,
                @"INSERT INTO PuDevices
                    (DeviceId, Priority, PictureSwitchTime, PageCount,
                     SwapX, SwapY, Brightness, BrightnessSleep, SleepTimeout)
                  VALUES
                    (@d, @p, @pst, @pc, @sx, @sy, @br, @bs, @st);",
                ct,
                ("@d", deviceId),
                ("@p", pu.Priority),
                ("@pst", pu.PictureSwitchTime),
                ("@pc", pu.PageCount),
                ("@sx", pu.SwapX),
                ("@sy", pu.SwapY),
                ("@br", pu.Brightness),
                ("@bs", pu.BrightnessSleep),
                ("@st", pu.SleepTimeout)).ConfigureAwait(false);

            // 2. Каналы + link
            if(pu.Channels is { Count: > 0 })
            {
                int pos = 0;
                foreach(var ch in pu.Channels)
                {
                    await ExecAsync(conn, tx,
                        @"INSERT INTO PuChannels (Local, Name, AdlpIndex, Type)
                          VALUES (@l, @n, @i, @t);",
                        ct,
                        ("@l", ch.Local),
                        ("@n", ch.Name ?? ""),
                        ("@i", ch.Index),
                        ("@t", ch.Type)).ConfigureAwait(false);

                    long channelId = await GetLastInsertIdAsync(conn, tx, ct).ConfigureAwait(false);

                    await ExecAsync(conn, tx,
                        @"INSERT INTO PuDeviceChannels (DeviceId, ChannelId, Position)
                          VALUES (@d, @c, @p);",
                        ct,
                        ("@d", deviceId),
                        ("@c", channelId),
                        ("@p", pos++)).ConfigureAwait(false);
                }
            }

            // 3. Группы + link Device↔Group + link Group↔Channel
            if(pu.Groups is { Count: > 0 })
            {
                foreach(var grp in pu.Groups)
                {
                    await ExecAsync(conn, tx,
                        @"INSERT INTO PuGroups (Local, AdlpIndex)
                          VALUES (@l, @i);",
                        ct,
                        ("@l", grp.Local),
                        ("@i", grp.Index)).ConfigureAwait(false);

                    long groupId = await GetLastInsertIdAsync(conn, tx, ct).ConfigureAwait(false);

                    await ExecAsync(conn, tx,
                        @"INSERT INTO PuDeviceGroups (DeviceId, GroupId, Position)
                          VALUES (@d, @g, @p);",
                        ct,
                        ("@d", deviceId),
                        ("@g", groupId),
                        ("@p", grp.Local)).ConfigureAwait(false);

                    if(grp.Channels is { Count: > 0 })
                    {
                        int slot = 1;
                        foreach(var adlpIdx in grp.Channels)
                        {
                            await ExecAsync(conn, tx,
                                @"INSERT INTO PuGroupChannels (GroupId, ChannelAdlpIndex, Slot)
                                  VALUES (@g, @ci, @s);",
                                ct,
                                ("@g", groupId),
                                ("@ci", adlpIdx),
                                ("@s", slot++)).ConfigureAwait(false);
                        }
                    }
                }
            }

            // 4. Картины + link
            if(pu.Pictures is { Count: > 0 })
            {
                int pos = 0;
                foreach(var pic in pu.Pictures)
                {
                    await ExecAsync(conn, tx,
                        @"INSERT INTO PuPictures (Local, Name)
                          VALUES (@l, @n);",
                        ct,
                        ("@l", pic.Local),
                        ("@n", pic.Name ?? "")).ConfigureAwait(false);

                    long picId = await GetLastInsertIdAsync(conn, tx, ct).ConfigureAwait(false);

                    await ExecAsync(conn, tx,
                        @"INSERT INTO PuDevicePictures (DeviceId, PictureId, Position)
                          VALUES (@d, @p, @pos);",
                        ct,
                        ("@d", deviceId),
                        ("@p", picId),
                        ("@pos", pos++)).ConfigureAwait(false);
                }
            }

            // 5. Кнопки + link Device↔Button + link Button↔Channel
            if(pu.Buttons is { Count: > 0 })
            {
                int pos = 0;
                foreach(var btn in pu.Buttons)
                {
                    await ExecAsync(conn, tx,
                        @"INSERT INTO PuButtons
                            (Local, Page, LocalChannel, PosX, PosY, Icon, Color)
                          VALUES
                            (@l, @p, @lc, @x, @y, @i, @c);",
                        ct,
                        ("@l", btn.Local),
                        ("@p", btn.Page),
                        ("@lc", btn.LocalChannel),
                        ("@x", btn.PosX),
                        ("@y", btn.PosY),
                        ("@i", btn.Icon),
                        ("@c", btn.Color)).ConfigureAwait(false);

                    long btnId = await GetLastInsertIdAsync(conn, tx, ct).ConfigureAwait(false);

                    await ExecAsync(conn, tx,
                        @"INSERT INTO PuDeviceButtons (DeviceId, ButtonId, Position)
                          VALUES (@d, @b, @p);",
                        ct,
                        ("@d", deviceId),
                        ("@b", btnId),
                        ("@p", pos++)).ConfigureAwait(false);

                    if(btn.LocalChannel > 0)
                    {
                        await ExecAsync(conn, tx,
                            @"INSERT INTO PuButtonChannels (DeviceId, ButtonId, ChannelLocal)
                              VALUES (@d, @b, @cl);",
                            ct,
                            ("@d", deviceId),
                            ("@b", btnId),
                            ("@cl", btn.LocalChannel)).ConfigureAwait(false);
                    }
                }
            }
        }

        // ---------- EXEC ----------

        private async Task SaveExecDependentsAsync(DbConnection conn, DbTransaction tx,
                                                    long deviceId, ExecConfigDto exec,
                                                    CancellationToken ct)
        {
            // 1. ExecDevices (1-к-1)
            await ExecAsync(conn, tx,
                @"INSERT INTO ExecDevices (DeviceId, BaseChannel)
                  VALUES (@d, @bc);",
                ct,
                ("@d", deviceId),
                ("@bc", exec.BaseChannel ?? "")).ConfigureAwait(false);

            // 2. Потоки + link
            var streamIndexByPackedId = new Dictionary<int, int>();
            if(exec.Streams is { Count: > 0 })
            {
                int pos = 0;
                foreach(var st in exec.Streams)
                {
                    await ExecAsync(conn, tx,
                        @"INSERT INTO ExecStreams (StreamIndex, Protocol, Universe, Address)
                          VALUES (@i, @p, @u, @a);",
                        ct,
                        ("@i", st.Index),
                        ("@p", st.Protocol ?? "нет"),
                        ("@u", st.Universe),
                        ("@a", st.Address)).ConfigureAwait(false);

                    long streamId = await GetLastInsertIdAsync(conn, tx, ct).ConfigureAwait(false);

                    await ExecAsync(conn, tx,
                        @"INSERT INTO ExecDeviceStreams (DeviceId, StreamId, Position)
                          VALUES (@d, @s, @p);",
                        ct,
                        ("@d", deviceId),
                        ("@s", streamId),
                        ("@p", pos++)).ConfigureAwait(false);

                    streamIndexByPackedId[st.GetPackedStreamId()] = st.Index;
                }
            }

            // 3. Каналы + link Device↔Channel + link Channel↔Stream
            if(exec.Channels is { Count: > 0 })
            {
                int pos = 0;
                foreach(var ch in exec.Channels)
                {
                    await ExecAsync(conn, tx,
                        @"INSERT INTO ExecChannels
                            (Local, GlobalIndex, Name, Control, Wide,
                             Patch, Preheat, LimitValue, Curve, SwitchDelay)
                          VALUES
                            (@l, @g, @n, @c, @w, @p, @h, @lm, @cv, @sd);",
                        ct,
                        ("@l", ch.Local),
                        ("@g", ch.GlobalIndex),
                        ("@n", ch.Name ?? ""),
                        ("@c", ch.Control ?? "adlp"),
                        ("@w", ch.Wide ? 1 : 0),
                        ("@p", ch.Patch),
                        ("@h", ch.Preheat),
                        ("@lm", ch.Limit),
                        ("@cv", ch.Curve),
                        ("@sd", ch.SwitchDelay)).ConfigureAwait(false);

                    long channelId = await GetLastInsertIdAsync(conn, tx, ct).ConfigureAwait(false);

                    await ExecAsync(conn, tx,
                        @"INSERT INTO ExecDeviceChannels (DeviceId, ChannelId, Position)
                          VALUES (@d, @c, @p);",
                        ct,
                        ("@d", deviceId),
                        ("@c", channelId),
                        ("@p", pos++)).ConfigureAwait(false);

                    if(ch.StreamId != 0 &&
                        streamIndexByPackedId.TryGetValue(ch.StreamId, out int streamIndex))
                    {
                        await ExecAsync(conn, tx,
                            @"INSERT INTO ExecChannelStreams (DeviceId, ChannelId, StreamIndex)
                              VALUES (@d, @c, @si);",
                            ct,
                            ("@d", deviceId),
                            ("@c", channelId),
                            ("@si", streamIndex)).ConfigureAwait(false);
                    }
                }
            }
        }

        // =========================================================================
        //  ЗАГРУЗКА
        // =========================================================================

        /// <summary>
        /// Загружает устройство по идентификатору. Тип определяется полем
        /// <c>ConfigType</c> в таблице <c>Devices</c>.
        /// </summary>
        public async Task<DeviceConfigDtoBase?> LoadAsync(long deviceId,
                                                          CancellationToken ct = default)
        {
            await using var conn = await OpenConnectionAsync(ct).ConfigureAwait(false);

            string? configType = await LoadConfigTypeAsync(conn, deviceId, ct).ConfigureAwait(false);
            if(configType == null) return null;

            return configType switch
            {
                TypePu => await LoadPuAsync(conn, deviceId, ct).ConfigureAwait(false),
                TypeExec => await LoadExecAsync(conn, deviceId, ct).ConfigureAwait(false),
                _ => throw new NotSupportedException(
                        $"Неизвестный ConfigType: '{configType}'.")
            };
        }

        private static async Task<string?> LoadConfigTypeAsync(DbConnection conn, long id,
                                                                CancellationToken ct)
        {
            await using var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT ConfigType FROM Devices WHERE Id = @id;";
            AddParam(cmd, "@id", id);
            return await cmd.ExecuteScalarAsync(ct).ConfigureAwait(false) as string;
        }

        private async Task<PuConfigDto> LoadPuAsync(DbConnection conn, long deviceId,
                                                     CancellationToken ct)
        {
            var dto = new PuConfigDto { Id = deviceId };

            // Base + PuDevices
            await using(var cmd = conn.CreateCommand())
            {
                cmd.CommandText = @"
                    SELECT d.Name, d.Mac, d.Ip, d.Mask, d.Gateway, d.Block,
                       p.Priority, p.PictureSwitchTime, p.PageCount,
                       p.SwapX, p.SwapY, p.Brightness, p.BrightnessSleep, p.SleepTimeout
                      FROM Devices d
                      INNER JOIN PuDevices p ON p.DeviceId = d.Id
                     WHERE d.Id = @id;";
                AddParam(cmd, "@id", deviceId);

                await using var r = await cmd.ExecuteReaderAsync(ct).ConfigureAwait(false);
                if(await r.ReadAsync(ct).ConfigureAwait(false))
                {
                    dto.Name = GetStringSafe(r, 0);
                    dto.Mac = GetStringSafe(r, 1);
                    dto.Ip = GetStringSafe(r, 2);
                    dto.Mask = GetStringSafe(r, 3);
                    dto.Gateway = GetStringSafe(r, 4);
                    dto.Block = GetInt32Safe(r, 5);
                    dto.Priority = GetInt32Safe(r, 6);
                    dto.PictureSwitchTime = GetInt32Safe(r, 7);
                    dto.PageCount = GetInt32Safe(r, 8);
                    dto.SwapX = GetInt32Safe(r, 9);
                    dto.SwapY = GetInt32Safe(r, 10);
                    dto.Brightness = GetInt32Safe(r, 11);
                    dto.BrightnessSleep = GetInt32Safe(r, 12);
                    dto.SleepTimeout = GetInt32Safe(r, 13);
                }
            }

            // Каналы
            dto.Channels = new List<LiveControlChannelCfg>();
            await using(var cmd = conn.CreateCommand())
            {
                cmd.CommandText = @"
                    SELECT c.Local, c.Name, c.AdlpIndex, c.Type
                      FROM PuChannels c
                      INNER JOIN PuDeviceChannels dc ON dc.ChannelId = c.Id
                     WHERE dc.DeviceId = @id
                     ORDER BY dc.Position;";
                AddParam(cmd, "@id", deviceId);
                await using var r = await cmd.ExecuteReaderAsync(ct).ConfigureAwait(false);
                while(await r.ReadAsync(ct).ConfigureAwait(false))
                {
                    dto.Channels.Add(new LiveControlChannelCfg
                    {
                        Local = r.GetInt32(0),
                        Name = r.GetString(1),
                        Index = r.GetInt32(2),
                        Type = r.GetInt32(3)
                    });
                }
            }

            // Группы + их каналы (один запрос вместо N+1)
            dto.Groups = new List<LiveControlGroupCfg>();
            var groupIds = new List<long>();
            await using(var cmd = conn.CreateCommand())
            {
                cmd.CommandText = @"
                    SELECT g.Id, g.Local, g.AdlpIndex
                      FROM PuGroups g
                      INNER JOIN PuDeviceGroups dg ON dg.GroupId = g.Id
                     WHERE dg.DeviceId = @id
                     ORDER BY g.Local;";
                AddParam(cmd, "@id", deviceId);

                await using var r = await cmd.ExecuteReaderAsync(ct).ConfigureAwait(false);
                while(await r.ReadAsync(ct).ConfigureAwait(false))
                {
                    var grp = new LiveControlGroupCfg
                    {
                        Local = r.GetInt32(1),
                        Index = r.GetInt32(2)
                    };
                    groupIds.Add(r.GetInt64(0));
                    dto.Groups.Add(grp);
                }
            }

            if(groupIds.Count > 0)
            {
                // Собираем все слоты одним запросом
                var slotsByGroup = new Dictionary<long, List<int>>();
                await using(var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = @"
                        SELECT gc.GroupId, gc.ChannelAdlpIndex
                          FROM PuGroupChannels gc
                          INNER JOIN PuDeviceGroups dg ON dg.GroupId = gc.GroupId
                         WHERE dg.DeviceId = @id
                         ORDER BY gc.GroupId, gc.Slot;";
                    AddParam(cmd, "@id", deviceId);

                    await using var r = await cmd.ExecuteReaderAsync(ct).ConfigureAwait(false);
                    while(await r.ReadAsync(ct).ConfigureAwait(false))
                    {
                        long gid = r.GetInt64(0);
                        if(!slotsByGroup.TryGetValue(gid, out var list))
                            slotsByGroup[gid] = list = new List<int>();
                        list.Add(r.GetInt32(1));
                    }
                }

                for(int i = 0; i < dto.Groups.Count; i++)
                {
                    if(slotsByGroup.TryGetValue(groupIds[i], out var list))
                        dto.Groups[i].Channels.AddRange(list);
                }
            }

            // Картины
            dto.Pictures = new List<LiveControlPictureCfg>();
            await using(var cmd = conn.CreateCommand())
            {
                cmd.CommandText = @"
                    SELECT p.Local, p.Name
                      FROM PuPictures p
                      INNER JOIN PuDevicePictures dp ON dp.PictureId = p.Id
                     WHERE dp.DeviceId = @id
                     ORDER BY dp.Position;";
                AddParam(cmd, "@id", deviceId);
                await using var r = await cmd.ExecuteReaderAsync(ct).ConfigureAwait(false);
                while(await r.ReadAsync(ct).ConfigureAwait(false))
                {
                    dto.Pictures.Add(new LiveControlPictureCfg
                    {
                        Local = r.GetInt32(0),
                        Name = r.GetString(1)
                    });
                }
            }

            // Кнопки
            dto.Buttons = new List<LiveControlButtonCfg>();
            await using(var cmd = conn.CreateCommand())
            {
                cmd.CommandText = @"
                    SELECT b.Id, b.Local, b.Page, b.LocalChannel,
                           b.PosX, b.PosY, b.Icon, b.Color
                      FROM PuButtons b
                      INNER JOIN PuDeviceButtons db ON db.ButtonId = b.Id
                     WHERE db.DeviceId = @id
                     ORDER BY db.Position;";
                AddParam(cmd, "@id", deviceId);
                await using var r = await cmd.ExecuteReaderAsync(ct).ConfigureAwait(false);
                while(await r.ReadAsync(ct).ConfigureAwait(false))
                {
                    dto.Buttons.Add(new LiveControlButtonCfg
                    {
                        Local = r.GetInt32(1),
                        Page = r.GetInt32(2),
                        LocalChannel = r.GetInt32(3),
                        PosX = r.GetInt32(4),
                        PosY = r.GetInt32(5),
                        Icon = r.GetInt32(6),
                        Color = r.GetInt32(7)
                    });
                }
            }

            return dto;
        }

        private async Task<ExecConfigDto> LoadExecAsync(DbConnection conn, long deviceId,
                                                        CancellationToken ct)
        {
            var dto = new ExecConfigDto { Id = deviceId };

            // Base + ExecDevices
            await using(var cmd = conn.CreateCommand())
            {
                cmd.CommandText = @"
                    SELECT d.Name, d.Mac, d.Ip, d.Mask, d.Gateway, d.Block, e.BaseChannel
                      FROM Devices d
                      INNER JOIN ExecDevices e ON e.DeviceId = d.Id
                     WHERE d.Id = @id;";
                AddParam(cmd, "@id", deviceId);

                await using var r = await cmd.ExecuteReaderAsync(ct).ConfigureAwait(false);
                if(await r.ReadAsync(ct).ConfigureAwait(false))
                {
                    dto.Name = GetStringSafe(r, 0);
                    dto.Mac = GetStringSafe(r, 1);
                    dto.Ip = GetStringSafe(r, 2);
                    dto.Mask = GetStringSafe(r, 3);
                    dto.Gateway = GetStringSafe(r, 4);
                    dto.Block = GetInt32Safe(r, 5);
                    dto.BaseChannel = GetStringSafe(r, 6);
                }
            }

            // Потоки
            dto.Streams = new List<LiveStreamCfg>();
            await using(var cmd = conn.CreateCommand())
            {
                cmd.CommandText = @"
                    SELECT s.StreamIndex, s.Protocol, s.Universe, s.Address
                      FROM ExecStreams s
                      INNER JOIN ExecDeviceStreams ds ON ds.StreamId = s.Id
                     WHERE ds.DeviceId = @id
                     ORDER BY ds.Position;";
                AddParam(cmd, "@id", deviceId);
                await using var r = await cmd.ExecuteReaderAsync(ct).ConfigureAwait(false);
                while(await r.ReadAsync(ct).ConfigureAwait(false))
                {
                    dto.Streams.Add(new LiveStreamCfg
                    {
                        Index = r.GetInt32(0),
                        Protocol = r.GetString(1),
                        Universe = r.GetInt32(2),
                        Address = r.GetInt32(3)
                    });
                }
            }

            // Link канал↔поток
            var channelStreamIndex = new Dictionary<long, int>();
            await using(var cmd = conn.CreateCommand())
            {
                cmd.CommandText = @"
                    SELECT ChannelId, StreamIndex
                      FROM ExecChannelStreams
                     WHERE DeviceId = @id;";
                AddParam(cmd, "@id", deviceId);
                await using var r = await cmd.ExecuteReaderAsync(ct).ConfigureAwait(false);
                while(await r.ReadAsync(ct).ConfigureAwait(false))
                    channelStreamIndex[r.GetInt64(0)] = r.GetInt32(1);
            }

            var streamIdByIndex = new Dictionary<int, int>();
            foreach(var s in dto.Streams)
                streamIdByIndex[s.Index] = s.GetPackedStreamId();

            // Каналы
            dto.Channels = new List<LiveChannelCfg>();
            await using(var cmd = conn.CreateCommand())
            {
                cmd.CommandText = @"
                    SELECT c.Id, c.Local, c.GlobalIndex, c.Name, c.Control, c.Wide,
                           c.Patch, c.Preheat, c.LimitValue, c.Curve, c.SwitchDelay
                      FROM ExecChannels c
                      INNER JOIN ExecDeviceChannels dc ON dc.ChannelId = c.Id
                     WHERE dc.DeviceId = @id
                     ORDER BY dc.Position;";
                AddParam(cmd, "@id", deviceId);
                await using var r = await cmd.ExecuteReaderAsync(ct).ConfigureAwait(false);
                while(await r.ReadAsync(ct).ConfigureAwait(false))
                {
                    long chId = r.GetInt64(0);
                    var ch = new LiveChannelCfg
                    {
                        Local = r.GetInt32(1),
                        GlobalIndex = r.GetInt32(2),
                        Name = r.GetString(3),
                        Control = r.GetString(4),
                        Wide = r.GetInt32(5) != 0,
                        Patch = r.GetInt32(6),
                        Preheat = r.GetInt32(7),
                        Limit = r.GetInt32(8),
                        Curve = r.GetInt32(9),
                        SwitchDelay = r.GetInt32(10)
                    };

                    if(channelStreamIndex.TryGetValue(chId, out int streamIndex) &&
                        streamIdByIndex.TryGetValue(streamIndex, out int packedId))
                    {
                        ch.StreamId = packedId;
                    }
                    else
                    {
                        ch.StreamId = 0;
                    }

                    dto.Channels.Add(ch);
                }
            }

            return dto;
        }

        // =========================================================================
        //  СПИСОК И УДАЛЕНИЕ
        // =========================================================================

        /// <summary>
        /// Возвращает краткие сведения обо всех устройствах.
        /// </summary>
        public async Task<List<DeviceHeader>> GetAllHeadersAsync(CancellationToken ct = default)
        {
            await using var conn = await OpenConnectionAsync(ct).ConfigureAwait(false);
            await using var cmd = conn.CreateCommand();
            cmd.CommandText = @"
                SELECT Id, Name, ConfigType, Mac, Ip, Mask, Gateway, Block
                  FROM Devices
                 ORDER BY ConfigType, Mac;";

            var list = new List<DeviceHeader>();
            await using var r = await cmd.ExecuteReaderAsync(ct).ConfigureAwait(false);
            while(await r.ReadAsync(ct).ConfigureAwait(false))
            {
                list.Add(new DeviceHeader
                {
                    Id = r.GetInt64(0),
                    Name = GetStringSafe(r, 1),
                    ConfigType = GetStringSafe(r, 2),
                    Mac = GetStringSafe(r, 3),
                    Ip = GetStringSafe(r, 4),
                    Mask = GetStringSafe(r, 5),
                    Gateway = GetStringSafe(r, 6),
                    Block = GetInt32Safe(r, 7)
                });
            }
            return list;
        }

        /// <summary>
        /// Удаляет устройство вместе со всеми зависимыми записями.
        /// </summary>
        public async Task DeleteAsync(long deviceId, CancellationToken ct = default)
        {
            await using var conn = await OpenConnectionAsync(ct).ConfigureAwait(false);
            var tx = await conn.BeginTransactionAsync(ct).ConfigureAwait(false);
            try
            {
                await DeleteDependentsAsync(conn, tx, deviceId, ct).ConfigureAwait(false);

                await ExecAsync(conn, tx, "DELETE FROM Devices WHERE Id = @id;", ct,
                    ("@id", deviceId)).ConfigureAwait(false);

                await tx.CommitAsync(ct).ConfigureAwait(false);
            }
            catch
            {
                await tx.RollbackAsync(ct).ConfigureAwait(false);
                throw;
            }
        }

        /// <summary>
        /// Удаляет все таблицы схемы AdlpConfig. Используется для полного сброса БД.
        /// </summary>
        public async Task DropSchemaAsync(CancellationToken ct = default)
        {
            await using var conn = await OpenConnectionAsync(ct).ConfigureAwait(false);

            if(_dbType == DatabaseType.SQLite)
            {
                await using var pragma = conn.CreateCommand();
                pragma.CommandText = "PRAGMA foreign_keys = OFF;";
                await pragma.ExecuteNonQueryAsync(ct).ConfigureAwait(false);
            }
            else
            {
                await using var pragma = conn.CreateCommand();
                pragma.CommandText = "SET FOREIGN_KEY_CHECKS = 0;";
                await pragma.ExecuteNonQueryAsync(ct).ConfigureAwait(false);
            }

            var tables = new[]
            {
                // PU
                "PuButtonChannels",
                "PuDeviceButtons",
                "PuButtons",
                "PuGroupChannels",
                "PuDeviceGroups",
                "PuGroups",
                "PuDevicePictures",
                "PuPictures",
                "PuDeviceChannels",
                "PuChannels",
                "PuDevices",
                // EXEC
                "ExecChannelStreams",
                "ExecDeviceChannels",
                "ExecChannels",
                "ExecDeviceStreams",
                "ExecStreams",
                "ExecDevices",
                // Общая
                "Devices"
            };

            foreach(var table in tables)
            {
                await using var cmd = conn.CreateCommand();
                cmd.CommandText = _dbType == DatabaseType.SQLite
                    ? $"DROP TABLE IF EXISTS {table};"
                    : $"DROP TABLE IF EXISTS `{table}`;";
                await cmd.ExecuteNonQueryAsync(ct).ConfigureAwait(false);
            }

            if(_dbType == DatabaseType.SQLite)
            {
                await using var pragma = conn.CreateCommand();
                pragma.CommandText = "PRAGMA foreign_keys = ON;";
                await pragma.ExecuteNonQueryAsync(ct).ConfigureAwait(false);
            }
            else
            {
                await using var pragma = conn.CreateCommand();
                pragma.CommandText = "SET FOREIGN_KEY_CHECKS = 1;";
                await pragma.ExecuteNonQueryAsync(ct).ConfigureAwait(false);
            }
        }

        /// <summary>Строковый код типа для поля <c>Devices.ConfigType</c>.</summary>
        public static string ConfigTypeOf(DeviceConfigDtoBase dto) => dto switch
        {
            PuConfigDto => "PU",
            ExecConfigDto => "EXEC",
            _ => throw new NotSupportedException(
                    $"Тип {dto.GetType().Name} не поддержан.")
        };

        /// <summary>
        /// Ищет идентификатор устройства по MAC и типу конфигурации.
        /// Возвращает <c>null</c>, если такого устройства ещё нет.
        /// </summary>
        public async Task<long?> FindDeviceIdByMacAsync(string mac, string configType,
                                                         CancellationToken ct = default)
        {
            if(string.IsNullOrWhiteSpace(mac)) return null;

            await using var conn = await OpenConnectionAsync(ct).ConfigureAwait(false);
            await using var cmd = conn.CreateCommand();
            cmd.CommandText = @"
                SELECT Id FROM Devices
                 WHERE Mac = @m AND ConfigType = @t
                 LIMIT 1;";
            AddParam(cmd, "@m", mac);
            AddParam(cmd, "@t", configType);

            var result = await cmd.ExecuteScalarAsync(ct).ConfigureAwait(false);
            return result == null ? null : Convert.ToInt64(result);
        }

        // =========================================================================
        //  ВНУТРЕННИЕ МЕТОДЫ
        // =========================================================================

        /// <summary>
        /// Открывает соединение и (для SQLite) включает поддержку внешних ключей.
        /// </summary>
        private async Task<DbConnection> OpenConnectionAsync(CancellationToken ct)
        {
            DbConnection conn = _dbType switch
            {
                DatabaseType.SQLite => new SqliteConnection(_connectionString),
                DatabaseType.MySQL => new MySqlConnection(_connectionString),
                _ => throw new NotSupportedException()
            };

            await conn.OpenAsync(ct).ConfigureAwait(false);

            string userName = Environment.UserName;  // Windows-логин

            if(_dbType == DatabaseType.SQLite)
            {
                await using(var pragma = conn.CreateCommand())
                {
                    pragma.CommandText = "PRAGMA foreign_keys = ON;";
                    await pragma.ExecuteNonQueryAsync(ct).ConfigureAwait(false);
                }

                await using(var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = @"
                        CREATE TABLE IF NOT EXISTS _audit_context (key TEXT PRIMARY KEY, value TEXT);
                        INSERT OR REPLACE INTO _audit_context (key, value) VALUES ('user', @u);";
                    AddParam(cmd, "@u", userName);
                    await cmd.ExecuteNonQueryAsync(ct).ConfigureAwait(false);
                }
            }
            else 
            {
                await using var cmd = conn.CreateCommand();
                cmd.CommandText = "SET @app_user = @u;";
                AddParam(cmd, "@u", userName);
                await cmd.ExecuteNonQueryAsync(ct).ConfigureAwait(false);
            }

            return conn;
        }

        private async Task<long> GetLastInsertIdAsync(DbConnection conn, DbTransaction tx,
                                                       CancellationToken ct)
        {
            await using var cmd = conn.CreateCommand();
            cmd.Transaction = tx;
            cmd.CommandText = _dbType == DatabaseType.SQLite
                ? "SELECT last_insert_rowid();"
                : "SELECT LAST_INSERT_ID();";
            var result = await cmd.ExecuteScalarAsync(ct).ConfigureAwait(false);
            return Convert.ToInt64(result);
        }

        /// <summary>
        /// Асинхронно выполняет SQL с параметрами, переданными парами-кортежами:
        /// <c>("@name", value)</c>, <c>("@name2", value2)</c> и т. д.
        /// </summary>
        private static async Task<int> ExecAsync(DbConnection conn, DbTransaction tx,
                                                  string sql, CancellationToken ct,
                                                  params (string Name, object? Value)[] parameters)
        {
            await using var cmd = conn.CreateCommand();
            cmd.Transaction = tx;
            cmd.CommandText = sql;

            foreach(var (name, value) in parameters)
            {
                var p = cmd.CreateParameter();
                p.ParameterName = name;
                p.Value = value ?? DBNull.Value;
                cmd.Parameters.Add(p);
            }

            return await cmd.ExecuteNonQueryAsync(ct).ConfigureAwait(false);
        }

        /// <summary>Добавляет параметр к существующей команде (для read-запросов).</summary>
        private static void AddParam(DbCommand cmd, string name, object? value)
        {
            var p = cmd.CreateParameter();
            p.ParameterName = name;
            p.Value = value ?? DBNull.Value;
            cmd.Parameters.Add(p);
        }

        /// <summary>
        /// Удаляет все связанные записи устройства. Порядок важен: сначала дочерние
        /// link-таблицы, затем сущности, затем расширение (PuDevices / ExecDevices).
        /// </summary>
        private async Task DeleteDependentsAsync(DbConnection conn, DbTransaction tx,
                                                  long deviceId, CancellationToken ct)
        {
            // PU
            await ExecAsync(conn, tx, "DELETE FROM PuButtonChannels WHERE DeviceId = @d;", ct,
                ("@d", deviceId)).ConfigureAwait(false);
            await ExecAsync(conn, tx, "DELETE FROM PuDeviceButtons  WHERE DeviceId = @d;", ct,
                ("@d", deviceId)).ConfigureAwait(false);
            await ExecAsync(conn, tx,
                "DELETE FROM PuButtons WHERE Id NOT IN (SELECT ButtonId FROM PuDeviceButtons);",
                ct).ConfigureAwait(false);
            await ExecAsync(conn, tx,
                @"DELETE FROM PuGroupChannels
                   WHERE GroupId IN (SELECT GroupId FROM PuDeviceGroups WHERE DeviceId = @d);",
                ct, ("@d", deviceId)).ConfigureAwait(false);
            await ExecAsync(conn, tx, "DELETE FROM PuDeviceGroups  WHERE DeviceId = @d;", ct,
                ("@d", deviceId)).ConfigureAwait(false);
            await ExecAsync(conn, tx,
                "DELETE FROM PuGroups WHERE Id NOT IN (SELECT GroupId FROM PuDeviceGroups);",
                ct).ConfigureAwait(false);
            await ExecAsync(conn, tx, "DELETE FROM PuDevicePictures WHERE DeviceId = @d;", ct,
                ("@d", deviceId)).ConfigureAwait(false);
            await ExecAsync(conn, tx,
                "DELETE FROM PuPictures WHERE Id NOT IN (SELECT PictureId FROM PuDevicePictures);",
                ct).ConfigureAwait(false);
            await ExecAsync(conn, tx, "DELETE FROM PuDeviceChannels WHERE DeviceId = @d;", ct,
                ("@d", deviceId)).ConfigureAwait(false);
            await ExecAsync(conn, tx,
                "DELETE FROM PuChannels WHERE Id NOT IN (SELECT ChannelId FROM PuDeviceChannels);",
                ct).ConfigureAwait(false);
            await ExecAsync(conn, tx, "DELETE FROM PuDevices WHERE DeviceId = @d;", ct,
                ("@d", deviceId)).ConfigureAwait(false);

            // EXEC
            await ExecAsync(conn, tx, "DELETE FROM ExecChannelStreams WHERE DeviceId = @d;", ct,
                ("@d", deviceId)).ConfigureAwait(false);
            await ExecAsync(conn, tx, "DELETE FROM ExecDeviceChannels WHERE DeviceId = @d;", ct,
                ("@d", deviceId)).ConfigureAwait(false);
            await ExecAsync(conn, tx,
                "DELETE FROM ExecChannels WHERE Id NOT IN (SELECT ChannelId FROM ExecDeviceChannels);",
                ct).ConfigureAwait(false);
            await ExecAsync(conn, tx, "DELETE FROM ExecDeviceStreams WHERE DeviceId = @d;", ct,
                ("@d", deviceId)).ConfigureAwait(false);
            await ExecAsync(conn, tx,
                "DELETE FROM ExecStreams WHERE Id NOT IN (SELECT StreamId FROM ExecDeviceStreams);",
                ct).ConfigureAwait(false);
            await ExecAsync(conn, tx, "DELETE FROM ExecDevices WHERE DeviceId = @d;", ct,
                ("@d", deviceId)).ConfigureAwait(false);
        }


        private static string GetStringSafe(DbDataReader r, int ordinal)
            => r.IsDBNull(ordinal) ? string.Empty : r.GetString(ordinal);

        private static int GetInt32Safe(DbDataReader r, int ordinal)
            => r.IsDBNull(ordinal) ? 0 : r.GetInt32(ordinal);
        // =========================================================================
        //  DDL
        // =========================================================================

        private static readonly string[] SqliteDdl =
        {
        @"CREATE TABLE IF NOT EXISTS Devices (
            Id          INTEGER PRIMARY KEY AUTOINCREMENT,
            Name        TEXT NOT NULL DEFAULT '',
            ConfigType  TEXT NOT NULL,
            Mac         TEXT NOT NULL DEFAULT '',
            Ip          TEXT NOT NULL DEFAULT '',
            Mask        TEXT NOT NULL DEFAULT '',
            Gateway     TEXT NOT NULL DEFAULT '',
            Block       INTEGER NOT NULL DEFAULT 0,
            CreatedAt   TEXT NULL,
            CreatedBy   TEXT NULL,
            UpdatedAt   TEXT NULL,
            UpdatedBy   TEXT NULL
        );",

        @"CREATE TABLE IF NOT EXISTS PuDevices (
            DeviceId INTEGER PRIMARY KEY,
            Priority INTEGER NOT NULL DEFAULT 5,
            PictureSwitchTime INTEGER NOT NULL DEFAULT 0,
            PageCount INTEGER NOT NULL DEFAULT 1,
            SwapX INTEGER NOT NULL DEFAULT 0,
            SwapY INTEGER NOT NULL DEFAULT 0,
            Brightness INTEGER NOT NULL DEFAULT 100,
            BrightnessSleep INTEGER NOT NULL DEFAULT 100,
            SleepTimeout INTEGER NOT NULL DEFAULT 0,
            FOREIGN KEY (DeviceId) REFERENCES Devices(Id) ON DELETE CASCADE);",

        @"CREATE TABLE IF NOT EXISTS PuChannels (
            Id INTEGER PRIMARY KEY AUTOINCREMENT,
            Local INTEGER NOT NULL,
            Name TEXT NOT NULL DEFAULT '',
            AdlpIndex INTEGER NOT NULL DEFAULT 0,
            Type INTEGER NOT NULL DEFAULT 1);",

        @"CREATE TABLE IF NOT EXISTS PuDeviceChannels (
            DeviceId INTEGER NOT NULL,
            ChannelId INTEGER NOT NULL,
            Position INTEGER NOT NULL DEFAULT 0,
            PRIMARY KEY (DeviceId, ChannelId),
            FOREIGN KEY (DeviceId) REFERENCES Devices(Id) ON DELETE CASCADE,
            FOREIGN KEY (ChannelId) REFERENCES PuChannels(Id) ON DELETE CASCADE);",

        @"CREATE TABLE IF NOT EXISTS PuGroups (
            Id INTEGER PRIMARY KEY AUTOINCREMENT,
            Local INTEGER NOT NULL,
            AdlpIndex INTEGER NOT NULL DEFAULT 0);",

        @"CREATE TABLE IF NOT EXISTS PuDeviceGroups (
            DeviceId INTEGER NOT NULL,
            GroupId INTEGER NOT NULL,
            Position INTEGER NOT NULL DEFAULT 0,
            PRIMARY KEY (DeviceId, GroupId),
            FOREIGN KEY (DeviceId) REFERENCES Devices(Id) ON DELETE CASCADE,
            FOREIGN KEY (GroupId) REFERENCES PuGroups(Id) ON DELETE CASCADE);",

        @"CREATE TABLE IF NOT EXISTS PuGroupChannels (
            GroupId INTEGER NOT NULL,
            ChannelAdlpIndex INTEGER NOT NULL,
            Slot INTEGER NOT NULL,
            PRIMARY KEY (GroupId, Slot),
            FOREIGN KEY (GroupId) REFERENCES PuGroups(Id) ON DELETE CASCADE);",

        @"CREATE TABLE IF NOT EXISTS PuPictures (
            Id INTEGER PRIMARY KEY AUTOINCREMENT,
            Local INTEGER NOT NULL,
            Name TEXT NOT NULL DEFAULT '');",

        @"CREATE TABLE IF NOT EXISTS PuDevicePictures (
            DeviceId INTEGER NOT NULL,
            PictureId INTEGER NOT NULL,
            Position INTEGER NOT NULL DEFAULT 0,
            PRIMARY KEY (DeviceId, PictureId),
            FOREIGN KEY (DeviceId) REFERENCES Devices(Id) ON DELETE CASCADE,
            FOREIGN KEY (PictureId) REFERENCES PuPictures(Id) ON DELETE CASCADE);",

        @"CREATE TABLE IF NOT EXISTS PuButtons (
            Id INTEGER PRIMARY KEY AUTOINCREMENT,
            Local INTEGER NOT NULL,
            Page INTEGER NOT NULL DEFAULT 1,
            LocalChannel INTEGER NOT NULL DEFAULT 0,
            PosX INTEGER NOT NULL DEFAULT 0,
            PosY INTEGER NOT NULL DEFAULT 0,
            Icon INTEGER NOT NULL DEFAULT 0,
            Color INTEGER NOT NULL DEFAULT 0);",

        @"CREATE TABLE IF NOT EXISTS PuDeviceButtons (
            DeviceId INTEGER NOT NULL,
            ButtonId INTEGER NOT NULL,
            Position INTEGER NOT NULL DEFAULT 0,
            PRIMARY KEY (DeviceId, ButtonId),
            FOREIGN KEY (DeviceId) REFERENCES Devices(Id) ON DELETE CASCADE,
            FOREIGN KEY (ButtonId) REFERENCES PuButtons(Id) ON DELETE CASCADE);",

        @"CREATE TABLE IF NOT EXISTS PuButtonChannels (
            DeviceId INTEGER NOT NULL,
            ButtonId INTEGER NOT NULL,
            ChannelLocal INTEGER NOT NULL,
            PRIMARY KEY (DeviceId, ButtonId),
            FOREIGN KEY (DeviceId, ButtonId)
                REFERENCES PuDeviceButtons(DeviceId, ButtonId) ON DELETE CASCADE);",

        @"CREATE TABLE IF NOT EXISTS ExecDevices (
            DeviceId INTEGER PRIMARY KEY,
            BaseChannel TEXT NOT NULL DEFAULT '',
            FOREIGN KEY (DeviceId) REFERENCES Devices(Id) ON DELETE CASCADE);",

        @"CREATE TABLE IF NOT EXISTS ExecStreams (
            Id INTEGER PRIMARY KEY AUTOINCREMENT,
            StreamIndex INTEGER NOT NULL,
            Protocol TEXT NOT NULL DEFAULT 'нет',
            Universe INTEGER NOT NULL DEFAULT 0,
            Address INTEGER NOT NULL DEFAULT 1);",

        @"CREATE TABLE IF NOT EXISTS ExecDeviceStreams (
            DeviceId INTEGER NOT NULL,
            StreamId INTEGER NOT NULL,
            Position INTEGER NOT NULL DEFAULT 0,
            PRIMARY KEY (DeviceId, StreamId),
            FOREIGN KEY (DeviceId) REFERENCES Devices(Id) ON DELETE CASCADE,
            FOREIGN KEY (StreamId) REFERENCES ExecStreams(Id) ON DELETE CASCADE);",

        @"CREATE TABLE IF NOT EXISTS ExecChannels (
            Id INTEGER PRIMARY KEY AUTOINCREMENT,
            Local INTEGER NOT NULL,
            GlobalIndex INTEGER NOT NULL DEFAULT 0,
            Name TEXT NOT NULL DEFAULT '',
            Control TEXT NOT NULL DEFAULT 'adlp',
            Wide INTEGER NOT NULL DEFAULT 0,
            Patch INTEGER NOT NULL DEFAULT 0,
            Preheat INTEGER NOT NULL DEFAULT 0,
            LimitValue INTEGER NOT NULL DEFAULT 100,
            Curve INTEGER NOT NULL DEFAULT 0,
            SwitchDelay INTEGER NOT NULL DEFAULT 0);",

        @"CREATE TABLE IF NOT EXISTS ExecDeviceChannels (
            DeviceId INTEGER NOT NULL,
            ChannelId INTEGER NOT NULL,
            Position INTEGER NOT NULL DEFAULT 0,
            PRIMARY KEY (DeviceId, ChannelId),
            FOREIGN KEY (DeviceId) REFERENCES Devices(Id) ON DELETE CASCADE,
            FOREIGN KEY (ChannelId) REFERENCES ExecChannels(Id) ON DELETE CASCADE);",

        @"CREATE TABLE IF NOT EXISTS _audit_context (
            key   TEXT PRIMARY KEY,
            value TEXT
        );",

        @"CREATE TABLE IF NOT EXISTS ExecChannelStreams (
            DeviceId INTEGER NOT NULL,
            ChannelId INTEGER NOT NULL,
            StreamIndex INTEGER NOT NULL,
            PRIMARY KEY (DeviceId, ChannelId),
            FOREIGN KEY (DeviceId, ChannelId)
                REFERENCES ExecDeviceChannels(DeviceId, ChannelId) ON DELETE CASCADE);"
    };

        private static readonly string[] MySqlDdl =
        {
        @"CREATE TABLE IF NOT EXISTS Devices (
            Id          INT AUTO_INCREMENT PRIMARY KEY,
            Name        VARCHAR(255) NOT NULL DEFAULT '',
            ConfigType  VARCHAR(10) NOT NULL,
            Mac         VARCHAR(32) NOT NULL DEFAULT '',
            Ip          VARCHAR(64) NOT NULL DEFAULT '',
            Mask        VARCHAR(64) NOT NULL DEFAULT '',
            Gateway     VARCHAR(64) NOT NULL DEFAULT '',
            Block       INT NOT NULL DEFAULT 0,
            CreatedAt   DATETIME     NULL,
            CreatedBy   VARCHAR(100) NULL,
            UpdatedAt   DATETIME     NULL,
            UpdatedBy   VARCHAR(100) NULL
        ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;",

        @"CREATE TABLE IF NOT EXISTS PuDevices (
            DeviceId INT PRIMARY KEY,
            Priority INT NOT NULL DEFAULT 5,
            PictureSwitchTime INT NOT NULL DEFAULT 0,
            PageCount INT NOT NULL DEFAULT 1,
            SwapX INT NOT NULL DEFAULT 0,
            SwapY INT NOT NULL DEFAULT 0,
            Brightness INT NOT NULL DEFAULT 100,
            BrightnessSleep INT NOT NULL DEFAULT 100,
            SleepTimeout INT NOT NULL DEFAULT 0,
            CONSTRAINT fk_pu_device FOREIGN KEY (DeviceId)
                REFERENCES Devices(Id) ON DELETE CASCADE
        ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;",

        @"CREATE TABLE IF NOT EXISTS PuChannels (
            Id INT AUTO_INCREMENT PRIMARY KEY,
            Local INT NOT NULL,
            Name VARCHAR(255) NOT NULL DEFAULT '',
            AdlpIndex INT NOT NULL DEFAULT 0,
            Type INT NOT NULL DEFAULT 1
        ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;",

        @"CREATE TABLE IF NOT EXISTS PuDeviceChannels (
            DeviceId INT NOT NULL,
            ChannelId INT NOT NULL,
            Position INT NOT NULL DEFAULT 0,
            PRIMARY KEY (DeviceId, ChannelId),
            CONSTRAINT fk_pdc_device FOREIGN KEY (DeviceId)
                REFERENCES Devices(Id) ON DELETE CASCADE,
            CONSTRAINT fk_pdc_channel FOREIGN KEY (ChannelId)
                REFERENCES PuChannels(Id) ON DELETE CASCADE
        ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;",

        @"CREATE TABLE IF NOT EXISTS PuGroups (
            Id INT AUTO_INCREMENT PRIMARY KEY,
            Local INT NOT NULL,
            AdlpIndex INT NOT NULL DEFAULT 0
        ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;",

        @"CREATE TABLE IF NOT EXISTS PuDeviceGroups (
            DeviceId INT NOT NULL,
            GroupId INT NOT NULL,
            Position INT NOT NULL DEFAULT 0,
            PRIMARY KEY (DeviceId, GroupId),
            CONSTRAINT fk_pdg_device FOREIGN KEY (DeviceId)
                REFERENCES Devices(Id) ON DELETE CASCADE,
            CONSTRAINT fk_pdg_group FOREIGN KEY (GroupId)
                REFERENCES PuGroups(Id) ON DELETE CASCADE
        ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;",

        @"CREATE TABLE IF NOT EXISTS PuGroupChannels (
            GroupId INT NOT NULL,
            ChannelAdlpIndex INT NOT NULL,
            Slot INT NOT NULL,
            PRIMARY KEY (GroupId, Slot),
            CONSTRAINT fk_pgc_group FOREIGN KEY (GroupId)
                REFERENCES PuGroups(Id) ON DELETE CASCADE
        ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;",

        @"CREATE TABLE IF NOT EXISTS PuPictures (
            Id INT AUTO_INCREMENT PRIMARY KEY,
            Local INT NOT NULL,
            Name VARCHAR(255) NOT NULL DEFAULT ''
        ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;",

        @"CREATE TABLE IF NOT EXISTS PuDevicePictures (
            DeviceId INT NOT NULL,
            PictureId INT NOT NULL,
            Position INT NOT NULL DEFAULT 0,
            PRIMARY KEY (DeviceId, PictureId),
            CONSTRAINT fk_pdp_device FOREIGN KEY (DeviceId)
                REFERENCES Devices(Id) ON DELETE CASCADE,
            CONSTRAINT fk_pdp_picture FOREIGN KEY (PictureId)
                REFERENCES PuPictures(Id) ON DELETE CASCADE
        ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;",

        @"CREATE TABLE IF NOT EXISTS PuButtons (
            Id INT AUTO_INCREMENT PRIMARY KEY,
            Local INT NOT NULL,
            Page INT NOT NULL DEFAULT 1,
            LocalChannel INT NOT NULL DEFAULT 0,
            PosX INT NOT NULL DEFAULT 0,
            PosY INT NOT NULL DEFAULT 0,
            Icon INT NOT NULL DEFAULT 0,
            Color INT NOT NULL DEFAULT 0
        ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;",

        @"CREATE TABLE IF NOT EXISTS PuDeviceButtons (
            DeviceId INT NOT NULL,
            ButtonId INT NOT NULL,
            Position INT NOT NULL DEFAULT 0,
            PRIMARY KEY (DeviceId, ButtonId),
            CONSTRAINT fk_pdb_device FOREIGN KEY (DeviceId)
                REFERENCES Devices(Id) ON DELETE CASCADE,
            CONSTRAINT fk_pdb_button FOREIGN KEY (ButtonId)
                REFERENCES PuButtons(Id) ON DELETE CASCADE
        ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;",

        @"CREATE TABLE IF NOT EXISTS PuButtonChannels (
            DeviceId INT NOT NULL,
            ButtonId INT NOT NULL,
            ChannelLocal INT NOT NULL,
            PRIMARY KEY (DeviceId, ButtonId),
            CONSTRAINT fk_pbc_button FOREIGN KEY (DeviceId, ButtonId)
                REFERENCES PuDeviceButtons(DeviceId, ButtonId) ON DELETE CASCADE
        ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;",

        @"CREATE TABLE IF NOT EXISTS ExecDevices (
            DeviceId INT PRIMARY KEY,
            BaseChannel VARCHAR(255) NOT NULL DEFAULT '',
            CONSTRAINT fk_exec_device FOREIGN KEY (DeviceId)
                REFERENCES Devices(Id) ON DELETE CASCADE
        ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;",

        @"CREATE TABLE IF NOT EXISTS ExecStreams (
            Id INT AUTO_INCREMENT PRIMARY KEY,
            StreamIndex INT NOT NULL,
            Protocol VARCHAR(50) NOT NULL DEFAULT 'нет',
            Universe INT NOT NULL DEFAULT 0,
            Address INT NOT NULL DEFAULT 1
        ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;",

        @"CREATE TABLE IF NOT EXISTS ExecDeviceStreams (
            DeviceId INT NOT NULL,
            StreamId INT NOT NULL,
            Position INT NOT NULL DEFAULT 0,
            PRIMARY KEY (DeviceId, StreamId),
            CONSTRAINT fk_eds_device FOREIGN KEY (DeviceId)
                REFERENCES Devices(Id) ON DELETE CASCADE,
            CONSTRAINT fk_eds_stream FOREIGN KEY (StreamId)
                REFERENCES ExecStreams(Id) ON DELETE CASCADE
        ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;",

        @"CREATE TABLE IF NOT EXISTS ExecChannels (
            Id INT AUTO_INCREMENT PRIMARY KEY,
            Local INT NOT NULL,
            GlobalIndex INT NOT NULL DEFAULT 0,
            Name VARCHAR(255) NOT NULL DEFAULT '',
            Control VARCHAR(20) NOT NULL DEFAULT 'adlp',
            Wide TINYINT(1) NOT NULL DEFAULT 0,
            Patch INT NOT NULL DEFAULT 0,
            Preheat INT NOT NULL DEFAULT 0,
            LimitValue INT NOT NULL DEFAULT 100,
            Curve INT NOT NULL DEFAULT 0,
            SwitchDelay INT NOT NULL DEFAULT 0
        ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;",

        @"CREATE TABLE IF NOT EXISTS ExecDeviceChannels (
            DeviceId INT NOT NULL,
            ChannelId INT NOT NULL,
            Position INT NOT NULL DEFAULT 0,
            PRIMARY KEY (DeviceId, ChannelId),
            CONSTRAINT fk_edc_device FOREIGN KEY (DeviceId)
                REFERENCES Devices(Id) ON DELETE CASCADE,
            CONSTRAINT fk_edc_channel FOREIGN KEY (ChannelId)
                REFERENCES ExecChannels(Id) ON DELETE CASCADE
        ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;",

        @"CREATE TABLE IF NOT EXISTS ExecChannelStreams (
            DeviceId INT NOT NULL,
            ChannelId INT NOT NULL,
            StreamIndex INT NOT NULL,
            PRIMARY KEY (DeviceId, ChannelId),
            CONSTRAINT fk_ecs_dc FOREIGN KEY (DeviceId, ChannelId)
                REFERENCES ExecDeviceChannels(DeviceId, ChannelId) ON DELETE CASCADE
        ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;"
    };
    }
}