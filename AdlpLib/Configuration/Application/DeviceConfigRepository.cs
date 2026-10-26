#nullable enable
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
using System.Threading;
using System.Threading.Tasks;

namespace AdlpLib.Configuration.Application
{
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
        private const string TypeExec = "EXEC"; // старый тип, поддерживаем для обратной совместимости
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
            await AdlpDatabaseInitializer.ApplyPendingMigrationsAsync(db, ct).ConfigureAwait(false);


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
                await db.Database
                    .ExecuteSqlRawAsync("SET @app_user = {0};", new object[] { userName }, ct)
                    .ConfigureAwait(false);
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
                DeviceEntity device;

                if(dto.Id == 0)
                {
                    device = new DeviceEntity();
                    db.Devices.Add(device);
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
            entity.ConfigType = configType;
            entity.DeviceType = dto.DeviceType ?? string.Empty;
            entity.SID = dto.SID;
            entity.Mac = dto.Mac ?? string.Empty;
            entity.Ip = dto.Ip ?? string.Empty;
            entity.Mask = dto.Mask ?? string.Empty;
            entity.Gateway = dto.Gateway ?? string.Empty;
            entity.Block = dto.Block;
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

        private static void CopyCommonToDto(DeviceEntity device, DeviceConfigDtoBase dto)
        {
            dto.Id = device.Id;
            dto.Name = device.Name;
            dto.SID = device.SID ?? 0;
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

        public async Task<long?> FindDeviceIdByMacAsync(
            string mac, string configType, CancellationToken ct = default)
        {
            if(string.IsNullOrWhiteSpace(mac))
                return null;

            await using var db = CreateDbContext();
            return await db.Devices.AsNoTracking()
                .Where(x => x.Mac == mac && x.ConfigType == configType)
                .OrderBy(x => x.Id)
                .Select(x => (long?)x.Id)
                .FirstOrDefaultAsync(ct)
                .ConfigureAwait(false);
        }
    }
}
