using System.Drawing;

using Core.Enums;
using Core.Ports;
using Core.Helpers;
using Core.Models.Business;
using Core.Models.Dtos;

namespace Transport.Ipc.Handlers
{
    public class SystemCropScreenshotsHandler
    {
        private const int BYTES_PER_PIXEL = 4;
        private const string NOTHING_SELECTED = "Nothing was selected to cut.";
        private const string OFF_THE_SCREENSHOTS = "The selection is not on any of the screenshots it was cut from.";

        private readonly IScreenshotService _screenshotService;

        public SystemCropScreenshotsHandler(IScreenshotService screenshotService)
        {
            _screenshotService = screenshotService;
        }

        /// <summary>
        /// The region as a PNG, put together from the screenshots it lies on. Whatever of it no
        /// screenshot covers is left transparent.
        /// </summary>
        public Task<ResultDto<byte[]>> HandleAsync(ScreenshotCropRequestDto dto, CancellationToken ct)
        {
            Rectangle region = new Rectangle(dto.Region.X, dto.Region.Y, dto.Region.Width, dto.Region.Height);
            if (region.Width <= 0 || region.Height <= 0)
                return Task.FromResult(ResultDto<byte[]>.Failure(NOTHING_SELECTED));

            RawImage cut = new RawImage
            {
                Width = region.Width,
                Height = region.Height,
                Stride = region.Width * BYTES_PER_PIXEL,
                Pixels = new byte[region.Width * region.Height * BYTES_PER_PIXEL],
            };

            bool isCovered = false;
            foreach (ScreenshotMonitorResponseDto screenshot in dto.Screenshots)
            {
                Rectangle bounds = new Rectangle(screenshot.X, screenshot.Y, screenshot.Width, screenshot.Height);
                if (!bounds.IntersectsWith(region))
                    continue;

                RawImage decoded = _screenshotService.Decode(screenshot.Screenshot);
                RawImageHelper.Paste(cut, region, decoded, bounds);
                isCovered = true;
            }

            if (!isCovered)
                return Task.FromResult(ResultDto<byte[]>.Failure(OFF_THE_SCREENSHOTS));

            byte[] png = _screenshotService.Encode(cut, ScreenshotFormatEnum.PNG, 100);
            return Task.FromResult(ResultDto<byte[]>.Success(png));
        }
    }
}
