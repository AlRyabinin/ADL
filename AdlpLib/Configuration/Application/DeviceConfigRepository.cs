#nullable enable
using AdlpLib.Core;
using AdlpLib.LiveConfig;
using AdlpLib.Models.Devices;
using AdlpLib.Models.Devices.Exec;
using AdlpLib.Models.Devices.NetGate;
using AdlpLib.Models.Devices.PU;
using AdlpLib.Persistence;
using AdlpLib.Persistence.Entities;
using Microsoft.EntityFrameworkCore;
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
    /// Строка представлений ListExecChannelsMulticastShot / ListPuChannelsMulticastShot
    /// (индекс канала ADLP и мультикаст-адрес устройства).
    /// </summary>
    internal sealed class MulticastViewRow
    {
        public int ChannelIndex { get; set; }
        public string Address { get; set; } = string.Empty;
    }

    /// <summary>Краткие сведения об устройстве для отображения в списке.</summary>
    public class DeviceHeader
    {
        public long Id { get; set; }
        public string ConfigType { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string DeviceType { get; set; } = string.Empty;
        public string Mac { get; set; } = string.Empty;
        public string Ip { get; set; } = string.Empty;
        public string Mask { get; set; } = string.Empty;
        public string Gateway { get; set; } = string.Empty;
        public int Block { get; set; }

        public override string ToString() => $"[{ConfigType}] {Name} [{Mac}]";
    }

    /// <summary>
    /// Репозиторий конфигураций, использующий EF Core для CRUD-операций.
    /// SQLite/MySQL выбирается через AdlpDbContextFactory. Прямой SQL оставлен
    /// только для DDL триггеров аудита и явного удаления схемы.
    /// </summary>
    public sealed class DeviceConfigRepository
    {
        private const string TypePu6 = "PU6";
        private const string TypePuBW = "PUBW";
        private const string TypePu = "PU";
        private const string TypeExec = "EXEC";
        private const string TypeIkb = "IKB";
        private const string TypeDimmer220 = "DIM220";
        private const string TypeDimmerCabinet = "DIMCABINET";
        private const string TypeNetGate = "NETGATE";

        private readonly string _connectionString;
        private readonly DatabaseType _dbType;

        public DeviceConfigRepository(string connectionString, DatabaseType dbType)
        {
            _connectionString = connectionString;
            _dbType = dbType;
        }

        private AdlpDbContext CreateDbContext() =>
            AdlpDbContextFactory.Create(_connectionString, _dbType);

        // ---------------------------------------------------------------------
        // Инициализация схемы. Структура таблиц управляется миграциями EF Core.
        // ---------------------------------------------------------------------

        public async Task EnsureSchemaAsync(CancellationToken ct = default)
        {
            await using var db = CreateDbContext();
            try
            {
                await AdlpDatabaseInitializer.ApplyPendingMigrationsAsync(db, ct)
                    .ConfigureAwait(false);
            }
            catch(Exception ex)
            {
                string diagnostics = await GetDatabaseDiagnosticsAsync(
                    CancellationToken.None);

                throw new InvalidOperationException(
                    ex.GetBaseException().Message +
                    Environment.NewLine +
                    Environment.NewLine +
                    diagnostics,
                    ex);
            }


            await db.Database.OpenConnectionAsync(ct).ConfigureAwait(false);
            try
            {
                await EnsureAuditTriggersAsync(db.Database.GetDbConnection(), ct).ConfigureAwait(false);
            }
            finally
            {
                await db.Database.CloseConnectionAsync().ConfigureAwait(false);
            }

            await PrepareConnectionAsync(db, ct).ConfigureAwait(false);
        }

        /// <summary>
        /// Оставляем триггеры аудита в БД: EF Core не имеет переносимого API
        /// для создания триггеров, зато все обычные операции ниже выполняются через EF.
        /// </summary>
        private async Task EnsureAuditTriggersAsync(DbConnection conn, CancellationToken ct)
        {
            if(_dbType == DatabaseType.MySQL)
            {
                await ExecuteDdlAsync(conn, "DROP TRIGGER IF EXISTS trg_devices_bi;", ct).ConfigureAwait(false);
                await ExecuteDdlAsync(conn, @"
                    CREATE TRIGGER trg_devices_bi
                    BEFORE INSERT ON Devices
                    FOR EACH ROW
                    BEGIN
                        SET NEW.CreatedAt = UTC_TIMESTAMP();
                        SET NEW.UpdatedAt = UTC_TIMESTAMP();
                        SET NEW.CreatedBy = COALESCE(@app_user, USER());
                        SET NEW.UpdatedBy = COALESCE(@app_user, USER());
                    END;", ct).ConfigureAwait(false);

                await ExecuteDdlAsync(conn, "DROP TRIGGER IF EXISTS trg_devices_bu;", ct).ConfigureAwait(false);
                await ExecuteDdlAsync(conn, @"
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
                await ExecuteDdlAsync(conn, "DROP TRIGGER IF EXISTS trg_devices_ai;", ct).ConfigureAwait(false);
                await ExecuteDdlAsync(conn, @"
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

                await ExecuteDdlAsync(conn, "DROP TRIGGER IF EXISTS trg_devices_au;", ct).ConfigureAwait(false);
                await ExecuteDdlAsync(conn, @"
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

        private static async Task ExecuteDdlAsync(DbConnection conn, string sql, CancellationToken ct)
        {
            await using var cmd = conn.CreateCommand();
            cmd.CommandText = sql;
            await cmd.ExecuteNonQueryAsync(ct).ConfigureAwait(false);
        }

        /// <summary>
        /// Открывает соединение на время операции и задаёт имя пользователя для старых
        /// audit-триггеров. При открытом соединении EF не переключает сессию посреди операции.
        /// </summary>
        private async Task PrepareConnectionAsync(AdlpDbContext db, CancellationToken ct)
        {
            await db.Database.OpenConnectionAsync(ct).ConfigureAwait(false);
            string userName = Environment.UserName;

            if(_dbType == DatabaseType.SQLite)
            {
                var row = await db.AuditContext
                    .SingleOrDefaultAsync(x => x.Key == "user", ct)
                    .ConfigureAwait(false);

                if(row == null)
                    db.AuditContext.Add(new AuditContextEntity { Key = "user", Value = userName });
                else
                    row.Value = userName;

                await db.SaveChangesAsync(ct).ConfigureAwait(false);
            }
            else
            {
                await db.Database.ExecuteSqlRawAsync("SET @app_user = {0};", new object[] { userName }, ct).ConfigureAwait(false);

                await db.Database.ExecuteSqlRawAsync("SET @BDConfigADLDCon_regWebUser = {0};", new object[] { userName }, ct).ConfigureAwait(false);
            }
        }

        // ---------------------------------------------------------------------
        // Сохранение
        // ---------------------------------------------------------------------

        public async Task<long> SaveAsync(DeviceConfigDtoBase dto, CancellationToken ct = default)
        {
            ArgumentNullException.ThrowIfNull(dto);

            await using var db = CreateDbContext();
            await PrepareConnectionAsync(db, ct).ConfigureAwait(false);
            await using var tx = await db.Database.BeginTransactionAsync(ct).ConfigureAwait(false);

            try
            {
                string configType = ConfigTypeOf(dto);

                // Выбранные SID и listUUID должны существовать и соответствовать друг другу и типу устройства.
                if(_dbType == DatabaseType.MySQL)
                    await ValidateListUuidAsync(db, dto, configType, ct).ConfigureAwait(false);

                DeviceEntity device;

                if(dto.Id == 0)
                {
                    device = null;

                    if(!string.IsNullOrWhiteSpace(dto.PkListUuid))
                    {
                        device = await db.Devices
                            .SingleOrDefaultAsync(
                                x => x.PkListUuid == dto.PkListUuid &&
                                     x.Block == dto.Block,
                                ct);
                    }

                    if(device == null)
                    {
                        device = new DeviceEntity();
                        db.Devices.Add(device);
                    }
                }
                else
                {
                    device = await db.Devices
                        .SingleOrDefaultAsync(x => x.Id == dto.Id, ct)
                        .ConfigureAwait(false)
                        ?? throw new InvalidOperationException($"Устройство с Id={dto.Id} не найдено.");
                }

                CopyToEntity(dto, device, configType);
                await db.SaveChangesAsync(ct).ConfigureAwait(false);
                long deviceId = device.Id;

                await DeleteDependentsAsync(db, deviceId, ct).ConfigureAwait(false);

                switch(dto)
                {
                    case PuConfigDtoBase pu:
                        await SavePuDependentsAsync(db, deviceId, pu, ct).ConfigureAwait(false);
                        break;
                    case ExecConfigDto exec:
                        await SaveExecDependentsAsync(db, deviceId, exec, ct).ConfigureAwait(false);
                        break;
                    case NetGateConfigDto netGate:
                        await SaveNetGateDependentsAsync(db, deviceId, netGate, ct).ConfigureAwait(false);
                        break;
                    default:
                        throw new NotSupportedException($"Тип {dto.GetType().Name} не поддержан.");
                }

                await db.SaveChangesAsync(ct).ConfigureAwait(false);
                await tx.CommitAsync(ct).ConfigureAwait(false);
                dto.Id = deviceId;
                return deviceId;
            }
            catch
            {
                await tx.RollbackAsync(CancellationToken.None).ConfigureAwait(false);
                throw;
            }
        }

        private static void CopyToEntity(DeviceConfigDtoBase dto, DeviceEntity entity, string configType)
        {
            entity.Name = dto.Name ?? string.Empty;
            entity.PkListUuid = string.IsNullOrWhiteSpace(dto.PkListUuid) ? null : dto.PkListUuid.Trim();
            entity.ConfigType = configType;
            entity.DeviceType = dto.DeviceType ?? string.Empty;
            entity.PkListSid = string.IsNullOrWhiteSpace(dto.PkListSid) ? null : dto.PkListSid.Trim();
            entity.Mac = dto.Mac ?? string.Empty;
            entity.Ip = dto.Ip ?? string.Empty;
            entity.Mask = dto.Mask ?? string.Empty;
            entity.Gateway = dto.Gateway ?? string.Empty;
            entity.Block = dto.Block;
        }

        // ---------------------------------------------------------------------
        // listUUID: связка SID + физическое устройство (SN) + тип устройства
        // ---------------------------------------------------------------------

        /// <summary>listUUID существует только в MySQL-схеме.</summary>
        public bool SupportsListUuid => _dbType == DatabaseType.MySQL;

        /// <summary>Записи listUUID для данного SID и типа устройства (для выпадающего списка).</summary>
        public async Task<List<ListUuidEntity>> GetListUuidsAsync(string sid, string configType, CancellationToken ct = default)
        {
            if(!SupportsListUuid || string.IsNullOrWhiteSpace(sid))
                return new List<ListUuidEntity>();

            await using var db = CreateDbContext();

            int typeId = await TryResolveTypeDeviceIdAsync(db, configType, ct).ConfigureAwait(false)
                ?? throw new InvalidOperationException(
                    $"Для типа '{configType}' нет соответствия в taypeDevices.");

            string key = sid.Trim();

            return await db.ListUuids.AsNoTracking()
                .Where(x => x.FkListSid == key && x.FkTypeDevices == typeId)
                .OrderBy(x => x.NumBlock)
                .ToListAsync(ct).ConfigureAwait(false);
        }

        /// <summary>
        /// Создаёт запись listUUID для (SID, SN, тип). PKlistUUID, NumBlock и regWebUser выдаёт триггер БД.
        /// </summary>
        public async Task<ListUuidEntity> CreateListUuidAsync(string sid, string configType, string sn, CancellationToken ct = default)
        {
            if(!SupportsListUuid)
                throw new NotSupportedException("Создание UUID поддерживается только в MySQL.");

            sid = sid?.Trim() ?? string.Empty;
            sn = sn?.Trim() ?? string.Empty;

            if(sid.Length == 0)
                throw new ArgumentException("Не выбран SID.");
            if(sn.Length == 0)
                throw new ArgumentException("Не указан SN.");
            if(sn.Length > 32)
                throw new ArgumentException("SN не должен превышать 32 символа.");

            await using var db = CreateDbContext();
            await PrepareConnectionAsync(db, ct).ConfigureAwait(false);
            await using var tx = await db.Database.BeginTransactionAsync(ct).ConfigureAwait(false);

            try
            {
                int typeId = await TryResolveTypeDeviceIdAsync(db, configType, ct).ConfigureAwait(false)
                    ?? throw new InvalidOperationException(
                        $"Для типа '{configType}' нет соответствия в taypeDevices.");

                if(!await db.ListSids.AnyAsync(x => x.PkListSid == sid, ct).ConfigureAwait(false))
                    throw new InvalidOperationException($"SID '{sid}' отсутствует в listSID.");

                if(await FindListUuidCoreAsync(db, sid, sn, typeId, ct).ConfigureAwait(false) != null)
                    throw new InvalidOperationException(
                        $"Запись с SN '{sn}' для этого SID и типа устройства уже существует.");

                await db.Database.ExecuteSqlRawAsync(
                    @"SET @BDConfigADLDCon_regWebUser =
                        COALESCE(@BDConfigADLDCon_regWebUser, @app_user, USER());",
                    ct).ConfigureAwait(false);

                await db.Database.ExecuteSqlInterpolatedAsync(
                    $@"INSERT INTO `listUUID`
                        (`SN`, `FKlistSID`, `FKtaypeDevices`, `idtxtDCon`)
                       VALUES
                        ({sn}, {sid}, {typeId}, {10000});",
                    ct).ConfigureAwait(false);

                string key = await FindListUuidCoreAsync(db, sid, sn, typeId, ct).ConfigureAwait(false)
                    ?? throw new InvalidOperationException(
                        "Запись listUUID создана, но PKlistUUID не найден.");

                await tx.CommitAsync(ct).ConfigureAwait(false);

                return await db.ListUuids.AsNoTracking()
                    .SingleAsync(x => x.PkListUuid == key, ct).ConfigureAwait(false);
            }
            catch
            {
                await tx.RollbackAsync(CancellationToken.None).ConfigureAwait(false);
                throw;
            }
        }

        public async Task<bool> ListSidExistsAsync(string sid, CancellationToken ct = default)
        {
            sid = sid?.Trim() ?? string.Empty;
            if(sid.Length == 0)
                return false;

            await using var db = CreateDbContext();
            return await db.ListSids.AsNoTracking()
                .AnyAsync(x => x.PkListSid == sid, ct).ConfigureAwait(false);
        }

        /// <summary>Запись listUUID по ключу или null, если её нет (или нет поддержки listUUID).</summary>
        public async Task<ListUuidEntity?> FindListUuidAsync(string pkListUuid, CancellationToken ct = default)
        {
            pkListUuid = pkListUuid?.Trim() ?? string.Empty;
            if(!SupportsListUuid || pkListUuid.Length == 0)
                return null;

            await using var db = CreateDbContext();
            return await db.ListUuids.AsNoTracking()
                .FirstOrDefaultAsync(x => x.PkListUuid == pkListUuid, ct).ConfigureAwait(false);
        }

        /// <summary>
        /// Добавляет в БД SID и/или UUID, считанные с устройства, если их там нет.
        /// Ключи (PKlistSID, PKlistUUID) передаются явно. Если триггер БД подменяет ключ своим,
        /// всё откатывается и выбрасывается InvalidOperationException.
        /// </summary>
        /// <returns>(SID добавлен, UUID добавлен)</returns>
        public async Task<(bool SidCreated, bool UuidCreated)> RegisterDeviceIdentityAsync(
            string sid,
            string? pkListUuid,
            string configType,
            string sidName,
            string sn,
            string comment,
            CancellationToken ct = default)
        {
            if(!SupportsListUuid)
                throw new NotSupportedException("Добавление SID и UUID поддерживается только в MySQL.");

            sid = sid?.Trim() ?? string.Empty;
            pkListUuid = string.IsNullOrWhiteSpace(pkListUuid) ? null : pkListUuid.Trim();
            sidName = sidName?.Trim() ?? string.Empty;
            sn = sn?.Trim() ?? string.Empty;
            comment ??= string.Empty;

            if(sid.Length != 8)
                throw new ArgumentException($"SID должен состоять из 8 символов: '{sid}'.");
            if(sidName.Length == 0 || sidName.Length > 64)
                throw new ArgumentException("Название SID должно быть от 1 до 64 символов.");
            if(comment.Length > 255)
                comment = comment.Substring(0, 255);
            if(pkListUuid != null && (sn.Length == 0 || sn.Length > 32))
                throw new ArgumentException("SN должен быть от 1 до 32 символов.");

            await using var db = CreateDbContext();
            await PrepareConnectionAsync(db, ct).ConfigureAwait(false);
            await using var tx = await db.Database.BeginTransactionAsync(ct).ConfigureAwait(false);

            try
            {
                await db.Database.ExecuteSqlRawAsync(
                    @"SET @BDConfigADLDCon_regWebUser =
                        COALESCE(@BDConfigADLDCon_regWebUser, @app_user, USER());",
                    ct).ConfigureAwait(false);

                bool sidCreated = false;
                if(!await db.ListSids.AnyAsync(x => x.PkListSid == sid, ct).ConfigureAwait(false))
                {
                    await db.Database.ExecuteSqlInterpolatedAsync(
                        $@"INSERT INTO `listSID`
                            (`PKlistSID`, `NameObjSID`, `idtxtDCon`, `comment`)
                           VALUES
                            ({sid}, {sidName}, {10000}, {comment});",
                        ct).ConfigureAwait(false);

                    if(!await db.ListSids.AnyAsync(x => x.PkListSid == sid, ct).ConfigureAwait(false))
                        throw new InvalidOperationException(
                            $"БД присвоила SID самостоятельно, а не '{sid}'. " +
                            "Добавить SID с устройства как есть не удалось, изменения отменены.");

                    sidCreated = true;
                }

                bool uuidCreated = false;
                if(pkListUuid != null &&
                   !await db.ListUuids.AnyAsync(x => x.PkListUuid == pkListUuid, ct).ConfigureAwait(false))
                {
                    int typeId = await TryResolveTypeDeviceIdAsync(db, configType, ct).ConfigureAwait(false)
                        ?? throw new InvalidOperationException(
                            $"Для типа '{configType}' нет соответствия в taypeDevices.");

                    if(await FindListUuidCoreAsync(db, sid, sn, typeId, ct).ConfigureAwait(false) != null)
                        throw new InvalidOperationException(
                            $"Для SID {sid} и этого типа уже есть запись с SN '{sn}' и другим UUID.");

                    await db.Database.ExecuteSqlInterpolatedAsync(
                        $@"INSERT INTO `listUUID`
                            (`PKlistUUID`, `SN`, `FKlistSID`, `FKtaypeDevices`, `idtxtDCon`)
                           VALUES
                            ({pkListUuid}, {sn}, {sid}, {typeId}, {10000});",
                        ct).ConfigureAwait(false);

                    if(!await db.ListUuids.AnyAsync(x => x.PkListUuid == pkListUuid, ct).ConfigureAwait(false))
                        throw new InvalidOperationException(
                            $"БД присвоила UUID самостоятельно, а не '{pkListUuid}'. " +
                            "Добавить UUID с устройства как есть не удалось, изменения отменены.");

                    uuidCreated = true;
                }

                await tx.CommitAsync(ct).ConfigureAwait(false);
                return (sidCreated, uuidCreated);
            }
            catch
            {
                await tx.RollbackAsync(CancellationToken.None).ConfigureAwait(false);
                throw;
            }
        }

        // ConfigType -> taypeDevices.Name. typeCodeUnit для этого не годится:
        // это числовой код семейства (char(2)), и он не уникален (PU24 и PU24C = "02").
        private static readonly Dictionary<string, string> TaypeDeviceNameByConfigType =
            new(StringComparer.OrdinalIgnoreCase)
            {
                [TypeIkb] = "IKB24",
                [TypePuBW] = "PU24",
                [TypePu] = "PU24C",
                [TypePu6] = "PU6",
                [TypeDimmerCabinet] = "DC96",
                [TypeNetGate] = "NET GATE"
            };

        private static async Task<int?> TryResolveTypeDeviceIdAsync(AdlpDbContext db, string configType, CancellationToken ct)
        {
            if(!TaypeDeviceNameByConfigType.TryGetValue(configType.Trim(), out var name))
                return null;

            var ids = await db.TaypeDevices.AsNoTracking()
                .Where(x => x.Name == name)
                .Select(x => (int?)x.PkTaypeDevices)
                .Take(2)
                .ToListAsync(ct).ConfigureAwait(false);

            if(ids.Count > 1)
                throw new InvalidOperationException(
                    $"В taypeDevices несколько записей с Name '{name}'.");

            return ids.Count == 1 ? ids[0] : null;
        }

        private static async Task<string?> FindListUuidCoreAsync(
    AdlpDbContext db,
    string sid,
    string sn,
    int typeId,
    CancellationToken ct)
        {
            var keys = await db.ListUuids
                .AsNoTracking()
                .Where(x => x.FkListSid == sid &&
                            x.FkTypeDevices == typeId &&
                            x.Sn == sn)
                .Select(x => x.PkListUuid)
                .Take(2)
                .ToListAsync(ct)
                .ConfigureAwait(false);

            if(keys.Count > 1)
                throw new InvalidOperationException(
                    $"В listUUID несколько записей для SID {sid}, SN '{sn}', типа {typeId}.");

            return keys.Count == 1
                ? keys[0].ToString().ToUpperInvariant()
                : null;
        }

        /// <summary>
        /// Проверяет перед сохранением: SID и listUUID выбраны, listUUID существует, относится к тому же SID
        /// и к тому же типу устройства. Devices.PKlistSID и PKlistUUID в БД NOT NULL.
        /// </summary>
        private async Task ValidateListUuidAsync(
            AdlpDbContext db, DeviceConfigDtoBase dto, string configType, CancellationToken ct)
        {
            string? sid = dto.PkListSid?.Trim();
            if(string.IsNullOrEmpty(sid))
                throw new InvalidOperationException("Не назначен SID устройства.");

            string? key = dto.PkListUuid?.Trim();
            if(string.IsNullOrEmpty(key))
                throw new InvalidOperationException("Не выбран UUID устройства (listUUID).");

            var row = await db.ListUuids.AsNoTracking()
                .SingleOrDefaultAsync(x => x.PkListUuid == key, ct).ConfigureAwait(false)
                ?? throw new InvalidOperationException($"UUID '{key}' отсутствует в listUUID.");

            if(!string.Equals(row.FkListSid.Trim(), sid, StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException(
                    $"SID устройства ({sid}) не совпадает с SID записи listUUID ({row.FkListSid.Trim()}). " +
                    "Выберите UUID, созданный для этого SID.");

            int typeId = await TryResolveTypeDeviceIdAsync(db, configType, ct).ConfigureAwait(false)
                ?? throw new InvalidOperationException(
                    $"Для типа '{configType}' нет соответствия в taypeDevices.");

            if(row.FkTypeDevices != typeId)
                throw new InvalidOperationException(
                    "Тип устройства не совпадает с типом записи listUUID.");
        }

        public async Task<List<TaypeDeviceEntity>> GetTaypeDevicesAsync(CancellationToken ct = default)
        {
            await using var db = CreateDbContext();

            return await db.TaypeDevices
                .AsNoTracking()
                .OrderBy(x => x.Name)
                .ToListAsync(ct);
        }

        private static async Task SavePuDependentsAsync(
            AdlpDbContext db, long deviceId, PuConfigDtoBase pu, CancellationToken ct)
        {
            db.PuDevices.Add(new PuDeviceEntity
            {
                DeviceId = deviceId,
                Priority = pu.Priority,
                PictureSwitchTime = pu.PictureSwitchTime,
                PageCount = pu.PageCount,
                SwapX = pu.SwapX,
                SwapY = pu.SwapY,
                Brightness = pu.Brightness,
                BrightnessSleep = pu.BrightnessSleep,
                SleepTimeout = pu.SleepTimeout
            });

            if(pu.Channels is { Count: > 0 })
            {
                var channels = pu.Channels.Select(ch => new PuChannelEntity
                {
                    Local = ch.Local,
                    Name = ch.Name ?? string.Empty,
                    AdlpIndex = ch.Index,
                    Type = ch.Type
                }).ToList();

                db.PuChannels.AddRange(channels);
                await db.SaveChangesAsync(ct).ConfigureAwait(false);

                db.PuDeviceChannels.AddRange(channels.Select((ch, position) => new PuDeviceChannelEntity
                {
                    DeviceId = deviceId,
                    ChannelId = ch.Id,
                    Position = position
                }));
            }

            if(pu.Groups is { Count: > 0 })
            {
                var groups = pu.Groups.Select(group => (Dto: group, Entity: new PuGroupEntity
                {
                    Local = group.Local,
                    AdlpIndex = group.Index
                })).ToList();

                db.PuGroups.AddRange(groups.Select(x => x.Entity));
                await db.SaveChangesAsync(ct).ConfigureAwait(false);

                foreach(var item in groups)
                {
                    db.PuDeviceGroups.Add(new PuDeviceGroupEntity
                    {
                        DeviceId = deviceId,
                        GroupId = item.Entity.Id,
                        Position = item.Dto.Local
                    });

                    if(item.Dto.Channels is { Count: > 0 })
                    {
                        db.PuGroupChannels.AddRange(item.Dto.Channels.Select((channelAdlpIndex, index) =>
                            new PuGroupChannelEntity
                            {
                                GroupId = item.Entity.Id,
                                ChannelAdlpIndex = channelAdlpIndex,
                                Slot = index + 1
                            }));
                    }
                }
            }

            if(pu.Pictures is { Count: > 0 })
            {
                var pictures = pu.Pictures.Select(picture => (Dto: picture, Entity: new PuPictureEntity
                {
                    Local = picture.Local,
                    Name = picture.Name ?? string.Empty
                })).ToList();

                db.PuPictures.AddRange(pictures.Select(x => x.Entity));
                await db.SaveChangesAsync(ct).ConfigureAwait(false);

                db.PuDevicePictures.AddRange(pictures.Select((item, position) => new PuDevicePictureEntity
                {
                    DeviceId = deviceId,
                    PictureId = item.Entity.Id,
                    Position = position
                }));
            }

            // Кнопки существуют только у цветного ПУ.
            if(pu is PuColorConfigDto color && color.Buttons is { Count: > 0 })
            {
                var buttons = color.Buttons.Select(button => (Dto: button, Entity: new PuButtonEntity
                {
                    Local = button.Local,
                    Page = button.Page,
                    LocalChannel = button.LocalChannel,
                    PosX = button.PosX,
                    PosY = button.PosY,
                    Icon = button.Icon,
                    Color = button.Color
                })).ToList();

                db.PuButtons.AddRange(buttons.Select(x => x.Entity));
                await db.SaveChangesAsync(ct).ConfigureAwait(false);

                db.PuDeviceButtons.AddRange(buttons.Select((item, position) => new PuDeviceButtonEntity
                {
                    DeviceId = deviceId,
                    ButtonId = item.Entity.Id,
                    Position = position
                }));

                db.PuButtonChannels.AddRange(buttons
                    .Where(x => x.Dto.LocalChannel > 0)
                    .Select(x => new PuButtonChannelEntity
                    {
                        DeviceId = deviceId,
                        ButtonId = x.Entity.Id,
                        ChannelLocal = x.Dto.LocalChannel
                    }));
            }
        }

        private static async Task SaveExecDependentsAsync(
            AdlpDbContext db, long deviceId, ExecConfigDto exec, CancellationToken ct)
        {
            db.ExecDevices.Add(new ExecDeviceEntity
            {
                DeviceId = deviceId,
                BaseChannel = exec.BaseChannel ?? string.Empty
            });

            var streamIndexByPackedId = new Dictionary<int, int>();
            if(exec.Streams is { Count: > 0 })
            {
                var streams = exec.Streams.Select(stream => new ExecStreamEntity
                {
                    StreamIndex = stream.Index,
                    Protocol = stream.Protocol ?? "нет",
                    Universe = stream.Universe,
                    Address = stream.Address
                }).ToList();

                db.ExecStreams.AddRange(streams);
                await db.SaveChangesAsync(ct).ConfigureAwait(false);

                db.ExecDeviceStreams.AddRange(streams.Select((stream, position) => new ExecDeviceStreamEntity
                {
                    DeviceId = deviceId,
                    StreamId = stream.Id,
                    Position = position
                }));

                foreach(var stream in exec.Streams)
                    streamIndexByPackedId[stream.GetPackedStreamId()] = stream.Index;
            }

            if(exec.Channels is { Count: > 0 })
            {
                var channels = exec.Channels.Select(channel => (Dto: channel, Entity: new ExecChannelEntity
                {
                    Local = channel.Local,
                    GlobalIndex = channel.GlobalIndex,
                    Name = channel.Name ?? string.Empty,
                    Control = channel.Control ?? "adlp",
                    Wide = channel.Wide,
                    Patch = channel.Patch,
                    Preheat = channel.Preheat,
                    LimitValue = channel.Limit,
                    Curve = channel.Curve,
                    SwitchDelay = channel.SwitchDelay
                })).ToList();

                db.ExecChannels.AddRange(channels.Select(x => x.Entity));
                await db.SaveChangesAsync(ct).ConfigureAwait(false);

                db.ExecDeviceChannels.AddRange(channels.Select((item, position) => new ExecDeviceChannelEntity
                {
                    DeviceId = deviceId,
                    ChannelId = item.Entity.Id,
                    Position = position
                }));

                db.ExecChannelStreams.AddRange(channels
                    .Where(item => item.Dto.StreamId != 0 && streamIndexByPackedId.ContainsKey(item.Dto.StreamId))
                    .Select(item => new ExecChannelStreamEntity
                    {
                        DeviceId = deviceId,
                        ChannelId = item.Entity.Id,
                        StreamIndex = streamIndexByPackedId[item.Dto.StreamId]
                    }));
            }
        }

        private static async Task SaveNetGateDependentsAsync(
            AdlpDbContext db, long deviceId, NetGateConfigDto netGate, CancellationToken ct)
        {
            db.NetGateDevices.Add(new NetGateDeviceEntity
            {
                DeviceId = deviceId,
                ConveyorCount = netGate.ConveyorCount
            });

            if(netGate.Conveyors is { Count: > 0 })
            {
                db.NetGateConveyors.AddRange(netGate.Conveyors.Select(conveyor => new NetGateConveyorEntity
                {
                    DeviceId = deviceId,
                    Local = conveyor.Local,
                    SourceAProtocol = conveyor.SourceA?.Protocol ?? "нет",
                    SourceAUniverse = conveyor.SourceA?.Universe ?? 0,
                    SourceBProtocol = conveyor.SourceB?.Protocol ?? "нет",
                    SourceBUniverse = conveyor.SourceB?.Universe ?? 0,
                    SourceCProtocol = conveyor.SourceC?.Protocol ?? "нет",
                    SourceCUniverse = conveyor.SourceC?.Universe ?? 0,
                    DestinationProtocol = conveyor.Destination?.Protocol ?? "нет",
                    DestinationUniverse = conveyor.Destination?.Universe ?? 0,
                    SourceSelect = conveyor.SourceSelect,
                    SumType = conveyor.SumType,
                    Frequency = conveyor.Frequency,
                    Recordable = conveyor.Recordable,
                    Timeouted = conveyor.Timeouted,
                    RdmEnabled = conveyor.RdmEnabled,
                    SumBFrom = conveyor.SumBFrom,
                    SumBTo = conveyor.SumBTo,
                    SumCFrom = conveyor.SumCFrom,
                    SumCTo = conveyor.SumCTo
                }));
            }

            if(netGate.Channels is { Count: > 0 })
            {
                db.NetGateChannels.AddRange(netGate.Channels.Select(channel => new NetGateChannelEntity
                {
                    DeviceId = deviceId,
                    Local = channel.Local,
                    Name = channel.Name ?? string.Empty,
                    AdlpIndex = channel.Index
                }));
            }

            if(netGate.Pictures is { Count: > 0 })
            {
                db.NetGatePictures.AddRange(netGate.Pictures.Select(picture => new NetGatePictureEntity
                {
                    DeviceId = deviceId,
                    Local = picture.Local,
                    Picture = picture.Picture,
                    FadeIn = picture.FadeIn,
                    FadeOut = picture.FadeOut,
                    Mux1 = picture.Mux1,
                    Mux2 = picture.Mux2,
                    Mux3 = picture.Mux3,
                    Mux4 = picture.Mux4
                }));
            }

            await db.SaveChangesAsync(ct).ConfigureAwait(false);
        }

        public async Task<(string OwnAddress, List<string> Subscriptions)> GetMulticastSettingsAsync(long deviceId, CancellationToken ct = default)
        {
            await using var db = CreateDbContext();

            string ownAddress = await db.DeviceMulticastAddresses
                .AsNoTracking()
                .Where(x => x.DeviceId == deviceId)
                .Select(x => x.MultiCastAddress)
                .FirstOrDefaultAsync(ct) ?? string.Empty;

            var subscriptions = await db.DeviceMulticastSubscriptions
                .AsNoTracking()
                .Where(x => x.DeviceId == deviceId)
                .Select(x => x.MulticastAddress)
                .ToListAsync(ct);

            return (ownAddress, subscriptions);
        }

        public static string GetConfigTypeFromDeviceName(string deviceName)
        {
            string type = deviceName.Trim()
                .Replace(" ", "")
                .ToUpperInvariant();

            return type switch
            {
                "PU" => TypePu,
                "PU6" => TypePu6,
                "PUBW" => TypePuBW,
                "IKB" => TypeIkb,
                "DIM220" => TypeDimmer220,
                "DIMCABINET" => TypeDimmerCabinet,
                "NETGATE" => TypeNetGate,
                "EXEC" => TypeExec,

                _ => throw new NotSupportedException(
                    $"Неизвестный тип устройства: {deviceName}")
            };
        }

        public async Task<long> GetOrCreateDeviceIdAsync(string? pkListUuid, int block, string deviceName, string? ipAddress = null, string? macAddress = null, CancellationToken ct = default)
        {
            await using var db = CreateDbContext();
            await PrepareConnectionAsync(db, ct);

            string configType = GetConfigTypeFromDeviceName(deviceName);

            var query = db.Devices
                .Where(x => x.Block == block &&
                            x.ConfigType == configType);

            if(!string.IsNullOrWhiteSpace(pkListUuid))
            {
                pkListUuid = pkListUuid.Trim();

                bool uuidExists = await db.ListUuids
                    .AnyAsync(x => x.PkListUuid == pkListUuid, ct);

                if(!uuidExists)
                    throw new InvalidOperationException(
                        $"PKlistUUID '{pkListUuid}' отсутствует в listUUID.");

                query = query.Where(x => x.PkListUuid == pkListUuid);
            }
            else if(!string.IsNullOrWhiteSpace(macAddress))
            {
                query = query.Where(x => x.Mac == macAddress.Trim());
            }
            else if(!string.IsNullOrWhiteSpace(ipAddress))
            {
                query = query.Where(x => x.Ip == ipAddress.Trim());
            }
            else
            {
                throw new InvalidOperationException(
                    "Для поиска устройства необходимо указать PKlistUUID, MAC или IP.");
            }

            var ids = await query
                .Select(x => x.Id)
                .Take(2)
                .ToListAsync(ct);

            if(ids.Count > 1)
                throw new InvalidOperationException(
                    "Найдено несколько устройств. Уточните идентификатор.");

            if(ids.Count == 1)
                return ids[0];

            var device = new DeviceEntity
            {
                PkListUuid = pkListUuid,
                Block = block,
                Name = deviceName,
                DeviceType = deviceName,
                ConfigType = configType,
                Mac = macAddress?.Trim() ?? string.Empty,
                Ip = ipAddress ?? string.Empty,
                Mask = string.Empty,
                Gateway = string.Empty
            };

            db.Devices.Add(device);
            await db.SaveChangesAsync(ct);

            return device.Id;
        }

        // Удаляет дочерние конфигурации перед повторным сохранением. Общие справочники
        // каналов/групп/картин/кнопок очищаются только после удаления связей и если они не используются.
        private static async Task DeleteDependentsAsync(AdlpDbContext db, long deviceId, CancellationToken ct)
        {
            await db.NetGatePictures.Where(x => x.DeviceId == deviceId).ExecuteDeleteAsync(ct).ConfigureAwait(false);
            await db.NetGateChannels.Where(x => x.DeviceId == deviceId).ExecuteDeleteAsync(ct).ConfigureAwait(false);
            await db.NetGateConveyors.Where(x => x.DeviceId == deviceId).ExecuteDeleteAsync(ct).ConfigureAwait(false);
            await db.NetGateDevices.Where(x => x.DeviceId == deviceId).ExecuteDeleteAsync(ct).ConfigureAwait(false);

            await db.PuButtonChannels.Where(x => x.DeviceId == deviceId).ExecuteDeleteAsync(ct).ConfigureAwait(false);
            await db.PuDeviceButtons.Where(x => x.DeviceId == deviceId).ExecuteDeleteAsync(ct).ConfigureAwait(false);
            await db.PuButtons.Where(button => !db.PuDeviceButtons.Any(link => link.ButtonId == button.Id))
                .ExecuteDeleteAsync(ct).ConfigureAwait(false);

            await db.PuGroupChannels
                .Where(channel => db.PuDeviceGroups.Any(link => link.DeviceId == deviceId && link.GroupId == channel.GroupId))
                .ExecuteDeleteAsync(ct).ConfigureAwait(false);
            await db.PuDeviceGroups.Where(x => x.DeviceId == deviceId).ExecuteDeleteAsync(ct).ConfigureAwait(false);
            await db.PuGroups.Where(group => !db.PuDeviceGroups.Any(link => link.GroupId == group.Id))
                .ExecuteDeleteAsync(ct).ConfigureAwait(false);

            await db.PuDevicePictures.Where(x => x.DeviceId == deviceId).ExecuteDeleteAsync(ct).ConfigureAwait(false);
            await db.PuPictures.Where(picture => !db.PuDevicePictures.Any(link => link.PictureId == picture.Id))
                .ExecuteDeleteAsync(ct).ConfigureAwait(false);

            await db.PuDeviceChannels.Where(x => x.DeviceId == deviceId).ExecuteDeleteAsync(ct).ConfigureAwait(false);
            await db.PuChannels.Where(channel => !db.PuDeviceChannels.Any(link => link.ChannelId == channel.Id))
                .ExecuteDeleteAsync(ct).ConfigureAwait(false);
            await db.PuDevices.Where(x => x.DeviceId == deviceId).ExecuteDeleteAsync(ct).ConfigureAwait(false);

            await db.ExecChannelStreams.Where(x => x.DeviceId == deviceId).ExecuteDeleteAsync(ct).ConfigureAwait(false);
            await db.ExecDeviceChannels.Where(x => x.DeviceId == deviceId).ExecuteDeleteAsync(ct).ConfigureAwait(false);
            await db.ExecChannels.Where(channel => !db.ExecDeviceChannels.Any(link => link.ChannelId == channel.Id))
                .ExecuteDeleteAsync(ct).ConfigureAwait(false);
            await db.ExecDeviceStreams.Where(x => x.DeviceId == deviceId).ExecuteDeleteAsync(ct).ConfigureAwait(false);
            await db.ExecStreams.Where(stream => !db.ExecDeviceStreams.Any(link => link.StreamId == stream.Id))
                .ExecuteDeleteAsync(ct).ConfigureAwait(false);
            await db.ExecDevices.Where(x => x.DeviceId == deviceId).ExecuteDeleteAsync(ct).ConfigureAwait(false);
        }

        // ---------------------------------------------------------------------
        // Загрузка
        // ---------------------------------------------------------------------

        public async Task<DeviceConfigDtoBase?> LoadAsync(long deviceId, CancellationToken ct = default)
        {
            await using var db = CreateDbContext();
            var device = await db.Devices.AsNoTracking()
                .SingleOrDefaultAsync(x => x.Id == deviceId, ct)
                .ConfigureAwait(false);
            if(device == null)
                return null;

            DeviceConfigDtoBase dto = device.ConfigType switch
            {
                TypePu6 => await LoadPuAsync<Pu6ConfigDto>(db, device, ct).ConfigureAwait(false),
                TypePuBW => await LoadPuAsync<PuBWConfigDto>(db, device, ct).ConfigureAwait(false),
                TypePu => await LoadPuAsync<PuColorConfigDto>(db, device, ct).ConfigureAwait(false),
                TypeIkb => await LoadExecAsync<IkbConfigDto>(db, device, ct).ConfigureAwait(false),
                TypeDimmer220 => await LoadExecAsync<Dimmer220ConfigDto>(db, device, ct).ConfigureAwait(false),
                TypeDimmerCabinet => await LoadExecAsync<DimmerCabinetConfigDto>(db, device, ct).ConfigureAwait(false),
                TypeExec => await LoadExecAsync<ExecConfigDto>(db, device, ct).ConfigureAwait(false),
                TypeNetGate => await LoadNetGateAsync(db, device, ct).ConfigureAwait(false),
                _ => throw new NotSupportedException($"Неизвестный ConfigType: '{device.ConfigType}'.")
            };

            return dto;
        }

        public async Task<ListSidEntity> CreateListSidAsync(string nameObjSid, string comment, CancellationToken ct = default)
        {
            if(_dbType != DatabaseType.MySQL)
                throw new NotSupportedException(
                    "Создание SID поддерживается только в MySQL.");

            if(string.IsNullOrWhiteSpace(nameObjSid))
                throw new ArgumentException("Укажите название SID.");

            if(nameObjSid.Trim().Length > 64)
                throw new ArgumentException(
                    "Название не должно превышать 64 символа.");

            comment ??= string.Empty;

            if(comment.Length > 255)
                throw new ArgumentException(
                    "Комментарий не должен превышать 255 символов.");

            string temporaryComment =
                $"__ADL_CREATE_{Guid.NewGuid():N}";

            await using var db = CreateDbContext();

            await PrepareConnectionAsync(db, ct);

            await using var tx =
                await db.Database.BeginTransactionAsync(ct);

            try
            {
                await db.Database.ExecuteSqlRawAsync(
                    @"SET @BDConfigADLDCon_regWebUser =
                COALESCE(
                    @BDConfigADLDCon_regWebUser,
                    @app_user,
                    USER()
                );",
                    ct);

                await db.Database.ExecuteSqlInterpolatedAsync(
                    $@"INSERT INTO `listSID`
                (`NameObjSID`, `idtxtDCon`, `comment`)
              VALUES (
                {nameObjSid.Trim()},
                {10000},
                {temporaryComment}
              );",
                    ct);

                string generatedKey = await db.Database
                    .SqlQueryRaw<string>(
                        @"SELECT `PKlistSID` AS `Value`
                  FROM `listSID`
                  WHERE `comment` = {0}",
                        temporaryComment)
                    .SingleAsync(ct);

                await db.Database.ExecuteSqlInterpolatedAsync(
                    $@"UPDATE `listSID`
               SET `comment` = {comment}
               WHERE `PKlistSID` = {generatedKey};",
                    ct);

                await tx.CommitAsync(ct);

                return await db.ListSids
                    .AsNoTracking()
                    .SingleAsync(
                        x => x.PkListSid == generatedKey,
                        ct);
            }
            catch
            {
                await tx.RollbackAsync(CancellationToken.None);
                throw;
            }
        }

        public async Task<List<ListSidEntity>> GetListSidsAsync(CancellationToken ct = default)
        {
            await using var db = CreateDbContext();

            return await db.ListSids
                .AsNoTracking()
                .OrderBy(x => x.NameObjSid)
                .ToListAsync(ct);
        }

        private static void CopyCommonToDto(DeviceEntity device, DeviceConfigDtoBase dto)
        {
            dto.Id = device.Id;
            dto.PkListUuid = device.PkListUuid;
            dto.Name = device.Name;
            dto.PkListSid = device.PkListSid;
            dto.Mac = device.Mac;
            dto.Ip = device.Ip;
            dto.Mask = device.Mask;
            dto.Gateway = device.Gateway;
            dto.Block = device.Block;
        }

        private static async Task<TPu> LoadPuAsync<TPu>(
            AdlpDbContext db, DeviceEntity device, CancellationToken ct)
            where TPu : PuConfigDtoBase, new()
        {
            var dto = new TPu();
            CopyCommonToDto(device, dto);

            var pu = await db.PuDevices.AsNoTracking()
                .SingleOrDefaultAsync(x => x.DeviceId == device.Id, ct).ConfigureAwait(false);
            if(pu != null)
            {
                dto.Priority = pu.Priority;
                dto.PictureSwitchTime = pu.PictureSwitchTime;
                dto.PageCount = pu.PageCount;
                dto.SwapX = pu.SwapX;
                dto.SwapY = pu.SwapY;
                dto.Brightness = pu.Brightness;
                dto.BrightnessSleep = pu.BrightnessSleep;
                dto.SleepTimeout = pu.SleepTimeout;
            }

            var channelRows = await (from link in db.PuDeviceChannels.AsNoTracking()
                                     join channel in db.PuChannels.AsNoTracking() on link.ChannelId equals channel.Id
                                     where link.DeviceId == device.Id
                                     orderby link.Position
                                     select new { channel.Local, channel.Name, channel.AdlpIndex, channel.Type })
                .ToListAsync(ct).ConfigureAwait(false);
            dto.Channels = channelRows.Select(x => new LiveControlChannelCfg
            {
                Local = x.Local,
                Name = x.Name,
                Index = x.AdlpIndex,
                Type = x.Type
            }).ToList();

            var groupLinks = await db.PuDeviceGroups.AsNoTracking()
                .Where(x => x.DeviceId == device.Id)
                .OrderBy(x => x.Position)
                .ToListAsync(ct).ConfigureAwait(false);
            var groupIds = groupLinks.Select(x => x.GroupId).ToList();
            var groupsById = groupIds.Count == 0
                ? new Dictionary<long, PuGroupEntity>()
                : await db.PuGroups.AsNoTracking().Where(x => groupIds.Contains(x.Id))
                    .ToDictionaryAsync(x => x.Id, ct).ConfigureAwait(false);
            var groupChannelRows = groupIds.Count == 0
                ? new List<PuGroupChannelEntity>()
                : await db.PuGroupChannels.AsNoTracking().Where(x => groupIds.Contains(x.GroupId))
                    .OrderBy(x => x.Slot).ToListAsync(ct).ConfigureAwait(false);

            dto.Groups = groupLinks
                .Where(link => groupsById.ContainsKey(link.GroupId))
                .Select(link =>
                {
                    var group = groupsById[link.GroupId];
                    return new LiveControlGroupCfg
                    {
                        Local = group.Local,
                        Index = group.AdlpIndex,
                        Channels = groupChannelRows.Where(x => x.GroupId == group.Id)
                            .OrderBy(x => x.Slot)
                            .Select(x => x.ChannelAdlpIndex)
                            .ToList()
                    };
                }).ToList();

            var pictureRows = await (from link in db.PuDevicePictures.AsNoTracking()
                                     join picture in db.PuPictures.AsNoTracking() on link.PictureId equals picture.Id
                                     where link.DeviceId == device.Id
                                     orderby link.Position
                                     select new { picture.Local, picture.Name })
                .ToListAsync(ct).ConfigureAwait(false);
            dto.Pictures = pictureRows.Select(x => new LiveControlPictureCfg
            {
                Local = x.Local,
                Name = x.Name
            }).ToList();

            if(dto is PuColorConfigDto color)
            {
                var buttonRows = await (from link in db.PuDeviceButtons.AsNoTracking()
                                        join button in db.PuButtons.AsNoTracking() on link.ButtonId equals button.Id
                                        where link.DeviceId == device.Id
                                        orderby link.Position
                                        select new
                                        {
                                            button.Local,
                                            button.Page,
                                            button.LocalChannel,
                                            button.PosX,
                                            button.PosY,
                                            button.Icon,
                                            button.Color
                                        })
                    .ToListAsync(ct).ConfigureAwait(false);

                color.Buttons = buttonRows.Select(x => new LiveControlButtonCfg
                {
                    Local = x.Local,
                    Page = x.Page,
                    LocalChannel = x.LocalChannel,
                    PosX = x.PosX,
                    PosY = x.PosY,
                    Icon = x.Icon,
                    Color = x.Color
                }).ToList();
            }

            return dto;
        }

        private static async Task<TExec> LoadExecAsync<TExec>(
            AdlpDbContext db, DeviceEntity device, CancellationToken ct)
            where TExec : ExecConfigDto, new()
        {
            var dto = new TExec();
            CopyCommonToDto(device, dto);

            var execDevice = await db.ExecDevices.AsNoTracking()
                .SingleOrDefaultAsync(x => x.DeviceId == device.Id, ct).ConfigureAwait(false);
            dto.BaseChannel = execDevice?.BaseChannel ?? string.Empty;

            var streamRows = await (from link in db.ExecDeviceStreams.AsNoTracking()
                                    join stream in db.ExecStreams.AsNoTracking() on link.StreamId equals stream.Id
                                    where link.DeviceId == device.Id
                                    orderby link.Position
                                    select new { stream.StreamIndex, stream.Protocol, stream.Universe, stream.Address })
                .ToListAsync(ct).ConfigureAwait(false);
            dto.Streams = streamRows.Select(x => new LiveStreamCfg
            {
                Index = x.StreamIndex,
                Protocol = x.Protocol,
                Universe = x.Universe,
                Address = x.Address
            }).ToList();

            var streamPackedIds = dto.Streams
                .GroupBy(x => x.Index)
                .ToDictionary(x => x.Key, x => x.First().GetPackedStreamId());
            var channelStreamRows = await db.ExecChannelStreams.AsNoTracking()
                .Where(x => x.DeviceId == device.Id)
                .ToListAsync(ct).ConfigureAwait(false);
            var streamIndexByChannelId = channelStreamRows
                .GroupBy(x => x.ChannelId)
                .ToDictionary(x => x.Key, x => x.First().StreamIndex);

            var channelRows = await (from link in db.ExecDeviceChannels.AsNoTracking()
                                     join channel in db.ExecChannels.AsNoTracking() on link.ChannelId equals channel.Id
                                     where link.DeviceId == device.Id
                                     orderby link.Position
                                     select new
                                     {
                                         link.ChannelId,
                                         channel.Local,
                                         channel.GlobalIndex,
                                         channel.Name,
                                         channel.Control,
                                         channel.Wide,
                                         channel.Patch,
                                         channel.Preheat,
                                         channel.LimitValue,
                                         channel.Curve,
                                         channel.SwitchDelay
                                     })
                .ToListAsync(ct).ConfigureAwait(false);

            dto.Channels = channelRows.Select(x =>
            {
                int streamId = streamIndexByChannelId.TryGetValue(x.ChannelId, out var streamIndex)
                    && streamPackedIds.TryGetValue(streamIndex, out var packedId)
                    ? packedId
                    : 0;

                return new LiveChannelCfg
                {
                    Local = x.Local,
                    GlobalIndex = x.GlobalIndex,
                    Name = x.Name,
                    Control = x.Control,
                    Wide = x.Wide,
                    StreamId = streamId,
                    Patch = x.Patch,
                    Preheat = x.Preheat,
                    Limit = x.LimitValue,
                    Curve = x.Curve,
                    SwitchDelay = x.SwitchDelay
                };
            }).ToList();

            return dto;
        }

        private static async Task<NetGateConfigDto> LoadNetGateAsync(
            AdlpDbContext db, DeviceEntity device, CancellationToken ct)
        {
            var dto = new NetGateConfigDto();
            CopyCommonToDto(device, dto);

            var netGate = await db.NetGateDevices.AsNoTracking()
                .SingleOrDefaultAsync(x => x.DeviceId == device.Id, ct).ConfigureAwait(false);
            dto.ConveyorCount = netGate?.ConveyorCount ?? 0;

            var conveyorRows = await db.NetGateConveyors.AsNoTracking()
                .Where(x => x.DeviceId == device.Id)
                .OrderBy(x => x.Local)
                .ToListAsync(ct).ConfigureAwait(false);
            dto.Conveyors = conveyorRows.Select(x => new NetGateConveyorCfg
            {
                Local = x.Local,
                SourceA = new NetGateSourceCfg { Protocol = x.SourceAProtocol, Universe = x.SourceAUniverse },
                SourceB = new NetGateSourceCfg { Protocol = x.SourceBProtocol, Universe = x.SourceBUniverse },
                SourceC = new NetGateSourceCfg { Protocol = x.SourceCProtocol, Universe = x.SourceCUniverse },
                Destination = new NetGateSourceCfg { Protocol = x.DestinationProtocol, Universe = x.DestinationUniverse },
                SourceSelect = x.SourceSelect,
                SumType = x.SumType,
                Frequency = x.Frequency,
                Recordable = x.Recordable,
                Timeouted = x.Timeouted,
                RdmEnabled = x.RdmEnabled,
                SumBFrom = x.SumBFrom,
                SumBTo = x.SumBTo,
                SumCFrom = x.SumCFrom,
                SumCTo = x.SumCTo
            }).ToList();

            dto.Channels = await db.NetGateChannels.AsNoTracking()
                .Where(x => x.DeviceId == device.Id)
                .OrderBy(x => x.Local)
                .Select(x => new NetGateChannelCfg
                {
                    Local = x.Local,
                    Name = x.Name,
                    Index = x.AdlpIndex
                })
                .ToListAsync(ct).ConfigureAwait(false);

            dto.Pictures = await db.NetGatePictures.AsNoTracking()
                .Where(x => x.DeviceId == device.Id)
                .OrderBy(x => x.Local)
                .Select(x => new NetGatePictureCfg
                {
                    Local = x.Local,
                    Picture = x.Picture,
                    FadeIn = x.FadeIn,
                    FadeOut = x.FadeOut,
                    Mux1 = x.Mux1,
                    Mux2 = x.Mux2,
                    Mux3 = x.Mux3,
                    Mux4 = x.Mux4
                })
                .ToListAsync(ct).ConfigureAwait(false);

            return dto;
        }

        // ---------------------------------------------------------------------
        // Список, поиск, удаление
        // ---------------------------------------------------------------------

        public async Task<List<DeviceHeader>> GetAllHeadersAsync(CancellationToken ct = default)
        {
            await using var db = CreateDbContext();
            return await db.Devices.AsNoTracking()
                .OrderBy(x => x.ConfigType)
                .ThenBy(x => x.Mac)
                .Select(x => new DeviceHeader
                {
                    Id = x.Id,
                    ConfigType = x.ConfigType,
                    Name = x.Name,
                    DeviceType = x.DeviceType,
                    Mac = x.Mac,
                    Ip = x.Ip,
                    Mask = x.Mask,
                    Gateway = x.Gateway,
                    Block = x.Block
                })
                .ToListAsync(ct).ConfigureAwait(false);
        }

        public async Task SaveMulticastSettingsAsync(long deviceId, string? ownAddress, IReadOnlyCollection<string> subscriptions, CancellationToken ct = default)
        {
            if(deviceId <= 0)
                throw new ArgumentOutOfRangeException(nameof(deviceId));

            ArgumentNullException.ThrowIfNull(subscriptions);

            var addresses = subscriptions
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Select(x => x.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            if(addresses.Count > 32)
                throw new ArgumentException(
                    "Максимальное количество подписок — 32.");

            ownAddress = string.IsNullOrWhiteSpace(ownAddress)
                ? string.Empty
                : ownAddress.Trim();

            if(ownAddress.Length > 0 && !IsMulticastAddress(ownAddress))
                throw new ArgumentException(
                    "Некорректный собственный multicast-адрес.");

            foreach(var address in addresses)
            {
                if(!IsMulticastAddress(address))
                    throw new ArgumentException(
                        $"Некорректный multicast-адрес: {address}");
            }

            await using var db = CreateDbContext();

            await PrepareConnectionAsync(db, ct).ConfigureAwait(false);

            if(!await db.Devices.AnyAsync(x => x.Id == deviceId, ct))
                throw new InvalidOperationException(
                    $"Устройство с Id={deviceId} не найдено.");

            await using var tx =
                await db.Database.BeginTransactionAsync(ct);

            try
            {
                var own = await db.DeviceMulticastAddresses
                    .FindAsync(new object[] { deviceId }, ct);

                if(!string.IsNullOrEmpty(ownAddress))
                {
                    var ownerId = await db.DeviceMulticastAddresses
                        .Where(x =>
                            x.MultiCastAddress == ownAddress &&
                            x.DeviceId != deviceId)
                        .Select(x => (long?)x.DeviceId)
                        .FirstOrDefaultAsync(ct);

                    if(ownerId.HasValue)
                    {
                        throw new InvalidOperationException(
                            $"Multicast-адрес {ownAddress} уже назначен " +
                            $"устройству Id={ownerId.Value}.");
                    }
                }

                bool ownChanged;

                if(string.IsNullOrEmpty(ownAddress))
                {
                    ownChanged = own != null;
                }
                else
                {
                    ownChanged = own == null ||
                        !string.Equals(
                            own.MultiCastAddress,
                            ownAddress,
                            StringComparison.OrdinalIgnoreCase);
                }

                var oldAddresses = await db.DeviceMulticastSubscriptions
                    .Where(x => x.DeviceId == deviceId)
                    .Select(x => x.MulticastAddress)
                    .ToListAsync(ct);

                bool subscriptionsChanged =
                    !new HashSet<string>(oldAddresses, StringComparer.OrdinalIgnoreCase)
                        .SetEquals(addresses);

                if(!ownChanged && !subscriptionsChanged)
                {
                    await tx.CommitAsync(ct);
                    return;
                }

                if(ownChanged)
                {
                    if(string.IsNullOrEmpty(ownAddress))
                    {
                        if(own != null)
                            db.DeviceMulticastAddresses.Remove(own);
                    }
                    else if(own == null)
                    {
                        db.DeviceMulticastAddresses.Add(
                            new DeviceMulticastAddressEntity
                            {
                                DeviceId = deviceId,
                                MultiCastAddress = ownAddress
                            });
                    }
                    else
                    {
                        own.MultiCastAddress = ownAddress;
                    }
                }

                if(subscriptionsChanged)
                {
                    var oldSubscriptions =
                        await db.DeviceMulticastSubscriptions
                            .Where(x => x.DeviceId == deviceId)
                            .ToListAsync(ct);

                    db.DeviceMulticastSubscriptions.RemoveRange(
                        oldSubscriptions);

                    db.DeviceMulticastSubscriptions.AddRange(
                        addresses.Select((address) =>
                            new DeviceMulticastSubscriptionEntity
                            {
                                DeviceId = deviceId,
                                MulticastAddress = address
                            }));
                }

                await db.SaveChangesAsync(ct);

                await tx.CommitAsync(ct);
            }
            catch
            {
                await tx.RollbackAsync(CancellationToken.None);
                throw;
            }
        }

        private static bool IsMulticastAddress(string address)
        {
            if(!System.Net.IPAddress.TryParse(address, out var ip) ||
               ip.AddressFamily !=
                   System.Net.Sockets.AddressFamily.InterNetwork)
                return false;

            byte[] bytes = ip.GetAddressBytes();

            return bytes[0] >= 224 && bytes[0] <= 239;
        }

        public async Task DeleteAsync(long deviceId, CancellationToken ct = default)
        {
            await using var db = CreateDbContext();
            await PrepareConnectionAsync(db, ct).ConfigureAwait(false);
            await using var tx = await db.Database.BeginTransactionAsync(ct).ConfigureAwait(false);
            try
            {
                await DeleteDependentsAsync(db, deviceId, ct).ConfigureAwait(false);

                var device = await db.Devices.SingleOrDefaultAsync(x => x.Id == deviceId, ct)
                    .ConfigureAwait(false);
                if(device != null)
                    db.Devices.Remove(device);

                await db.SaveChangesAsync(ct).ConfigureAwait(false);
                await tx.CommitAsync(ct).ConfigureAwait(false);
            }
            catch
            {
                await tx.RollbackAsync(CancellationToken.None).ConfigureAwait(false);
                throw;
            }
        }

        /// <summary>
        /// Явное удаление таблиц (административная операция). Обычная работа репозитория
        /// не использует ручной SQL. Таблица истории удаляется последней, чтобы EnsureSchemaAsync
        /// смог повторно запустить начальную миграцию после сброса.
        /// </summary>
        public async Task DropSchemaAsync(CancellationToken ct = default)
        {
            await using var db = CreateDbContext();
            await db.Database.OpenConnectionAsync(ct).ConfigureAwait(false);
            try
            {
                var conn = db.Database.GetDbConnection();
                await ExecuteDdlAsync(conn, _dbType == DatabaseType.SQLite
                    ? "PRAGMA foreign_keys = OFF;"
                    : "SET FOREIGN_KEY_CHECKS = 0;", ct).ConfigureAwait(false);

                string[] tables =
                {
                    "DeviceGroupDevices", "DeviceGroups",
                    "NetGatePictures", "NetGateChannels", "NetGateConveyors", "NetGateDevices",
                    "ExecChannelStreams", "ExecDeviceChannels", "ExecChannels",
                    "ExecDeviceStreams", "ExecStreams", "ExecDevices",
                    "PuButtonChannels", "PuDeviceButtons", "PuButtons",
                    "PuDevicePictures", "PuPictures", "PuGroupChannels", "PuDeviceGroups",
                    "PuGroups", "PuDeviceChannels", "PuChannels", "PuDevices",

                    "DeviceMulticastSubscriptions",
                    "DeviceMulticastAddresses",

                    "Devices", "_audit_context", "__EFMigrationsHistory"
                };

                foreach(var table in tables)
                {
                    string quoted = _dbType == DatabaseType.SQLite ? $"\"{table}\"" : $"`{table}`";
                    await ExecuteDdlAsync(conn, $"DROP TABLE IF EXISTS {quoted};", ct).ConfigureAwait(false);
                }

                await ExecuteDdlAsync(conn, _dbType == DatabaseType.SQLite
                    ? "PRAGMA foreign_keys = ON;"
                    : "SET FOREIGN_KEY_CHECKS = 1;", ct).ConfigureAwait(false);
            }
            finally
            {
                await db.Database.CloseConnectionAsync().ConfigureAwait(false);
            }
        }

        public static string ConfigTypeOf(DeviceConfigDtoBase dto) => dto switch
        {
            Pu6ConfigDto => TypePu6,
            PuBWConfigDto => TypePuBW,
            PuColorConfigDto => TypePu,
            IkbConfigDto => TypeIkb,
            Dimmer220ConfigDto => TypeDimmer220,
            DimmerCabinetConfigDto => TypeDimmerCabinet,
            ExecConfigDto => TypeExec,
            NetGateConfigDto => TypeNetGate,
            _ => throw new NotSupportedException($"Тип {dto.GetType().Name} не поддержан.")
        };

        public async Task<string> GetOrAssignMulticastAddressAsync(long deviceId, CancellationToken ct = default)
        {
            if(deviceId <= 0)
                throw new ArgumentOutOfRangeException(nameof(deviceId));

            await using var db = CreateDbContext();

            await PrepareConnectionAsync(db, ct);

            await using var tx = await db.Database.BeginTransactionAsync(
                System.Data.IsolationLevel.Serializable, ct);

            var device = await db.Devices
                .AsNoTracking()
                .SingleOrDefaultAsync(x => x.Id == deviceId, ct);

            if(device == null)
                throw new InvalidOperationException(
                    $"Устройство с Id={deviceId} не найдено.");

            var own = await db.DeviceMulticastAddresses
                .SingleOrDefaultAsync(x => x.DeviceId == deviceId, ct);

            if(own != null && !string.IsNullOrWhiteSpace(own.MultiCastAddress))
            {
                await tx.CommitAsync(ct);
                return own.MultiCastAddress;
            }

            var range = GetMulticastAddressRange(device.ConfigType);

            var usedAddresses = await db.DeviceMulticastAddresses
                .AsNoTracking()
                .Where(x => x.DeviceId != deviceId)
                .Select(x => x.MultiCastAddress)
                .ToListAsync(ct);

            var used = new HashSet<string>(
                usedAddresses,
                StringComparer.OrdinalIgnoreCase);

            string? newAddress = null;

            for(int third = range.First; third <= range.Last; third++)
            {
                for(int fourth = 0; fourth <= 255; fourth++)
                {
                    string candidate = $"239.251.{third}.{fourth}";

                    if(!used.Contains(candidate))
                    {
                        newAddress = candidate;
                        break;
                    }
                }

                if(newAddress != null)
                    break;
            }

            if(newAddress == null)
            {
                throw new InvalidOperationException(
                    $"Закончились multicast-адреса для типа {device.ConfigType}. " +
                    $"Диапазон 239.251.{range.First}.0 – " +
                    $"239.251.{range.Last}.0 полностью занят.");
            }

            if(own == null)
            {
                db.DeviceMulticastAddresses.Add(
                    new DeviceMulticastAddressEntity
                    {
                        DeviceId = deviceId,
                        MultiCastAddress = newAddress
                    });
            }
            else
            {
                own.MultiCastAddress = newAddress;
            }

            await db.SaveChangesAsync(ct);
            await tx.CommitAsync(ct);

            return newAddress;
        }

        private static (int First, int Last) GetMulticastAddressRange(string configType)
        {
            return configType.Trim().ToUpperInvariant() switch
            {
                TypeExec or TypeIkb or TypeDimmer220 or TypeDimmerCabinet => (1, 10),

                TypePu or TypePu6 or TypePuBW => (11, 20),

                TypeNetGate => (21, 30),

                _ => (31, 40)
            };
        }

        /// <summary>
        /// Адреса мультикаста устройств, подходящих текущему по пересечению индексов каналов.
        /// В MySQL адреса берутся одним запросом из представления: для PU это
        /// ListExecChannelsMulticastShot, для Exec это ListPuChannelsMulticastShot.
        /// Если представления нет или это SQLite, используется полный перебор устройств.
        /// </summary>
        public async Task<List<string>> GetMatchingMulticastAddressesAsync(long deviceId, CancellationToken ct = default)
        {
            var currentConfig = await LoadAsync(deviceId, ct);

            if(currentConfig == null)
                return new List<string>();

            var family = GetMulticastFamily(currentConfig);

            if(SupportsListUuid &&
               (family == MulticastDeviceFamily.Pu || family == MulticastDeviceFamily.Exec))
            {
                var indexes = GetMulticastChannelIndexes(currentConfig);

                if(indexes.Count == 0)
                    return new List<string>();

                string sql = family == MulticastDeviceFamily.Pu
                    ? ExecChannelsMulticastSql
                    : PuChannelsMulticastSql;

                try
                {
                    return await GetMulticastAddressesFromViewAsync(sql, indexes, ct);
                }
                catch(DbException ex)
                {
                    System.Diagnostics.Debug.WriteLine(
                        $"Представление мультикаста недоступно, полный перебор: {ex.Message}");
                }
            }

            return await GetMatchingMulticastAddressesScanAsync(deviceId, currentConfig, ct);
        }

        private const string ExecChannelsMulticastSql =
            @"SELECT IndexADLP_ExecChannel AS ChannelIndex,
                     multicastExecChannel  AS Address
              FROM ListExecChannelsMulticastShot";

        private const string PuChannelsMulticastSql =
            @"SELECT IndexADLP_PuChannel   AS ChannelIndex,
                     multicastPuChannel    AS Address
              FROM ListPuChannelsMulticastShot";

        private async Task<List<string>> GetMulticastAddressesFromViewAsync(
            string viewSql, IEnumerable<int> indexes, CancellationToken ct)
        {
            var indexList = indexes.Distinct().ToList();

            await using var db = CreateDbContext();

            var addresses = await db.Database
                .SqlQueryRaw<MulticastViewRow>(viewSql)
                .Where(x => indexList.Contains(x.ChannelIndex))
                .Select(x => x.Address)
                .Distinct()
                .ToListAsync(ct)
                .ConfigureAwait(false);

            return addresses
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Select(x => x.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(x => x)
                .ToList();
        }

        private async Task<List<string>> GetMatchingMulticastAddressesScanAsync(
            long deviceId, DeviceConfigDtoBase currentConfig, CancellationToken ct)
        {
            var currentFamily = GetMulticastFamily(currentConfig);

            if(currentFamily != MulticastDeviceFamily.Pu &&
               currentFamily != MulticastDeviceFamily.Exec)
                return new List<string>();

            var currentIndexes = GetMulticastChannelIndexes(currentConfig);

            if(currentIndexes.Count == 0)
                return new List<string>();

            var headers = await GetAllHeadersAsync(ct);

            var result = new HashSet<string>(
                StringComparer.OrdinalIgnoreCase);

            foreach(var header in headers)
            {
                ct.ThrowIfCancellationRequested();

                if(header.Id == deviceId)
                    continue;

                var otherConfig = await LoadAsync(header.Id, ct);

                if(otherConfig == null)
                    continue;

                var otherFamily = GetMulticastFamily(otherConfig);

                bool validPair =
                    (currentFamily == MulticastDeviceFamily.Pu &&
                     otherFamily == MulticastDeviceFamily.Exec) ||
                    (currentFamily == MulticastDeviceFamily.Exec &&
                     otherFamily == MulticastDeviceFamily.Pu);

                if(!validPair)
                    continue;

                var otherIndexes = GetMulticastChannelIndexes(otherConfig);

                if(!currentIndexes.Overlaps(otherIndexes))
                    continue;

                var multicast = await GetMulticastSettingsAsync(
                    header.Id, ct);

                if(string.IsNullOrWhiteSpace(multicast.OwnAddress))
                    continue;

                result.Add(multicast.OwnAddress.Trim());
            }

            return result.OrderBy(x => x).ToList();
        }

        private enum MulticastDeviceFamily
        {
            Exec,
            Pu,
            NetGate
        }

        private static MulticastDeviceFamily? GetMulticastFamily(DeviceConfigDtoBase dto)
        {
            return dto switch
            {
                ExecConfigDto => MulticastDeviceFamily.Exec,

                PuConfigDtoBase => MulticastDeviceFamily.Pu,

                NetGateConfigDto => MulticastDeviceFamily.NetGate,

                _ => null
            };
        }

        private static HashSet<int> GetMulticastChannelIndexes(DeviceConfigDtoBase dto)
        {
            return dto switch
            {
                ExecConfigDto exec => new HashSet<int>(
                    exec.Channels?
                        .Where(x => x.GlobalIndex > 0)
                        .Select(x => x.GlobalIndex)
                    ?? Enumerable.Empty<int>()),

                PuConfigDtoBase pu => new HashSet<int>(
                    pu.Groups?
                        .Where(group => group.Channels != null)
                        .SelectMany(group => group.Channels)
                        .Where(index => index > 0)
                    ?? Enumerable.Empty<int>()),

                NetGateConfigDto gate => new HashSet<int>(
                    gate.Channels?
                        .Where(x => x.Index > 0)
                        .Select(x => x.Index)
                    ?? Enumerable.Empty<int>()),

                _ => new HashSet<int>()
            };
        }

        public async Task<long?> FindDeviceIdByListUuidAndBlockAsync(string? pkListUuid, int block, CancellationToken ct = default)
        {
            if(string.IsNullOrWhiteSpace(pkListUuid))
                return null;

            await using var db = CreateDbContext();

            return await db.Devices
                .AsNoTracking()
                .Where(x =>
                    x.PkListUuid == pkListUuid.Trim() &&
                    x.Block == block)
                .Select(x => (long?)x.Id)
                .SingleOrDefaultAsync(ct);
        }

        public async Task<long?> GetMulticastAddressOwnerAsync(string address, long currentDeviceId, CancellationToken ct = default)
        {
            await using var db = CreateDbContext();

            return await db.DeviceMulticastAddresses
                .Where(x =>
                    x.MultiCastAddress == address &&
                    x.DeviceId != currentDeviceId)
                .Select(x => (long?)x.DeviceId)
                .FirstOrDefaultAsync(ct);
        }

        public async Task<string> GetDatabaseDiagnosticsAsync(CancellationToken ct = default)
        {
            var sb = new StringBuilder();

            await using var db = CreateDbContext();

            sb.AppendLine($"Provider: {db.Database.ProviderName}");
            sb.AppendLine($"Migration assembly: {typeof(AdlpDbContext).Assembly.Location}");

            try
            {
                var all = db.Database.GetMigrations();
                var applied = db.Database.GetAppliedMigrations();
                var pending = db.Database.GetPendingMigrations();

                sb.AppendLine();
                sb.AppendLine("=== ALL MIGRATIONS ===");

                foreach(var item in all)
                    sb.AppendLine(item);

                sb.AppendLine();
                sb.AppendLine("=== APPLIED MIGRATIONS ===");

                foreach(var item in applied)
                    sb.AppendLine(item);

                sb.AppendLine();
                sb.AppendLine("=== PENDING MIGRATIONS ===");

                foreach(var item in pending)
                    sb.AppendLine(item);

            }
            catch(Exception ex)
            {
                sb.AppendLine("Ошибка чтения истории миграций:");
                sb.AppendLine(ex.ToString());
            }

            return sb.ToString();
        }

        public async Task<int> GetTaypeDeviceIdAsync(string configType, CancellationToken ct = default)
        {
            await using var db = CreateDbContext();

            return await TryResolveTypeDeviceIdAsync(db, configType, ct)
                .ConfigureAwait(false)
                ?? throw new InvalidOperationException(
                    $"Для типа '{configType}' нет соответствия в taypeDevices.");
        }
    }
}