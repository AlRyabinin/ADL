using System;
using System.Collections.Generic;

namespace AdlpLib.Persistence;

/// <summary>Provider-specific DDL preserving the current ADL schema.</summary>
internal static class AdlpSchemaSql
{
    public static IReadOnlyList<string> ForProvider(string provider) => provider switch
    {
        "Microsoft.EntityFrameworkCore.Sqlite" => Sqlite,
        "Pomelo.EntityFrameworkCore.MySql" => MySql,
        _ => throw new NotSupportedException($"Provider '{provider}' is not supported.")
    };

    private static readonly string[] Sqlite =
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
            FOREIGN KEY (DeviceId) REFERENCES Devices(Id) ON DELETE CASCADE);",
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
            );",
    };

    private static readonly string[] MySql =
    {
        @"CREATE TABLE IF NOT EXISTS _audit_context (
            `key` VARCHAR(255) NOT NULL PRIMARY KEY,
            `value` VARCHAR(255) NULL
        ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;",
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
        ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;",
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
            ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;",
    };
}
