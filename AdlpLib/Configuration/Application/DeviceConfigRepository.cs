using AdlpLib.LiveConfig;
using AdlpLib.Models.Devices;
using AdlpLib.Models.Devices.Exec;
using AdlpLib.Models.Devices.NetGate;
using AdlpLib.Models.Devices.PU;
using Microsoft.Data.Sqlite;
using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
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
        public string DeviceType { get; set; } = "";
        public string Mac { get; set; } = "";
        public string Ip { get; set; } = "";
        public string Mask { get; set; } = "";
        public string Gateway { get; set; } = "";
        public int Block { get; set; }

        public override string ToString() => $"[{ConfigType}] {Name} [{Mac}]";
    }

    /// <summary>
    /// Асинхронный репозиторий сохранения/загрузки конфигураций устройств (PU, EXEC, NETGATE).
    /// Работает и с SQLite, и с MySQL — в зависимости от <see cref="DatabaseType"/>.
    /// </summary>
    public class DeviceConfigRepository
    {
        private const string TypePu6 = "PU6";
        private const string TypePuBW = "PUBW";
        private const string TypePu = "PU";   // legacy = PuColorConfigDto
        private const string TypeExec = "EXEC";
        private const string TypeNetGate = "NETGATE";

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

        private async Task EnsureAuditColumnsAsync(DbConnection conn, CancellationToken ct)
        {
            var needed = new (string Name, string SqliteType, string MySqlType, bool NotNull)[]
            {
                ("Name",       "TEXT",       "VARCHAR(255)",  true),
                ("DeviceType", "TEXT",       "VARCHAR(100)",  true),
                ("SID",        "INTEGER",    "INT",           false),
                ("CreatedAt",  "TEXT",       "DATETIME",      false),
                ("CreatedBy",  "TEXT",       "VARCHAR(100)",  false),
                ("UpdatedAt",  "TEXT",       "DATETIME",      false),
                ("UpdatedBy",  "TEXT",       "VARCHAR(100)",  false)
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
        }

        private async Task EnsureAuditTriggersAsync(DbConnection conn, CancellationToken ct)
        {
            if(_dbType == DatabaseType.MySQL)
            {
                await ExecNoTxAsync(conn, "DROP TRIGGER IF EXISTS trg_devices_bi;", ct).ConfigureAwait(false);
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

                await ExecNoTxAsync(conn, "DROP TRIGGER IF EXISTS trg_devices_bu;", ct).ConfigureAwait(false);
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
            else
            {
                await ExecNoTxAsync(conn, "DROP TRIGGER IF EXISTS trg_devices_ai;", ct).ConfigureAwait(false);
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

                await ExecNoTxAsync(conn, "DROP TRIGGER IF EXISTS trg_devices_au;", ct).ConfigureAwait(false);
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

        private static async Task ExecNoTxAsync(DbConnection conn, string sql, CancellationToken ct)
        {
            await using var cmd = conn.CreateCommand();
            cmd.CommandText = sql;
            await cmd.ExecuteNonQueryAsync(ct).ConfigureAwait(false);
        }

        private async Task ValidateSchemaAsync(DbConnection conn, CancellationToken ct)
        {
            var required = new Dictionary<string, string[]>
            {
                ["Devices"] = new[] { "Id", "Name", "ConfigType", "DeviceType", "Mac", "Ip", "Mask", "Gateway", "Block" },
                ["PuDevices"] = new[] { "DeviceId", "Priority", "Brightness", "SleepTimeout" },
                ["ExecDevices"] = new[] { "DeviceId", "BaseChannel" },
                ["NetGateDevices"] = new[] { "DeviceId", "ConveyorCount" },
                ["NetGateConveyors"] = new[] { "Id", "DeviceId", "Local" },
                ["NetGateChannels"] = new[] { "Id", "DeviceId", "Local", "Name", "AdlpIndex" },
                ["NetGatePictures"] = new[] { "Id", "DeviceId", "Local", "Picture" }
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

        private async Task<List<string>> GetTableColumnsAsync(DbConnection conn, string table, CancellationToken ct)
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

        public async Task<long> SaveAsync(DeviceConfigDtoBase dto, CancellationToken ct = default)
        {
            await using var conn = await OpenConnectionAsync(ct).ConfigureAwait(false);
            var tx = await conn.BeginTransactionAsync(ct).ConfigureAwait(false);

            try
            {
                string configType = dto switch
                {
                    Pu6ConfigDto => TypePu6,
                    PuBWConfigDto => TypePuBW,
                    PuColorConfigDto => TypePu,
                    ExecConfigDto => TypeExec,
                    NetGateConfigDto => TypeNetGate,
                    _ => throw new NotSupportedException($"Тип {dto.GetType().Name} не поддержан.")
                };

                long deviceId = await UpsertDeviceAsync(conn, tx, dto, configType, ct);
                await DeleteDependentsAsync(conn, tx, deviceId, ct);

                switch(dto)
                {
                    case PuConfigDtoBase pu:               
                        await SavePuDependentsAsync(conn, tx, deviceId, pu, ct);
                        break;
                    case ExecConfigDto exec:
                        await SaveExecDependentsAsync(conn, tx, deviceId, exec, ct);
                        break;
                    case NetGateConfigDto ng:
                        await SaveNetGateDependentsAsync(conn, tx, deviceId, ng, ct);
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
                    @"INSERT INTO Devices (Name, ConfigType, DeviceType, SID, Mac, Ip, Mask, Gateway, Block)
                        VALUES (@n, @t, @dt, @sid, @m, @ip, @mask, @gw, @b);",
                    ct,
                    ("@n", dto.Name ?? ""),
                    ("@t", configType),
                    ("@dt", dto.DeviceType ?? ""),
                    ("@sid", dto.SID),
                    ("@m", dto.Mac ?? ""),
                    ("@ip", dto.Ip ?? ""),
                    ("@mask", dto.Mask ?? ""),
                    ("@gw", dto.Gateway ?? ""),
                    ("@b", dto.Block)).ConfigureAwait(false);

                return await GetLastInsertIdAsync(conn, tx, ct).ConfigureAwait(false);
            }

            await ExecAsync(conn, tx,
                @"UPDATE Devices
                     SET Name = @n, ConfigType = @t, DeviceType = @dt, SID = @sid,
                         Mac = @m, Ip = @ip, Mask = @mask, Gateway = @gw, Block = @b
                   WHERE Id = @id;",
                ct,
                ("@n", dto.Name ?? ""),
                ("@t", configType),
                ("@dt", dto.DeviceType ?? ""),
                ("@sid", dto.SID),
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
                                                  long deviceId, PuConfigDtoBase pu,
                                                  CancellationToken ct)
        {
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
            await ExecAsync(conn, tx,
                @"INSERT INTO ExecDevices (DeviceId, BaseChannel)
                  VALUES (@d, @bc);",
                ct,
                ("@d", deviceId),
                ("@bc", exec.BaseChannel ?? "")).ConfigureAwait(false);

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

        // ---------- NETGATE ----------

        private async Task SaveNetGateDependentsAsync(DbConnection conn, DbTransaction tx,
                                                       long deviceId, NetGateConfigDto ng,
                                                       CancellationToken ct)
        {
            await ExecAsync(conn, tx,
                @"INSERT INTO NetGateDevices (DeviceId, ConveyorCount)
                  VALUES (@d, @cc);",
                ct,
                ("@d", deviceId),
                ("@cc", ng.ConveyorCount)).ConfigureAwait(false);

            if(ng.Conveyors is { Count: > 0 })
            {
                foreach(var cv in ng.Conveyors)
                {
                    await ExecAsync(conn, tx,
                        @"INSERT INTO NetGateConveyors
                            (DeviceId, Local,
                             SourceAProtocol, SourceAUniverse,
                             SourceBProtocol, SourceBUniverse,
                             SourceCProtocol, SourceCUniverse,
                             DestinationProtocol, DestinationUniverse,
                             SourceSelect, SumType, Frequency,
                             Recordable, Timeouted, RdmEnabled,
                             SumBFrom, SumBTo, SumCFrom, SumCTo)
                          VALUES
                            (@d, @l,
                             @sap, @sau,
                             @sbp, @sbu,
                             @scp, @scu,
                             @dp,  @du,
                             @ss, @st, @fq,
                             @rec, @to, @rdm,
                             @sbf, @sbt, @scf, @sct);",
                        ct,
                        ("@d", deviceId),
                        ("@l", cv.Local),
                        ("@sap", cv.SourceA?.Protocol ?? "нет"),
                        ("@sau", cv.SourceA?.Universe ?? 0),
                        ("@sbp", cv.SourceB?.Protocol ?? "нет"),
                        ("@sbu", cv.SourceB?.Universe ?? 0),
                        ("@scp", cv.SourceC?.Protocol ?? "нет"),
                        ("@scu", cv.SourceC?.Universe ?? 0),
                        ("@dp", cv.Destination?.Protocol ?? "нет"),
                        ("@du", cv.Destination?.Universe ?? 0),
                        ("@ss", cv.SourceSelect),
                        ("@st", cv.SumType),
                        ("@fq", cv.Frequency),
                        ("@rec", cv.Recordable ? 1 : 0),
                        ("@to", cv.Timeouted ? 1 : 0),
                        ("@rdm", cv.RdmEnabled ? 1 : 0),
                        ("@sbf", cv.SumBFrom),
                        ("@sbt", cv.SumBTo),
                        ("@scf", cv.SumCFrom),
                        ("@sct", cv.SumCTo)).ConfigureAwait(false);
                }
            }

            if(ng.Channels is { Count: > 0 })
            {
                foreach(var ch in ng.Channels)
                {
                    await ExecAsync(conn, tx,
                        @"INSERT INTO NetGateChannels (DeviceId, Local, Name, AdlpIndex)
                          VALUES (@d, @l, @n, @i);",
                        ct,
                        ("@d", deviceId),
                        ("@l", ch.Local),
                        ("@n", ch.Name ?? ""),
                        ("@i", ch.Index)).ConfigureAwait(false);
                }
            }

            if(ng.Pictures is { Count: > 0 })
            {
                foreach(var pic in ng.Pictures)
                {
                    await ExecAsync(conn, tx,
                        @"INSERT INTO NetGatePictures
                            (DeviceId, Local, Picture, FadeIn, FadeOut,
                             Mux1, Mux2, Mux3, Mux4)
                          VALUES
                            (@d, @l, @p, @fi, @fo, @m1, @m2, @m3, @m4);",
                        ct,
                        ("@d", deviceId),
                        ("@l", pic.Local),
                        ("@p", pic.Picture),
                        ("@fi", pic.FadeIn),
                        ("@fo", pic.FadeOut),
                        ("@m1", pic.Mux1),
                        ("@m2", pic.Mux2),
                        ("@m3", pic.Mux3),
                        ("@m4", pic.Mux4)).ConfigureAwait(false);
                }
            }
        }

        // =========================================================================
        //  ЗАГРУЗКА
        // =========================================================================

        public async Task<DeviceConfigDtoBase?> LoadAsync(long deviceId, CancellationToken ct = default)
        {
            await using var conn = await OpenConnectionAsync(ct).ConfigureAwait(false);

            string? configType = await LoadConfigTypeAsync(conn, deviceId, ct).ConfigureAwait(false);
            if(configType == null) return null;

            return configType switch
            {
                TypePu6 => await LoadPuAsync<Pu6ConfigDto>(conn, deviceId, ct),
                TypePuBW => await LoadPuAsync<PuBWConfigDto>(conn, deviceId, ct),
                TypePu => await LoadPuAsync<PuColorConfigDto>(conn, deviceId, ct),
                TypeExec => await LoadExecAsync(conn, deviceId, ct),
                TypeNetGate => await LoadNetGateAsync(conn, deviceId, ct),
                _ => throw new NotSupportedException($"Неизвестный ConfigType: '{configType}'.")
            };
        }

        private static async Task<string?> LoadConfigTypeAsync(DbConnection conn, long id, CancellationToken ct)
        {
            await using var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT ConfigType FROM Devices WHERE Id = @id;";
            AddParam(cmd, "@id", id);
            return await cmd.ExecuteScalarAsync(ct).ConfigureAwait(false) as string;
        }

        private async Task<TPu> LoadPuAsync<TPu>(DbConnection conn, long deviceId, CancellationToken ct)
            where TPu : PuConfigDtoBase, new()
        {
            var dto = new TPu { Id = deviceId };

            await using(var cmd = conn.CreateCommand())
            {
                cmd.CommandText = @"
                    SELECT d.Name, d.SID, d.Mac, d.Ip, d.Mask, d.Gateway, d.Block,
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
                    dto.SID = GetInt32Safe(r, 1);
                    dto.Mac = GetStringSafe(r, 2);
                    dto.Ip = GetStringSafe(r, 3);
                    dto.Mask = GetStringSafe(r, 4);
                    dto.Gateway = GetStringSafe(r, 5);
                    dto.Block = GetInt32Safe(r, 6);
                    dto.Priority = GetInt32Safe(r, 7);
                    dto.PictureSwitchTime = GetInt32Safe(r, 8);
                    dto.PageCount = GetInt32Safe(r, 9);
                    dto.SwapX = GetInt32Safe(r, 10);
                    dto.SwapY = GetInt32Safe(r, 11);
                    dto.Brightness = GetInt32Safe(r, 12);
                    dto.BrightnessSleep = GetInt32Safe(r, 13);
                    dto.SleepTimeout = GetInt32Safe(r, 14);
                }
            }

            dto.Channels ??= new List<LiveControlChannelCfg>();
            dto.Groups ??= new List<LiveControlGroupCfg>();
            dto.Pictures ??= new List<LiveControlPictureCfg>();
            dto.Buttons ??= new List<LiveControlButtonCfg>();
            return dto;
        }

        private async Task<ExecConfigDto> LoadExecAsync(DbConnection conn, long deviceId, CancellationToken ct)
        {
            var dto = new ExecConfigDto { Id = deviceId };

            await using(var cmd = conn.CreateCommand())
            {
                cmd.CommandText = @"
                    SELECT d.Name, d.SID, d.Mac, d.Ip, d.Mask, d.Gateway, d.Block, e.BaseChannel
                      FROM Devices d
                      INNER JOIN ExecDevices e ON e.DeviceId = d.Id
                     WHERE d.Id = @id;";
                AddParam(cmd, "@id", deviceId);

                await using var r = await cmd.ExecuteReaderAsync(ct).ConfigureAwait(false);
                if(await r.ReadAsync(ct).ConfigureAwait(false))
                {
                    dto.Name = GetStringSafe(r, 0);
                    dto.SID = GetInt32Safe(r, 1);
                    dto.Mac = GetStringSafe(r, 2);
                    dto.Ip = GetStringSafe(r, 3);
                    dto.Mask = GetStringSafe(r, 4);
                    dto.Gateway = GetStringSafe(r, 5);
                    dto.Block = GetInt32Safe(r, 6);
                    dto.BaseChannel = GetStringSafe(r, 7);
                }
            }

            dto.Streams ??= new List<LiveStreamCfg>();
            dto.Channels ??= new List<LiveChannelCfg>();
            return dto;
        }

        private async Task<NetGateConfigDto> LoadNetGateAsync(DbConnection conn, long deviceId, CancellationToken ct)
        {
            var dto = new NetGateConfigDto { Id = deviceId };

            // Base + NetGateDevices
            await using(var cmd = conn.CreateCommand())
            {
                cmd.CommandText = @"
                    SELECT d.Name, d.SID, d.Mac, d.Ip, d.Mask, d.Gateway, d.Block,
                           ng.ConveyorCount
                      FROM Devices d
                      INNER JOIN NetGateDevices ng ON ng.DeviceId = d.Id
                     WHERE d.Id = @id;";
                AddParam(cmd, "@id", deviceId);

                await using var r = await cmd.ExecuteReaderAsync(ct).ConfigureAwait(false);
                if(await r.ReadAsync(ct).ConfigureAwait(false))
                {
                    dto.Name = GetStringSafe(r, 0);
                    dto.SID = GetInt32Safe(r, 1);
                    dto.Mac = GetStringSafe(r, 2);
                    dto.Ip = GetStringSafe(r, 3);
                    dto.Mask = GetStringSafe(r, 4);
                    dto.Gateway = GetStringSafe(r, 5);
                    dto.Block = GetInt32Safe(r, 6);
                    dto.ConveyorCount = GetInt32Safe(r, 7);
                }
            }

            // Conveyors
            dto.Conveyors = new List<NetGateConveyorCfg>();
            await using(var cmd = conn.CreateCommand())
            {
                cmd.CommandText = @"
                    SELECT Local,
                           SourceAProtocol, SourceAUniverse,
                           SourceBProtocol, SourceBUniverse,
                           SourceCProtocol, SourceCUniverse,
                           DestinationProtocol, DestinationUniverse,
                           SourceSelect, SumType, Frequency,
                           Recordable, Timeouted, RdmEnabled,
                           SumBFrom, SumBTo, SumCFrom, SumCTo
                      FROM NetGateConveyors
                     WHERE DeviceId = @id
                     ORDER BY Local;";
                AddParam(cmd, "@id", deviceId);
                await using var r = await cmd.ExecuteReaderAsync(ct).ConfigureAwait(false);
                while(await r.ReadAsync(ct).ConfigureAwait(false))
                {
                    dto.Conveyors.Add(new NetGateConveyorCfg
                    {
                        Local = r.GetInt32(0),
                        SourceA = new NetGateSourceCfg { Protocol = GetStringSafe(r, 1), Universe = GetInt32Safe(r, 2) },
                        SourceB = new NetGateSourceCfg { Protocol = GetStringSafe(r, 3), Universe = GetInt32Safe(r, 4) },
                        SourceC = new NetGateSourceCfg { Protocol = GetStringSafe(r, 5), Universe = GetInt32Safe(r, 6) },
                        Destination = new NetGateSourceCfg { Protocol = GetStringSafe(r, 7), Universe = GetInt32Safe(r, 8) },
                        SourceSelect = GetInt32Safe(r, 9),
                        SumType = GetInt32Safe(r, 10),
                        Frequency = GetInt32Safe(r, 11),
                        Recordable = GetInt32Safe(r, 12) != 0,
                        Timeouted = GetInt32Safe(r, 13) != 0,
                        RdmEnabled = GetInt32Safe(r, 14) != 0,
                        SumBFrom = GetInt32Safe(r, 15),
                        SumBTo = GetInt32Safe(r, 16),
                        SumCFrom = GetInt32Safe(r, 17),
                        SumCTo = GetInt32Safe(r, 18)
                    });
                }
            }

            // Channels
            dto.Channels = new List<NetGateChannelCfg>();
            await using(var cmd = conn.CreateCommand())
            {
                cmd.CommandText = @"
                    SELECT Local, Name, AdlpIndex
                      FROM NetGateChannels
                     WHERE DeviceId = @id
                     ORDER BY Local;";
                AddParam(cmd, "@id", deviceId);
                await using var r = await cmd.ExecuteReaderAsync(ct).ConfigureAwait(false);
                while(await r.ReadAsync(ct).ConfigureAwait(false))
                {
                    dto.Channels.Add(new NetGateChannelCfg
                    {
                        Local = r.GetInt32(0),
                        Name = GetStringSafe(r, 1),
                        Index = GetInt32Safe(r, 2)
                    });
                }
            }

            // Pictures
            dto.Pictures = new List<NetGatePictureCfg>();
            await using(var cmd = conn.CreateCommand())
            {
                cmd.CommandText = @"
                    SELECT Local, Picture, FadeIn, FadeOut,
                           Mux1, Mux2, Mux3, Mux4
                      FROM NetGatePictures
                     WHERE DeviceId = @id
                     ORDER BY Local;";
                AddParam(cmd, "@id", deviceId);
                await using var r = await cmd.ExecuteReaderAsync(ct).ConfigureAwait(false);
                while(await r.ReadAsync(ct).ConfigureAwait(false))
                {
                    dto.Pictures.Add(new NetGatePictureCfg
                    {
                        Local = r.GetInt32(0),
                        Picture = GetInt32Safe(r, 1),
                        FadeIn = GetInt32Safe(r, 2),
                        FadeOut = GetInt32Safe(r, 3),
                        Mux1 = GetInt32Safe(r, 4),
                        Mux2 = GetInt32Safe(r, 5),
                        Mux3 = GetInt32Safe(r, 6),
                        Mux4 = GetInt32Safe(r, 7)
                    });
                }
            }

            return dto;
        }

        // =========================================================================
        //  СПИСОК И УДАЛЕНИЕ
        // =========================================================================

        public async Task<List<DeviceHeader>> GetAllHeadersAsync(CancellationToken ct = default)
        {
            await using var conn = await OpenConnectionAsync(ct).ConfigureAwait(false);
            await using var cmd = conn.CreateCommand();
            cmd.CommandText = @"
                SELECT Id, Name, ConfigType, DeviceType, Mac, Ip, Mask, Gateway, Block
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
                    DeviceType = GetStringSafe(r, 3),
                    Mac = GetStringSafe(r, 4),
                    Ip = GetStringSafe(r, 5),
                    Mask = GetStringSafe(r, 6),
                    Gateway = GetStringSafe(r, 7),
                    Block = GetInt32Safe(r, 8)
                });
            }
            return list;
        }

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
                // NetGate
                "NetGatePictures",
                "NetGateChannels",
                "NetGateConveyors",
                "NetGateDevices",
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
            Pu6ConfigDto => TypePu6,
            PuBWConfigDto => TypePuBW,
            PuColorConfigDto => TypePu,
            ExecConfigDto => TypeExec,
            NetGateConfigDto => TypeNetGate,
            _ => throw new NotSupportedException($"Тип {dto.GetType().Name} не поддержан.")
        };

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

        private async Task<DbConnection> OpenConnectionAsync(CancellationToken ct)
        {
            DbConnection conn = _dbType switch
            {
                DatabaseType.SQLite => new SqliteConnection(_connectionString),
                DatabaseType.MySQL => new MySqlConnection(_connectionString),
                _ => throw new NotSupportedException()
            };

            await conn.OpenAsync(ct).ConfigureAwait(false);

            string userName = Environment.UserName;

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

        private static void AddParam(DbCommand cmd, string name, object? value)
        {
            var p = cmd.CreateParameter();
            p.ParameterName = name;
            p.Value = value ?? DBNull.Value;
            cmd.Parameters.Add(p);
        }

        private async Task DeleteDependentsAsync(DbConnection conn, DbTransaction tx,
                                                  long deviceId, CancellationToken ct)
        {
            // NetGate
            await ExecAsync(conn, tx, "DELETE FROM NetGatePictures WHERE DeviceId = @d;", ct,
                ("@d", deviceId)).ConfigureAwait(false);
            await ExecAsync(conn, tx, "DELETE FROM NetGateChannels WHERE DeviceId = @d;", ct,
                ("@d", deviceId)).ConfigureAwait(false);
            await ExecAsync(conn, tx, "DELETE FROM NetGateConveyors WHERE DeviceId = @d;", ct,
                ("@d", deviceId)).ConfigureAwait(false);
            await ExecAsync(conn, tx, "DELETE FROM NetGateDevices WHERE DeviceId = @d;", ct,
                ("@d", deviceId)).ConfigureAwait(false);

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
            DeviceType  TEXT NOT NULL DEFAULT '',
            SID         INTEGER NULL,
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

        @"CREATE TABLE IF NOT EXISTS ExecChannelStreams (
            DeviceId INTEGER NOT NULL,
            ChannelId INTEGER NOT NULL,
            StreamIndex INTEGER NOT NULL,
            PRIMARY KEY (DeviceId, ChannelId),
            FOREIGN KEY (DeviceId, ChannelId)
                REFERENCES ExecDeviceChannels(DeviceId, ChannelId) ON DELETE CASCADE);",

        @"CREATE TABLE IF NOT EXISTS _audit_context (
            key   TEXT PRIMARY KEY,
            value TEXT
        );",

        // ---- NetGate ----

        @"CREATE TABLE IF NOT EXISTS NetGateDevices (
            DeviceId INTEGER PRIMARY KEY,
            ConveyorCount INTEGER NOT NULL DEFAULT 0,
            FOREIGN KEY (DeviceId) REFERENCES Devices(Id) ON DELETE CASCADE);",

        @"CREATE TABLE IF NOT EXISTS NetGateConveyors (
            Id INTEGER PRIMARY KEY AUTOINCREMENT,
            DeviceId INTEGER NOT NULL,
            Local INTEGER NOT NULL,
            SourceAProtocol TEXT NOT NULL DEFAULT 'нет',
            SourceAUniverse INTEGER NOT NULL DEFAULT 0,
            SourceBProtocol TEXT NOT NULL DEFAULT 'нет',
            SourceBUniverse INTEGER NOT NULL DEFAULT 0,
            SourceCProtocol TEXT NOT NULL DEFAULT 'нет',
            SourceCUniverse INTEGER NOT NULL DEFAULT 0,
            DestinationProtocol TEXT NOT NULL DEFAULT 'нет',
            DestinationUniverse INTEGER NOT NULL DEFAULT 0,
            SourceSelect INTEGER NOT NULL DEFAULT 0,
            SumType INTEGER NOT NULL DEFAULT 0,
            Frequency INTEGER NOT NULL DEFAULT 0,
            Recordable INTEGER NOT NULL DEFAULT 0,
            Timeouted INTEGER NOT NULL DEFAULT 0,
            RdmEnabled INTEGER NOT NULL DEFAULT 0,
            SumBFrom INTEGER NOT NULL DEFAULT 0,
            SumBTo INTEGER NOT NULL DEFAULT 255,
            SumCFrom INTEGER NOT NULL DEFAULT 256,
            SumCTo INTEGER NOT NULL DEFAULT 511,
            FOREIGN KEY (DeviceId) REFERENCES Devices(Id) ON DELETE CASCADE);",

        @"CREATE TABLE IF NOT EXISTS NetGateChannels (
            Id INTEGER PRIMARY KEY AUTOINCREMENT,
            DeviceId INTEGER NOT NULL,
            Local INTEGER NOT NULL,
            Name TEXT NOT NULL DEFAULT '',
            AdlpIndex INTEGER NOT NULL DEFAULT 0,
            FOREIGN KEY (DeviceId) REFERENCES Devices(Id) ON DELETE CASCADE);",

        @"CREATE TABLE IF NOT EXISTS NetGatePictures (
            Id INTEGER PRIMARY KEY AUTOINCREMENT,
            DeviceId INTEGER NOT NULL,
            Local INTEGER NOT NULL,
            Picture INTEGER NOT NULL DEFAULT 0,
            FadeIn INTEGER NOT NULL DEFAULT 0,
            FadeOut INTEGER NOT NULL DEFAULT 0,
            Mux1 INTEGER NOT NULL DEFAULT 0,
            Mux2 INTEGER NOT NULL DEFAULT 0,
            Mux3 INTEGER NOT NULL DEFAULT 0,
            Mux4 INTEGER NOT NULL DEFAULT 0,
            FOREIGN KEY (DeviceId) REFERENCES Devices(Id) ON DELETE CASCADE);"
    };

        private static readonly string[] MySqlDdl =
        {
        @"CREATE TABLE IF NOT EXISTS Devices (
            Id          INT AUTO_INCREMENT PRIMARY KEY,
            Name        VARCHAR(255) NOT NULL DEFAULT '',
            ConfigType  VARCHAR(10) NOT NULL,
            DeviceType  VARCHAR(100) NOT NULL DEFAULT '',
            SID         INT NULL, 
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
        ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;",

        // ---- NetGate ----

        @"CREATE TABLE IF NOT EXISTS NetGateDevices (
            DeviceId INT PRIMARY KEY,
            ConveyorCount INT NOT NULL DEFAULT 0,
            CONSTRAINT fk_ng_device FOREIGN KEY (DeviceId)
                REFERENCES Devices(Id) ON DELETE CASCADE
        ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;",

        @"CREATE TABLE IF NOT EXISTS NetGateConveyors (
            Id INT AUTO_INCREMENT PRIMARY KEY,
            DeviceId INT NOT NULL,
            Local INT NOT NULL,
            SourceAProtocol VARCHAR(50) NOT NULL DEFAULT 'нет',
            SourceAUniverse INT NOT NULL DEFAULT 0,
            SourceBProtocol VARCHAR(50) NOT NULL DEFAULT 'нет',
            SourceBUniverse INT NOT NULL DEFAULT 0,
            SourceCProtocol VARCHAR(50) NOT NULL DEFAULT 'нет',
            SourceCUniverse INT NOT NULL DEFAULT 0,
            DestinationProtocol VARCHAR(50) NOT NULL DEFAULT 'нет',
            DestinationUniverse INT NOT NULL DEFAULT 0,
            SourceSelect INT NOT NULL DEFAULT 0,
            SumType INT NOT NULL DEFAULT 0,
            Frequency INT NOT NULL DEFAULT 0,
            Recordable TINYINT(1) NOT NULL DEFAULT 0,
            Timeouted TINYINT(1) NOT NULL DEFAULT 0,
            RdmEnabled TINYINT(1) NOT NULL DEFAULT 0,
            SumBFrom INT NOT NULL DEFAULT 0,
            SumBTo INT NOT NULL DEFAULT 255,
            SumCFrom INT NOT NULL DEFAULT 256,
            SumCTo INT NOT NULL DEFAULT 511,
            CONSTRAINT fk_ngc_device FOREIGN KEY (DeviceId)
                REFERENCES Devices(Id) ON DELETE CASCADE
        ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;",

        @"CREATE TABLE IF NOT EXISTS NetGateChannels (
            Id INT AUTO_INCREMENT PRIMARY KEY,
            DeviceId INT NOT NULL,
            Local INT NOT NULL,
            Name VARCHAR(255) NOT NULL DEFAULT '',
            AdlpIndex INT NOT NULL DEFAULT 0,
            CONSTRAINT fk_ngch_device FOREIGN KEY (DeviceId)
                REFERENCES Devices(Id) ON DELETE CASCADE
        ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;",

        @"CREATE TABLE IF NOT EXISTS NetGatePictures (
            Id INT AUTO_INCREMENT PRIMARY KEY,
            DeviceId INT NOT NULL,
            Local INT NOT NULL,
            Picture INT NOT NULL DEFAULT 0,
            FadeIn INT NOT NULL DEFAULT 0,
            FadeOut INT NOT NULL DEFAULT 0,
            Mux1 INT NOT NULL DEFAULT 0,
            Mux2 INT NOT NULL DEFAULT 0,
            Mux3 INT NOT NULL DEFAULT 0,
            Mux4 INT NOT NULL DEFAULT 0,
            CONSTRAINT fk_ngp_device FOREIGN KEY (DeviceId)
                REFERENCES Devices(Id) ON DELETE CASCADE
        ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;"
    };
    }
}