using AutoMapper;
using Business.DataService.Services;
using Core.Models.Dtos;
using Core.Models.Ipc;
using DataAccess;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System.Runtime.InteropServices;

namespace Business.Ipc.Handlers
{
    public class GetLookupMonitorHandler : IRequestHandler<GetLookupMonitorQuery, ResultDto<LookupResponseDto>>
    {
        // Win32 structures and P/Invoke
        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
        public struct DISPLAY_DEVICE
        {
            [MarshalAs(UnmanagedType.U4)]
            public int cb;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
            public string DeviceName;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
            public string DeviceString;
            [MarshalAs(UnmanagedType.U4)]
            public uint StateFlags;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
            public string DeviceID;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
            public string DeviceKey;
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
        public struct DEVMODE
        {
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
            public string dmDeviceName;
            public short dmSpecVersion;
            public short dmDriverVersion;
            public short dmSize;
            public short dmDriverExtra;
            public int dmFields;
            public int dmPositionX;     // important for multi-monitor position
            public int dmPositionY;
            public int dmDisplayOrientation;
            public int dmDisplayFixedOutput;
            public short dmColor;
            public short dmDuplex;
            public short dmYResolution;
            public short dmTTOption;
            public short dmCollate;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
            public string dmFormName;
            public short dmLogPixels;
            public int dmBitsPerPel;
            public int dmPelsWidth;
            public int dmPelsHeight;
            public int dmDisplayFlags;
            public int dmDisplayFrequency;
            // You can add more fields if needed later
        }

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        private static extern bool EnumDisplayDevices(string? lpDevice, uint iDevNum, ref DISPLAY_DEVICE lpDisplayDevice, uint dwFlags);

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        private static extern bool EnumDisplaySettings(string? lpszDeviceName, int iModeNum, ref DEVMODE lpDevMode);

        private const int ENUM_CURRENT_SETTINGS = -1;
        private const uint DISPLAY_DEVICE_ATTACHED_TO_DESKTOP = 0x00000001;
        private const uint DISPLAY_DEVICE_PRIMARY_DEVICE = 0x00000004;


        private readonly IMapper _mapper;
        private IDbContextFactory<AppDbContext> _dbContextFactory;

        public GetLookupMonitorHandler(IMapper mapper, IDataService dataService, IDbContextFactory<AppDbContext> dbContextFactory)
        {
            _mapper = mapper;
            _dbContextFactory = dbContextFactory;
        }

        public async Task<ResultDto<LookupResponseDto>> Handle(GetLookupMonitorQuery request, CancellationToken ct)
        {
            // LookupRequestDto dto = request.dto;   // you can use it for filtering later if needed

            var items = new List<LookupItemDto>();

            uint deviceIndex = 0;
            DISPLAY_DEVICE displayDevice = new DISPLAY_DEVICE { cb = Marshal.SizeOf(typeof(DISPLAY_DEVICE)) };

            while (EnumDisplayDevices(null, deviceIndex, ref displayDevice, 0))
            {
                // Only consider monitors attached to the desktop
                if ((displayDevice.StateFlags & DISPLAY_DEVICE_ATTACHED_TO_DESKTOP) != 0)
                {
                    DEVMODE devMode = new DEVMODE { dmSize = (short)Marshal.SizeOf(typeof(DEVMODE)) };

                    if (EnumDisplaySettings(displayDevice.DeviceName, ENUM_CURRENT_SETTINGS, ref devMode))
                    {
                        bool isPrimary = (displayDevice.StateFlags & DISPLAY_DEVICE_PRIMARY_DEVICE) != 0;

                        var label = isPrimary
                            ? $"Primary - {devMode.dmPelsWidth}×{devMode.dmPelsHeight}"
                            : displayDevice.DeviceName;

                        items.Add(new LookupItemDto
                        {
                            Value = deviceIndex.ToString(),           // or use a stable identifier if you prefer
                            Label = label,
                            Description = $"{devMode.dmPelsWidth}×{devMode.dmPelsHeight} @ ({devMode.dmPositionX}, {devMode.dmPositionY})",
                            ExtraData = new
                            {
                                Index = (int)deviceIndex,
                                DeviceName = displayDevice.DeviceName,
                                IsPrimary = isPrimary,
                                X = devMode.dmPositionX,
                                Y = devMode.dmPositionY,
                                Width = devMode.dmPelsWidth,
                                Height = devMode.dmPelsHeight
                            }
                        });
                    }
                }

                deviceIndex++;
            }

            var response = new LookupResponseDto { Data = items };

            return ResultDto<LookupResponseDto>.Success(response);
        }
    }
}
