using AdlpLib.Persistence;
using AdlpLib.Persistence.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace AdlpLib.Persistence.Migrations;

[DbContext(typeof(AdlpDbContext))]
[Migration("20261020000000_InitialCreate")]
public sealed class InitialCreate : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        foreach (var sql in AdlpSchemaSql.ForProvider(ActiveProvider))
            migrationBuilder.Sql(sql);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        var tables = new[]
        {
            "DeviceGroupDevices",
            "DeviceGroups",
            "NetGatePictures",
            "NetGateChannels",
            "NetGateConveyors",
            "NetGateDevices",
            "_audit_context",
            "ExecChannelStreams",
            "ExecDeviceChannels",
            "ExecChannels",
            "ExecDeviceStreams",
            "ExecStreams",
            "ExecDevices",
            "PuButtonChannels",
            "PuDeviceButtons",
            "PuButtons",
            "PuDevicePictures",
            "PuPictures",
            "PuGroupChannels",
            "PuDeviceGroups",
            "PuGroups",
            "PuDeviceChannels",
            "PuChannels",
            "PuDevices",
            "Devices",
        };
        foreach (var table in tables)
        {
            var name = ActiveProvider == "Microsoft.EntityFrameworkCore.Sqlite"
                ? table : $"`{table}`";
            migrationBuilder.Sql($"DROP TABLE IF EXISTS {name};");
        }
    }

    protected override void BuildTargetModel(ModelBuilder modelBuilder)
    {
        modelBuilder.HasAnnotation("ProductVersion", "8.0.32");
        modelBuilder.HasAnnotation("Relational:MaxIdentifierLength", 64);
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
