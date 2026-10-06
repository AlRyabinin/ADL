using AdlpLib.Models.Devices;
using AdlpLib.Models.DevicesGroup;
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
    /// <summary>Краткие сведения о группе для отображения в списке.</summary>
    public class DevicesGroupHeader
    {
        public long Id { get; set; }
        public string Name { get; set; } = "";
        public int DeviceCount { get; set; }
        public string DisplayText => $"{Name} (устройств : {DeviceCount})";

        public override string ToString() => DisplayText;
    }

    /// <summary>
    /// Асинхронный репозиторий групп устройств. Работает и с SQLite, и с MySQL
    /// в зависимости от <see cref="DatabaseType"/>. Группа ссылается на устройства
    /// по Id (таблица DeviceGroupDevices), сами конфиги — в DeviceConfigRepository.
    /// </summary>
    public class DevicesGroupRepository
    {
        private readonly string _connectionString;
        private readonly DatabaseType _dbType;
        private readonly DeviceConfigRepository _deviceRepo;

        public DevicesGroupRepository(string connectionString, DatabaseType dbType)
        : this(connectionString, dbType, null) { }

        public DevicesGroupRepository(string connectionString, DatabaseType dbType,
                                      DeviceConfigRepository deviceRepo)
        {
            _connectionString = connectionString;
            _dbType = dbType;
            _deviceRepo = deviceRepo ?? new DeviceConfigRepository(connectionString, dbType);
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

            await ValidateSchemaAsync(conn, ct).ConfigureAwait(false);
            await EnsureAuditColumnsAsync(conn, ct).ConfigureAwait(false);
            await EnsureAuditTriggersAsync(conn, ct).ConfigureAwait(false);
        }

        private async Task EnsureAuditColumnsAsync(DbConnection conn, CancellationToken ct)
        {
            var needed = new (string Name, string SqliteType, string MySqlType)[]
            {
                ("CreatedAt", "TEXT",         "DATETIME"),
                ("CreatedBy", "TEXT",         "VARCHAR(100)"),
                ("UpdatedAt", "TEXT",         "DATETIME"),
                ("UpdatedBy", "TEXT",         "VARCHAR(100)")
            };

            var existing = await GetTableColumnsAsync(conn, "DeviceGroups", ct)
                .ConfigureAwait(false);

            foreach(var (name, sqliteType, mySqlType) in needed)
            {
                if(existing.Contains(name, StringComparer.OrdinalIgnoreCase)) continue;

                await using var cmd = conn.CreateCommand();
                cmd.CommandText = _dbType == DatabaseType.SQLite
                    ? $"ALTER TABLE DeviceGroups ADD COLUMN {name} {sqliteType} NULL;"
                    : $"ALTER TABLE DeviceGroups ADD COLUMN `{name}` {mySqlType} NULL;";
                await cmd.ExecuteNonQueryAsync(ct).ConfigureAwait(false);
            }
        }

        private async Task EnsureAuditTriggersAsync(DbConnection conn, CancellationToken ct)
        {
            if(_dbType == DatabaseType.MySQL)
            {
                await ExecNoTxAsync(conn,
                    "DROP TRIGGER IF EXISTS trg_devicegroups_bi;", ct).ConfigureAwait(false);
                await ExecNoTxAsync(conn, @"
            CREATE TRIGGER trg_devicegroups_bi
            BEFORE INSERT ON DeviceGroups
            FOR EACH ROW
            BEGIN
                SET NEW.CreatedAt = UTC_TIMESTAMP();
                SET NEW.UpdatedAt = UTC_TIMESTAMP();
                SET NEW.CreatedBy = COALESCE(@app_user, USER());
                SET NEW.UpdatedBy = COALESCE(@app_user, USER());
            END;", ct).ConfigureAwait(false);

                await ExecNoTxAsync(conn,
                    "DROP TRIGGER IF EXISTS trg_devicegroups_bu;", ct).ConfigureAwait(false);
                await ExecNoTxAsync(conn, @"
            CREATE TRIGGER trg_devicegroups_bu
            BEFORE UPDATE ON DeviceGroups
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
                    "DROP TRIGGER IF EXISTS trg_devicegroups_ai;", ct).ConfigureAwait(false);
                await ExecNoTxAsync(conn, @"
            CREATE TRIGGER trg_devicegroups_ai
            AFTER INSERT ON DeviceGroups
            FOR EACH ROW
            BEGIN
                UPDATE DeviceGroups
                   SET CreatedAt = datetime('now'),
                       UpdatedAt = datetime('now'),
                       CreatedBy = COALESCE((SELECT value FROM _audit_context WHERE key = 'user'), ''),
                       UpdatedBy = COALESCE((SELECT value FROM _audit_context WHERE key = 'user'), '')
                 WHERE Id = NEW.Id;
            END;", ct).ConfigureAwait(false);

                await ExecNoTxAsync(conn,
                    "DROP TRIGGER IF EXISTS trg_devicegroups_au;", ct).ConfigureAwait(false);
                await ExecNoTxAsync(conn, @"
            CREATE TRIGGER trg_devicegroups_au
            AFTER UPDATE ON DeviceGroups
            FOR EACH ROW
            WHEN NEW.UpdatedAt IS OLD.UpdatedAt
            BEGIN
                UPDATE DeviceGroups
                   SET UpdatedAt = datetime('now'),
                       UpdatedBy = COALESCE((SELECT value FROM _audit_context WHERE key = 'user'), '')
                 WHERE Id = NEW.Id;
            END;", ct).ConfigureAwait(false);
            }
        }

        private async Task ValidateSchemaAsync(DbConnection conn, CancellationToken ct)
        {
            var required = new Dictionary<string, string[]>
            {
                ["DeviceGroups"] = new[] { "Id", "Name" },
                ["DeviceGroupDevices"] = new[] { "GroupId", "DeviceId", "Position" }
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
                list.Add(_dbType == DatabaseType.SQLite ? r.GetString(1) : r.GetString(0));
            return list;
        }

        private IReadOnlyList<string> GetDdlBatches() => _dbType switch
        {
            DatabaseType.SQLite => SqliteDdl,
            DatabaseType.MySQL => MySqlDdl,
            _ => throw new NotSupportedException($"Тип БД {_dbType} не поддержан.")
        };

        // =========================================================================
        //  СОХРАНЕНИЕ / ЗАГРУЗКА
        // =========================================================================

        /// <summary>
        /// Сохраняет группу. Если <c>Id == 0</c> — вставка, иначе — обновление
        /// названия и полная замена списка устройств.
        /// </summary>
        /// <returns>Идентификатор сохранённой группы.</returns>
        public async Task<long> SaveAsync(DevicesGroupDto dto, CancellationToken ct = default)
        {
            if(dto == null) throw new ArgumentNullException(nameof(dto));

            await using var conn = await OpenConnectionAsync(ct).ConfigureAwait(false);
            var tx = await conn.BeginTransactionAsync(ct).ConfigureAwait(false);

            try
            {
                long groupId;

                if(dto.Id == 0)
                {
                    await ExecAsync(conn, tx,
                        "INSERT INTO DeviceGroups (Name) VALUES (@n);",
                        ct, ("@n", dto.Name ?? "")).ConfigureAwait(false);
                    groupId = await GetLastInsertIdAsync(conn, tx, ct).ConfigureAwait(false);
                }
                else
                {
                    await ExecAsync(conn, tx,
                        "UPDATE DeviceGroups SET Name = @n WHERE Id = @id;",
                        ct,
                        ("@n", dto.Name ?? ""),
                        ("@id", dto.Id)).ConfigureAwait(false);
                    groupId = dto.Id;
                }

                // полная замена связей
                await ExecAsync(conn, tx,
                    "DELETE FROM DeviceGroupDevices WHERE GroupId = @g;",
                    ct, ("@g", groupId)).ConfigureAwait(false);

                if(dto.DeviceIds is { Count: > 0 })
                {
                    int pos = 0;
                    // Distinct — на случай дублей во входном списке (PK не даст вставить дважды).
                    foreach(var deviceId in dto.DeviceIds.Distinct())
                    {
                        await ExecAsync(conn, tx,
                            @"INSERT INTO DeviceGroupDevices (GroupId, DeviceId, Position)
                              VALUES (@g, @d, @p);",
                            ct,
                            ("@g", groupId),
                            ("@d", deviceId),
                            ("@p", pos++)).ConfigureAwait(false);
                    }
                }

                await tx.CommitAsync(ct).ConfigureAwait(false);
                dto.Id = groupId;
                return groupId;
            }
            catch
            {
                await tx.RollbackAsync(ct).ConfigureAwait(false);
                throw;
            }
        }

        /// <summary>Загружает группу по идентификатору. <c>null</c>, если не найдена.</summary>
        public async Task<DevicesGroupDto?> LoadAsync(long groupId, CancellationToken ct = default)
        {
            await using var conn = await OpenConnectionAsync(ct).ConfigureAwait(false);

            var dto = new DevicesGroupDto { Id = groupId };

            await using(var cmd = conn.CreateCommand())
            {
                cmd.CommandText = "SELECT Name FROM DeviceGroups WHERE Id = @id;";
                AddParam(cmd, "@id", groupId);
                var name = await cmd.ExecuteScalarAsync(ct).ConfigureAwait(false);
                if(name == null || name == DBNull.Value) return null;
                dto.Name = (string)name;
            }

            await using(var cmd = conn.CreateCommand())
            {
                cmd.CommandText = @"SELECT DeviceId
                                      FROM DeviceGroupDevices
                                     WHERE GroupId = @id
                                     ORDER BY Position;";
                AddParam(cmd, "@id", groupId);
                await using var r = await cmd.ExecuteReaderAsync(ct).ConfigureAwait(false);
                while(await r.ReadAsync(ct).ConfigureAwait(false))
                    dto.DeviceIds.Add(r.GetInt64(0));
            }

            return dto;
        }

        /// <summary>Краткие сведения обо всех группах (с числом устройств).</summary>
        public async Task<List<DevicesGroupHeader>> GetAllHeadersAsync(
            CancellationToken ct = default)
        {
            await using var conn = await OpenConnectionAsync(ct).ConfigureAwait(false);
            await using var cmd = conn.CreateCommand();
            cmd.CommandText = @"
                SELECT g.Id, g.Name, COUNT(d.DeviceId)
                  FROM DeviceGroups g
                  LEFT JOIN DeviceGroupDevices d ON d.GroupId = g.Id
                 GROUP BY g.Id, g.Name
                 ORDER BY g.Id;";

            var list = new List<DevicesGroupHeader>();
            await using var r = await cmd.ExecuteReaderAsync(ct).ConfigureAwait(false);
            while(await r.ReadAsync(ct).ConfigureAwait(false))
            {
                list.Add(new DevicesGroupHeader
                {
                    Id = r.GetInt64(0),
                    Name = r.GetString(1),
                    DeviceCount = Convert.ToInt32(r.GetValue(2))
                });
            }
            return list;
        }

        /// <summary>Все группы целиком — за два запроса, без N+1.</summary>
        public async Task<List<DevicesGroupDto>> GetAllAsync(CancellationToken ct = default)
        {
            await using var conn = await OpenConnectionAsync(ct).ConfigureAwait(false);

            var groups = new List<DevicesGroupDto>();
            var byId = new Dictionary<long, DevicesGroupDto>();

            await using(var cmd = conn.CreateCommand())
            {
                cmd.CommandText = "SELECT Id, Name FROM DeviceGroups ORDER BY Name;";
                await using var r = await cmd.ExecuteReaderAsync(ct).ConfigureAwait(false);
                while(await r.ReadAsync(ct).ConfigureAwait(false))
                {
                    var g = new DevicesGroupDto
                    {
                        Id = r.GetInt64(0),
                        Name = r.GetString(1)
                    };
                    groups.Add(g);
                    byId[g.Id] = g;
                }
            }

            if(groups.Count > 0)
            {
                await using var cmd = conn.CreateCommand();
                cmd.CommandText = @"SELECT GroupId, DeviceId
                                      FROM DeviceGroupDevices
                                     ORDER BY GroupId, Position;";
                await using var r = await cmd.ExecuteReaderAsync(ct).ConfigureAwait(false);
                while(await r.ReadAsync(ct).ConfigureAwait(false))
                {
                    long gid = r.GetInt64(0);
                    if(byId.TryGetValue(gid, out var g))
                        g.DeviceIds.Add(r.GetInt64(1));
                }
            }

            return groups;
        }

        /// <summary>Ищет Id группы по имени. <c>null</c>, если такой нет.</summary>
        public async Task<long?> FindGroupIdByNameAsync(string name, CancellationToken ct = default)
        {
            if(string.IsNullOrWhiteSpace(name)) return null;

            await using var conn = await OpenConnectionAsync(ct).ConfigureAwait(false);
            await using var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT Id FROM DeviceGroups WHERE Name = @n LIMIT 1;";
            AddParam(cmd, "@n", name);

            var result = await cmd.ExecuteScalarAsync(ct).ConfigureAwait(false);
            return result == null || result == DBNull.Value ? null : Convert.ToInt64(result);
        }

        // =========================================================================
        //  УДАЛЕНИЕ
        // =========================================================================

        /// <summary>Удаляет группу вместе со связями.</summary>
        public async Task DeleteAsync(long groupId, CancellationToken ct = default)
        {
            await using var conn = await OpenConnectionAsync(ct).ConfigureAwait(false);
            var tx = await conn.BeginTransactionAsync(ct).ConfigureAwait(false);
            try
            {
                await ExecAsync(conn, tx,
                    "DELETE FROM DeviceGroupDevices WHERE GroupId = @id;",
                    ct, ("@id", groupId)).ConfigureAwait(false);
                await ExecAsync(conn, tx,
                    "DELETE FROM DeviceGroups WHERE Id = @id;",
                    ct, ("@id", groupId)).ConfigureAwait(false);
                await tx.CommitAsync(ct).ConfigureAwait(false);
            }
            catch
            {
                await tx.RollbackAsync(ct).ConfigureAwait(false);
                throw;
            }
        }

        /// <summary>Удаляет все таблицы групп (для полного сброса БД).</summary>
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

            foreach(var table in new[] { "DeviceGroupDevices", "DeviceGroups" })
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

        // =========================================================================
        //  уСТРОЙСТВА ГРУППЫ
        // =========================================================================

        /// <summary>
        /// Возвращает полные DTO всех устройств группы, в порядке их следования.
        /// </summary>
        public async Task<List<DeviceConfigDtoBase>> GetDevicesAsync(long groupId,
                                                                     CancellationToken ct = default)
        {
            var ids = await GetDeviceIdsAsync(groupId, ct).ConfigureAwait(false);
            var result = new List<DeviceConfigDtoBase>(ids.Count);

            foreach(var id in ids)
            {
                var dto = await _deviceRepo.LoadAsync(id, ct).ConfigureAwait(false);
                if(dto != null) result.Add(dto);
            }

            return result;
        }

        /// <summary>
        /// Идентификаторы устройств группы, отсортированные по Block (затем по имени).
        /// Устройства с Block = 0 уезжают в конец списка.
        /// </summary>
        public async Task<List<long>> GetDeviceIdsAsync(long groupId,
                                                        CancellationToken ct = default)
        {
            await using var conn = await OpenConnectionAsync(ct).ConfigureAwait(false);
            await using var cmd = conn.CreateCommand();

            cmd.CommandText = @"
                SELECT dgd.DeviceId
                  FROM DeviceGroupDevices dgd
                  INNER JOIN Devices d ON d.Id = dgd.DeviceId
                 WHERE dgd.GroupId = @g
                 ORDER BY CASE WHEN d.Block = 0 THEN 1 ELSE 0 END,
                          d.Block,
                          d.Name;";

            AddParam(cmd, "@g", groupId);

            var list = new List<long>();
            await using var r = await cmd.ExecuteReaderAsync(ct).ConfigureAwait(false);
            while(await r.ReadAsync(ct).ConfigureAwait(false))
                list.Add(r.GetInt64(0));

            return list;
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

        private static async Task<int> GetNextPositionAsync(DbConnection conn, DbTransaction tx,
                                                            long groupId, CancellationToken ct)
        {
            await using var cmd = conn.CreateCommand();
            cmd.Transaction = tx;
            cmd.CommandText = @"SELECT COALESCE(MAX(Position), -1) + 1
                                  FROM DeviceGroupDevices WHERE GroupId = @g;";
            AddParam(cmd, "@g", groupId);
            var result = await cmd.ExecuteScalarAsync(ct).ConfigureAwait(false);
            return Convert.ToInt32(result);
        }

        private static async Task ExecNoTxAsync(DbConnection conn, string sql, CancellationToken ct)
        {
            await using var cmd = conn.CreateCommand();
            cmd.CommandText = sql;
            await cmd.ExecuteNonQueryAsync(ct).ConfigureAwait(false);
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

        // =========================================================================
        //  DDL
        // =========================================================================

        private static readonly string[] SqliteDdl =
        {
            @"CREATE TABLE IF NOT EXISTS DeviceGroups (
                Id          INTEGER PRIMARY KEY AUTOINCREMENT,
                Name        TEXT NOT NULL DEFAULT '',
                CreatedAt   TEXT NULL,
                CreatedBy   TEXT NULL,
                UpdatedAt   TEXT NULL,
                UpdatedBy   TEXT NULL
            );",

            @"CREATE TABLE IF NOT EXISTS DeviceGroupDevices (
                GroupId   INTEGER NOT NULL,
                DeviceId  INTEGER NOT NULL,
                Position  INTEGER NOT NULL DEFAULT 0,
                PRIMARY KEY (GroupId, DeviceId),
                FOREIGN KEY (GroupId)  REFERENCES DeviceGroups(Id) ON DELETE CASCADE,
                FOREIGN KEY (DeviceId) REFERENCES Devices(Id)      ON DELETE CASCADE
            );"
        };

        private static readonly string[] MySqlDdl =
        {
            @"CREATE TABLE IF NOT EXISTS DeviceGroups (
                Id          INT AUTO_INCREMENT PRIMARY KEY,
                Name        VARCHAR(255) NOT NULL DEFAULT '',
                CreatedAt   DATETIME     NULL,
                CreatedBy   VARCHAR(100) NULL,
                UpdatedAt   DATETIME     NULL,
                UpdatedBy   VARCHAR(100) NULL
            ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;",

            @"CREATE TABLE IF NOT EXISTS DeviceGroupDevices (
                GroupId   INT NOT NULL,
                DeviceId  INT NOT NULL,
                Position  INT NOT NULL DEFAULT 0,
                PRIMARY KEY (GroupId, DeviceId),
                CONSTRAINT fk_dgd_group  FOREIGN KEY (GroupId)
                    REFERENCES DeviceGroups(Id) ON DELETE CASCADE,
                CONSTRAINT fk_dgd_device FOREIGN KEY (DeviceId)
                    REFERENCES Devices(Id) ON DELETE CASCADE
            ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;"
        };
    }
}