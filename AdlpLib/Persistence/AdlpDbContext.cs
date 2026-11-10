using AdlpLib.Persistence.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AdlpLib.Persistence;

/// <summary>EF Core context for the ADL database.</summary>
public sealed class AdlpDbContext : DbContext
{
    public AdlpDbContext(DbContextOptions<AdlpDbContext> options) : base(options) { }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder.LogTo(
            message => System.Diagnostics.Debug.WriteLine(message),
            LogLevel.Information);
    }

    public DbSet<DeviceEntity> Devices => Set<DeviceEntity>();
    public DbSet<ListSidEntity> ListSids => Set<ListSidEntity>();
    public DbSet<ObjectEntity> Objects => Set<ObjectEntity>();
    public DbSet<ListUuidEntity> ListUuids => Set<ListUuidEntity>();
    public DbSet<TaypeDeviceEntity> TaypeDevices => Set<TaypeDeviceEntity>();
    public DbSet<DeviceMulticastAddressEntity> DeviceMulticastAddresses => Set<DeviceMulticastAddressEntity>();
    public DbSet<DeviceMulticastSubscriptionEntity> DeviceMulticastSubscriptions => Set<DeviceMulticastSubscriptionEntity>();
    public DbSet<PuDeviceEntity> PuDevices => Set<PuDeviceEntity>();
    public DbSet<PuChannelEntity> PuChannels => Set<PuChannelEntity>();
    public DbSet<PuDeviceChannelEntity> PuDeviceChannels => Set<PuDeviceChannelEntity>();
    public DbSet<PuGroupEntity> PuGroups => Set<PuGroupEntity>();
    public DbSet<PuDeviceGroupEntity> PuDeviceGroups => Set<PuDeviceGroupEntity>();
    public DbSet<PuGroupChannelEntity> PuGroupChannels => Set<PuGroupChannelEntity>();
    public DbSet<PuPictureEntity> PuPictures => Set<PuPictureEntity>();
    public DbSet<PuDevicePictureEntity> PuDevicePictures => Set<PuDevicePictureEntity>();
    public DbSet<PuButtonEntity> PuButtons => Set<PuButtonEntity>();
    public DbSet<PuDeviceButtonEntity> PuDeviceButtons => Set<PuDeviceButtonEntity>();
    public DbSet<PuButtonChannelEntity> PuButtonChannels => Set<PuButtonChannelEntity>();
    public DbSet<ExecDeviceEntity> ExecDevices => Set<ExecDeviceEntity>();
    public DbSet<ExecStreamEntity> ExecStreams => Set<ExecStreamEntity>();
    public DbSet<ExecDeviceStreamEntity> ExecDeviceStreams => Set<ExecDeviceStreamEntity>();
    public DbSet<ExecChannelEntity> ExecChannels => Set<ExecChannelEntity>();
    public DbSet<ExecDeviceChannelEntity> ExecDeviceChannels => Set<ExecDeviceChannelEntity>();
    public DbSet<ExecChannelStreamEntity> ExecChannelStreams => Set<ExecChannelStreamEntity>();
    public DbSet<AuditContextEntity> AuditContext => Set<AuditContextEntity>();
    public DbSet<NetGateDeviceEntity> NetGateDevices => Set<NetGateDeviceEntity>();
    public DbSet<NetGateConveyorEntity> NetGateConveyors => Set<NetGateConveyorEntity>();
    public DbSet<NetGateChannelEntity> NetGateChannels => Set<NetGateChannelEntity>();
    public DbSet<NetGatePictureEntity> NetGatePictures => Set<NetGatePictureEntity>();
    public DbSet<DeviceGroupEntity> DeviceGroups => Set<DeviceGroupEntity>();
    public DbSet<DeviceGroupDeviceEntity> DeviceGroupDevices => Set<DeviceGroupDeviceEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        ConfigureModel(modelBuilder);
        base.OnModelCreating(modelBuilder);
    }

    internal static void ConfigureModel(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ListSidEntity>(entity =>
        {
            entity.ToTable("listSID", table => table.ExcludeFromMigrations());

            entity.HasKey(x => x.PkListSid);

            entity.Property(x => x.PkListSid)
                .HasColumnName("PKlistSID")
                .HasColumnType("char(8)")
                .HasMaxLength(8)
                .IsFixedLength()
                .IsRequired();

            entity.Property(x => x.NameObjSid)
                .HasColumnName("NameObjSID")
                .HasMaxLength(64)
                .IsRequired();

            entity.Property(x => x.IdTxtDCon)
                .HasColumnName("idtxtDCon")
                .IsRequired();

            entity.Property(x => x.Comment)
                .HasColumnName("comment")
                .HasMaxLength(255)
                .IsRequired(false);

            entity.Property(x => x.RegWebUser)
                .HasColumnName("regWebUser")
                .HasMaxLength(50)
                .IsRequired();

            entity.Property(x => x.DateRec)
                .HasColumnName("DateRec")
                .IsRequired(false);

            entity.Property(x => x.DateRecOn)
                .HasColumnName("DateRecOn")
                .IsRequired()
                .ValueGeneratedOnAddOrUpdate();

            entity.Property(x => x.ObjectId).HasColumnName("ObjectId").IsRequired(false);
            entity.HasOne(x => x.ObjectRecord).WithMany()
                  .HasForeignKey(x => x.ObjectId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<ObjectEntity>(entity =>
        {
            entity.ToTable("Object", table => table.ExcludeFromMigrations());
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Id).ValueGeneratedOnAdd();
            entity.Property(x => x.Name).HasColumnName("name").HasMaxLength(255).IsRequired();
            entity.Property(x => x.RegWebUser).HasColumnName("regWebUser").HasMaxLength(50)
                  .ValueGeneratedOnAddOrUpdate();
            entity.Property(x => x.DateRec).HasColumnName("DateRec").ValueGeneratedOnAdd();
            entity.Property(x => x.DateRecOn).HasColumnName("DateRecOn").ValueGeneratedOnAddOrUpdate();
        });

        modelBuilder.Entity<ListUuidEntity>(entity =>
        {
            entity.ToTable("listUUID", table => table.ExcludeFromMigrations());

            entity.HasKey(x => x.PkListUuid);

            entity.Property(x => x.PkListUuid)
                .HasColumnName("PKlistUUID")
                .HasColumnType("char(32)")
                .HasMaxLength(32)
                .IsFixedLength()
                .UseCollation("utf8_general_ci")
                .IsRequired();

            entity.Property(x => x.Sn)
                .HasColumnName("SN")
                .HasMaxLength(32)
                .IsRequired(false);

            entity.Property(x => x.FkListSid)
                .HasColumnName("FKlistSID")
                .HasColumnType("char(8)")
                .HasMaxLength(8)
                .IsFixedLength()
                .IsRequired();

            entity.Property(x => x.FkTypeDevices)
                .HasColumnName("FKtaypeDevices")
                .IsRequired();

            entity.Property(x => x.NumBlock)
                .HasColumnName("NumBlock")
                .IsRequired(false);

            entity.Property(x => x.IdTxtDCon)
                .HasColumnName("idtxtDCon")
                .IsRequired();

            entity.Property(x => x.Mac)
                .HasColumnName("MAC")
                .HasColumnType("char(24)")
                .HasMaxLength(24)
                .IsFixedLength()
                .IsRequired(false);

            entity.Property(x => x.Ip)
                .HasColumnName("IP")
                .HasColumnType("char(20)")
                .HasMaxLength(20)
                .IsFixedLength()
                .IsRequired(false);

            entity.Property(x => x.Port)
                .HasColumnName("port")
                .IsRequired(false);

            entity.Property(x => x.Comment)
                .HasColumnName("comment")
                .HasMaxLength(255)
                .IsRequired(false);

            entity.Property(x => x.RegWebUser)
                .HasColumnName("regWebUser")
                .HasMaxLength(50)
                .IsRequired();

            entity.Property(x => x.DateRec)
                .HasColumnName("DateRec")
                .IsRequired(false);

            entity.Property(x => x.DateRecOn)
                .HasColumnName("DateRecOn")
                .ValueGeneratedOnAddOrUpdate()
                .IsRequired();
        });

        modelBuilder.Entity<DeviceEntity>(entity =>
        {
            entity.ToTable("Devices");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).ValueGeneratedOnAdd();
            entity.Property(e => e.Name).IsRequired();
            entity.Property(e => e.Name).HasMaxLength(255);
            entity.Property(e => e.Name).HasDefaultValue("");
            entity.Property(e => e.ConfigType).IsRequired();
            entity.Property(e => e.ConfigType).HasMaxLength(10);
            entity.Property(e => e.DeviceType).IsRequired();
            entity.Property(e => e.DeviceType).HasMaxLength(100);
            entity.Property(e => e.DeviceType).HasDefaultValue("");
            entity.Property(e => e.Mac).IsRequired();
            entity.Property(e => e.Mac).HasMaxLength(32);
            entity.Property(e => e.Mac).HasDefaultValue("");
            entity.Property(e => e.Ip).IsRequired();
            entity.Property(e => e.Ip).HasMaxLength(64);
            entity.Property(e => e.Ip).HasDefaultValue("");
            entity.Property(e => e.Mask).IsRequired();
            entity.Property(e => e.Mask).HasMaxLength(64);
            entity.Property(e => e.Mask).HasDefaultValue("");
            entity.Property(e => e.Gateway).IsRequired();
            entity.Property(e => e.Gateway).HasMaxLength(64);
            entity.Property(e => e.Gateway).HasDefaultValue("");
            entity.Property(e => e.Block).HasDefaultValue(0);
            entity.Property(e => e.CreatedBy).HasMaxLength(100);
            entity.Property(e => e.UpdatedBy).HasMaxLength(100);
            entity.HasIndex(e => new { e.PkListUuid, e.Block }).IsUnique();
            entity.Property(x => x.PkListUuid).HasColumnName("PKlistUUID").HasColumnType("char(32)").HasMaxLength(32).IsFixedLength().UseCollation("utf8_general_ci").IsRequired(false);
            entity.HasOne(x => x.UuidRecord).WithMany().HasForeignKey(x => x.PkListUuid).OnDelete(DeleteBehavior.Restrict);
            entity.Property(x => x.PkListSid).HasColumnName("PKlistSID").HasColumnType("char(8)").HasMaxLength(8).IsFixedLength();
            entity.HasOne(x => x.SidRecord).WithMany().HasForeignKey(x => x.PkListSid).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<TaypeDeviceEntity>(entity =>
        {
            entity.ToTable("taypeDevices",
                table => table.ExcludeFromMigrations());

            entity.HasKey(x => x.PkTaypeDevices);

            entity.Property(x => x.PkTaypeDevices)
                .HasColumnName("PKtaypeDevices")
                .ValueGeneratedOnAdd();

            entity.Property(x => x.Name)
                .HasColumnName("Name")
                .HasMaxLength(255)
                .IsRequired();

            entity.Property(x => x.IdTxtDConTxtName)
                .HasColumnName("idtxtDCon_txt_name")
                .IsRequired();

            entity.Property(x => x.Brend)
                .HasColumnName("Brend")
                .HasColumnType("enum('etc','ADL')")
                .HasDefaultValue("ADL");

            entity.Property(x => x.TypeCodeUnit)
                .HasColumnName("typeCodeUnit")
                .HasColumnType("char(2)")
                .HasMaxLength(2)
                .IsFixedLength()
                .IsRequired();

            entity.Property(x => x.IdTxtDConTxtNote)
                .HasColumnName("idtxtDCon_txt_note");

            entity.Property(x => x.RegWebUser)
                .HasColumnName("regWebUser")
                .HasMaxLength(50)
                .IsRequired();

            entity.Property(x => x.DateRec)
                .HasColumnName("DateRec");

            entity.Property(x => x.DateRecOn)
                .HasColumnName("DateRecOn")
                .HasColumnType("timestamp")
                .ValueGeneratedOnAddOrUpdate();
        });

        modelBuilder.Entity<DeviceMulticastAddressEntity>(entity =>
        {
            entity.ToTable("DeviceMulticastAddresses");

            entity.HasKey(x => x.DeviceId);

            entity.Property(x => x.MultiCastAdress)
                .HasMaxLength(15)
                .IsRequired();

            entity.HasIndex(x => x.MultiCastAdress)
                .IsUnique();

            entity.HasOne<DeviceEntity>()
                .WithOne()
                .HasForeignKey<DeviceMulticastAddressEntity>(x => x.DeviceId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<DeviceMulticastSubscriptionEntity>(entity =>
        {
            entity.ToTable("DeviceMulticastSubscriptions");

            entity.HasKey(x => new { x.DeviceId, x.MulticastAddress });

            entity.Property(x => x.MulticastAddress)
                .HasMaxLength(15)
                .IsRequired();      

            entity.HasOne<DeviceEntity>()
                .WithMany()
                .HasForeignKey(x => x.DeviceId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<PuDeviceEntity>(entity =>
        {
            entity.ToTable("PuDevices");
            entity.HasKey(e => e.DeviceId);
            entity.Property(e => e.DeviceId).ValueGeneratedNever();
            entity.Property(e => e.Priority).HasDefaultValue(5);
            entity.Property(e => e.PictureSwitchTime).HasDefaultValue(0);
            entity.Property(e => e.PageCount).HasDefaultValue(1);
            entity.Property(e => e.SwapX).HasDefaultValue(0);
            entity.Property(e => e.SwapY).HasDefaultValue(0);
            entity.Property(e => e.Brightness).HasDefaultValue(100);
            entity.Property(e => e.BrightnessSleep).HasDefaultValue(100);
            entity.Property(e => e.SleepTimeout).HasDefaultValue(0);
            entity.HasOne<DeviceEntity>().WithMany().HasForeignKey(e => e.DeviceId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<PuChannelEntity>(entity =>
        {
            entity.ToTable("PuChannels");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).ValueGeneratedOnAdd();
            entity.Property(e => e.Name).IsRequired();
            entity.Property(e => e.Name).HasMaxLength(255);
            entity.Property(e => e.Name).HasDefaultValue("");
            entity.Property(e => e.AdlpIndex).HasDefaultValue(0);
            entity.Property(e => e.Type).HasDefaultValue(1);
        });

        modelBuilder.Entity<PuDeviceChannelEntity>(entity =>
        {
            entity.ToTable("PuDeviceChannels");
            entity.HasKey(e => new { e.DeviceId, e.ChannelId });
            entity.Property(e => e.Position).HasDefaultValue(0);
            entity.HasOne<DeviceEntity>().WithMany().HasForeignKey(e => e.DeviceId).OnDelete(DeleteBehavior.Cascade);
            entity.HasOne<PuChannelEntity>().WithMany().HasForeignKey(e => e.ChannelId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<PuGroupEntity>(entity =>
        {
            entity.ToTable("PuGroups");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).ValueGeneratedOnAdd();
            entity.Property(e => e.AdlpIndex).HasDefaultValue(0);
        });

        modelBuilder.Entity<PuDeviceGroupEntity>(entity =>
        {
            entity.ToTable("PuDeviceGroups");
            entity.HasKey(e => new { e.DeviceId, e.GroupId });
            entity.Property(e => e.Position).HasDefaultValue(0);
            entity.HasOne<DeviceEntity>().WithMany().HasForeignKey(e => e.DeviceId).OnDelete(DeleteBehavior.Cascade);
            entity.HasOne<PuGroupEntity>().WithMany().HasForeignKey(e => e.GroupId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<PuGroupChannelEntity>(entity =>
        {
            entity.ToTable("PuGroupChannels");
            entity.HasKey(e => new { e.GroupId, e.Slot });
            entity.HasOne<PuGroupEntity>().WithMany().HasForeignKey(e => e.GroupId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<PuPictureEntity>(entity =>
        {
            entity.ToTable("PuPictures");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).ValueGeneratedOnAdd();
            entity.Property(e => e.Name).IsRequired();
            entity.Property(e => e.Name).HasMaxLength(255);
            entity.Property(e => e.Name).HasDefaultValue("");
        });

        modelBuilder.Entity<PuDevicePictureEntity>(entity =>
        {
            entity.ToTable("PuDevicePictures");
            entity.HasKey(e => new { e.DeviceId, e.PictureId });
            entity.Property(e => e.Position).HasDefaultValue(0);
            entity.HasOne<DeviceEntity>().WithMany().HasForeignKey(e => e.DeviceId).OnDelete(DeleteBehavior.Cascade);
            entity.HasOne<PuPictureEntity>().WithMany().HasForeignKey(e => e.PictureId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<PuButtonEntity>(entity =>
        {
            entity.ToTable("PuButtons");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).ValueGeneratedOnAdd();
            entity.Property(e => e.Page).HasDefaultValue(1);
            entity.Property(e => e.LocalChannel).HasDefaultValue(0);
            entity.Property(e => e.PosX).HasDefaultValue(0);
            entity.Property(e => e.PosY).HasDefaultValue(0);
            entity.Property(e => e.Icon).HasDefaultValue(0);
            entity.Property(e => e.Color).HasDefaultValue(0);
        });

        modelBuilder.Entity<PuDeviceButtonEntity>(entity =>
        {
            entity.ToTable("PuDeviceButtons");
            entity.HasKey(e => new { e.DeviceId, e.ButtonId });
            entity.Property(e => e.Position).HasDefaultValue(0);
            entity.HasOne<DeviceEntity>().WithMany().HasForeignKey(e => e.DeviceId).OnDelete(DeleteBehavior.Cascade);
            entity.HasOne<PuButtonEntity>().WithMany().HasForeignKey(e => e.ButtonId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<PuButtonChannelEntity>(entity =>
        {
            entity.ToTable("PuButtonChannels");
            entity.HasKey(e => new { e.DeviceId, e.ButtonId });
            entity.HasOne<PuDeviceButtonEntity>().WithMany().HasForeignKey(e => new { e.DeviceId, e.ButtonId }).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<ExecDeviceEntity>(entity =>
        {
            entity.ToTable("ExecDevices");
            entity.HasKey(e => e.DeviceId);
            entity.Property(e => e.DeviceId).ValueGeneratedNever();
            entity.Property(e => e.BaseChannel).IsRequired();
            entity.Property(e => e.BaseChannel).HasMaxLength(255);
            entity.Property(e => e.BaseChannel).HasDefaultValue("");
            entity.HasOne<DeviceEntity>().WithMany().HasForeignKey(e => e.DeviceId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<ExecStreamEntity>(entity =>
        {
            entity.ToTable("ExecStreams");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).ValueGeneratedOnAdd();
            entity.Property(e => e.Protocol).IsRequired();
            entity.Property(e => e.Protocol).HasMaxLength(50);
            entity.Property(e => e.Protocol).HasDefaultValue("нет");
            entity.Property(e => e.Universe).HasDefaultValue(0);
            entity.Property(e => e.Address).HasDefaultValue(1);
        });

        modelBuilder.Entity<ExecDeviceStreamEntity>(entity =>
        {
            entity.ToTable("ExecDeviceStreams");
            entity.HasKey(e => new { e.DeviceId, e.StreamId });
            entity.Property(e => e.Position).HasDefaultValue(0);
            entity.HasOne<DeviceEntity>().WithMany().HasForeignKey(e => e.DeviceId).OnDelete(DeleteBehavior.Cascade);
            entity.HasOne<ExecStreamEntity>().WithMany().HasForeignKey(e => e.StreamId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<ExecChannelEntity>(entity =>
        {
            entity.ToTable("ExecChannels");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).ValueGeneratedOnAdd();
            entity.Property(e => e.GlobalIndex).HasDefaultValue(0);
            entity.Property(e => e.Name).IsRequired();
            entity.Property(e => e.Name).HasMaxLength(255);
            entity.Property(e => e.Name).HasDefaultValue("");
            entity.Property(e => e.Control).IsRequired();
            entity.Property(e => e.Control).HasMaxLength(20);
            entity.Property(e => e.Control).HasDefaultValue("adlp");
            entity.Property(e => e.Wide).HasDefaultValue(false);
            entity.Property(e => e.Patch).HasDefaultValue(0);
            entity.Property(e => e.Preheat).HasDefaultValue(0);
            entity.Property(e => e.LimitValue).HasDefaultValue(100);
            entity.Property(e => e.Curve).HasDefaultValue(0);
            entity.Property(e => e.SwitchDelay).HasDefaultValue(0);
        });

        modelBuilder.Entity<ExecDeviceChannelEntity>(entity =>
        {
            entity.ToTable("ExecDeviceChannels");
            entity.HasKey(e => new { e.DeviceId, e.ChannelId });
            entity.Property(e => e.Position).HasDefaultValue(0);
            entity.HasOne<DeviceEntity>().WithMany().HasForeignKey(e => e.DeviceId).OnDelete(DeleteBehavior.Cascade);
            entity.HasOne<ExecChannelEntity>().WithMany().HasForeignKey(e => e.ChannelId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<ExecChannelStreamEntity>(entity =>
        {
            entity.ToTable("ExecChannelStreams");
            entity.HasKey(e => new { e.DeviceId, e.ChannelId });
            entity.HasOne<ExecDeviceChannelEntity>().WithMany().HasForeignKey(e => new { e.DeviceId, e.ChannelId }).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<AuditContextEntity>(entity =>
        {
            entity.ToTable("_audit_context");
            entity.HasKey(e => e.Key);
            entity.Property(e => e.Key).ValueGeneratedNever();
            entity.Property(e => e.Key).HasColumnName("key");
            entity.Property(e => e.Key).IsRequired();
            entity.Property(e => e.Key).HasMaxLength(255);
            entity.Property(e => e.Value).HasColumnName("value");
            entity.Property(e => e.Value).IsRequired();
            entity.Property(e => e.Value).HasMaxLength(255);
        });

        modelBuilder.Entity<NetGateDeviceEntity>(entity =>
        {
            entity.ToTable("NetGateDevices");
            entity.HasKey(e => e.DeviceId);
            entity.Property(e => e.DeviceId).ValueGeneratedNever();
            entity.Property(e => e.ConveyorCount).HasDefaultValue(0);
            entity.HasOne<DeviceEntity>().WithMany().HasForeignKey(e => e.DeviceId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<NetGateConveyorEntity>(entity =>
        {
            entity.ToTable("NetGateConveyors");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).ValueGeneratedOnAdd();
            entity.Property(e => e.SourceAProtocol).IsRequired();
            entity.Property(e => e.SourceAProtocol).HasMaxLength(50);
            entity.Property(e => e.SourceAProtocol).HasDefaultValue("нет");
            entity.Property(e => e.SourceAUniverse).HasDefaultValue(0);
            entity.Property(e => e.SourceBProtocol).IsRequired();
            entity.Property(e => e.SourceBProtocol).HasMaxLength(50);
            entity.Property(e => e.SourceBProtocol).HasDefaultValue("нет");
            entity.Property(e => e.SourceBUniverse).HasDefaultValue(0);
            entity.Property(e => e.SourceCProtocol).IsRequired();
            entity.Property(e => e.SourceCProtocol).HasMaxLength(50);
            entity.Property(e => e.SourceCProtocol).HasDefaultValue("нет");
            entity.Property(e => e.SourceCUniverse).HasDefaultValue(0);
            entity.Property(e => e.DestinationProtocol).IsRequired();
            entity.Property(e => e.DestinationProtocol).HasMaxLength(50);
            entity.Property(e => e.DestinationProtocol).HasDefaultValue("нет");
            entity.Property(e => e.DestinationUniverse).HasDefaultValue(0);
            entity.Property(e => e.SourceSelect).HasDefaultValue(0);
            entity.Property(e => e.SumType).HasDefaultValue(0);
            entity.Property(e => e.Frequency).HasDefaultValue(0);
            entity.Property(e => e.Recordable).HasDefaultValue(false);
            entity.Property(e => e.Timeouted).HasDefaultValue(false);
            entity.Property(e => e.RdmEnabled).HasDefaultValue(false);
            entity.Property(e => e.SumBFrom).HasDefaultValue(0);
            entity.Property(e => e.SumBTo).HasDefaultValue(255);
            entity.Property(e => e.SumCFrom).HasDefaultValue(256);
            entity.Property(e => e.SumCTo).HasDefaultValue(511);
            entity.HasOne<DeviceEntity>().WithMany().HasForeignKey(e => e.DeviceId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<NetGateChannelEntity>(entity =>
        {
            entity.ToTable("NetGateChannels");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).ValueGeneratedOnAdd();
            entity.Property(e => e.Name).IsRequired();
            entity.Property(e => e.Name).HasMaxLength(255);
            entity.Property(e => e.Name).HasDefaultValue("");
            entity.Property(e => e.AdlpIndex).HasDefaultValue(0);
            entity.HasOne<DeviceEntity>().WithMany().HasForeignKey(e => e.DeviceId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<NetGatePictureEntity>(entity =>
        {
            entity.ToTable("NetGatePictures");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).ValueGeneratedOnAdd();
            entity.Property(e => e.Picture).HasDefaultValue(0);
            entity.Property(e => e.FadeIn).HasDefaultValue(0);
            entity.Property(e => e.FadeOut).HasDefaultValue(0);
            entity.Property(e => e.Mux1).HasDefaultValue(0);
            entity.Property(e => e.Mux2).HasDefaultValue(0);
            entity.Property(e => e.Mux3).HasDefaultValue(0);
            entity.Property(e => e.Mux4).HasDefaultValue(0);
            entity.HasOne<DeviceEntity>().WithMany().HasForeignKey(e => e.DeviceId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<DeviceGroupEntity>(entity =>
        {
            entity.ToTable("DeviceGroups");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).ValueGeneratedOnAdd();
            entity.Property(e => e.Name).IsRequired();
            entity.Property(e => e.Name).HasMaxLength(255);
            entity.Property(e => e.Name).HasDefaultValue("");
            entity.Property(e => e.CreatedBy).HasMaxLength(100);
            entity.Property(e => e.UpdatedBy).HasMaxLength(100);
        });

        modelBuilder.Entity<DeviceGroupDeviceEntity>(entity =>
        {
            entity.ToTable("DeviceGroupDevices");
            entity.HasKey(e => new { e.GroupId, e.DeviceId });
            entity.Property(e => e.Position).HasDefaultValue(0);
            entity.HasOne<DeviceGroupEntity>().WithMany().HasForeignKey(e => e.GroupId).OnDelete(DeleteBehavior.Cascade);
            entity.HasOne<DeviceEntity>().WithMany().HasForeignKey(e => e.DeviceId).OnDelete(DeleteBehavior.Cascade);
        });
    }
}
