using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AdlpLib.Core.Packet.Config
{
    public enum ParamCode
    {
        CoreVersion             = 0,    // STR [RO]

        DeviceName              = 1,    // STR
        DeviceBlock             = 2,    // U16
        DeviceNetIP             = 3,    // U32
        DeviceNetMask           = 4,    // U32
        DeviceNetGateway        = 5,    // U32
        DeviceNetMAC            = 6,    // RAW[6]
        DeviceConfiguration     = 7,    // U8
        DeviceUUID              = 8,    // RAW[16]
        DeviceFirmware          = 9,    // U8

        ChannelName             = 10,   // STR
        ChannelIndex            = 11,   // U16
        ChannelControl          = 12,   // U8
        ChannelStreamId         = 13,   // U32
        ChannelPatch            = 14,   // U16
        ChannelWide             = 15,   // U8
        ChannelType             = 16,   // U8
        ChannelCurve            = 17,   // U8
        ChannelPreheat          = 18,   // U8
        ChannelLimit            = 19,   // U8
        ChannelSwitchDelay      = 20,   // U32

        DevicePriority          = 30,   // U8
        DeviceStartChannel      = 31,   // U16
        DevicePictureSwitchTime = 32,   // U16
        DevicePictureStream     = 33,   // U32
        DevicePictureFrameTime  = 34,   // U16
        DeviceSystemId          = 35,   // U32
        DeviceTimeRequest       = 36,   // U8
        DeviceLocation          = 37,   // STR

        GroupIndex              = 50,   // U16

        StreamId                = 60,   // U32
        StreamStart             = 61,   // U16

        DeviceArtnetNet2Enabled  = 80,  // U8
        DeviceArtnetNet10Enabled = 81,  // U8
        DeviceArtnetNet2Address  = 82,  // U32
        DeviceArtnetNet2Netmask  = 83,  // U32
        DeviceArtnetNet10Address = 84,  // U32
        DeviceArtnetNet10Netmask = 85,  // U32

        PictureName             = 90,   // STR[32]
        PictureTriggerPresetWrite = 91, // U8
        PictureFadeIn           = 92,   // U8
        PictureFadeOut          = 93,   // U8
        PicturePreset           = 94,   // U8
        PictureMux1             = 95,   // U8
        PictureMux2             = 96,   // U8
        PictureMux3             = 97,   // U8
        PictureMux4             = 98,   // U8

        ControlButtonPage       = 100,  // U8
        ControlButtonLocal      = 101,  // U16
        ControlButtonPosX       = 102,  // U16
        ControlButtonPosY       = 103,  // U16
        ControlButtonIcon       = 104,  // U8
        ControlButtonColor      = 105,  // U8

        DevicePageCount         = 120,  // U8
        DeviceTouchscreenSwapX  = 121,  // U8
        DeviceTouchscreenSwapY  = 122,  // U8
        DeviceBrightness        = 123,  // U8
        DeviceBrightnessSleep   = 124,  // U8
        DeviceSleepTimeout      = 125,  // U16
        DeviceAcnVersion        = 126,  // U8
        DeviceTargetTemperature = 127,  // U8
        DeviceStatusTransport = 128,          // U8: 0 — broadcast, 1 — multicast
        DeviceStatusMulticastAddress = 129,   // U32: адрес группы статусов

        GateConveyorRecordable  = 150,  // U8
        GateConveyorSourceA     = 151,  // U32
        GateConveyorSourceB     = 152,  // U32
        GateConveyorSourceC     = 153,  // U32
        GateConveyorDestination = 154,  // U32
        GateConveyorSourceSelect = 155, // U8
        GateConveyorSumType     = 156,  // U8
        GateConveyorSumBFrom    = 157,  // U16
        GateConveyorSumBTo      = 158,  // U16
        GateConveyorSumCFrom    = 159,  // U16
        GateConveyorSumCTo      = 160,  // U16

        PixelgateLineName       = 180,  // STR[6]

        IgmpAddress = 200,                      // U32: адрес IGMP-подписки

        GroupChannelStart       = 1000, // U16
        GroupChannelEnd         = 1100, // U16
    }
}
