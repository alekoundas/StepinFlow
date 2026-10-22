using Core.Models.Business;
using Core.Models.Dtos;
using Transport.Ipc.Handlers;
using Transport.Tests.Fakes;

namespace Transport.Tests.Overlay
{
    /// <summary>
    /// A template is cut from the screenshots the overlay showed, never grabbed from the screen
    /// afterwards, where the overlay can still be fading out.
    /// </summary>
    public sealed class SystemCropScreenshotsHandlerTests
    {
        private const int WIDTH = 4;
        private const int HEIGHT = 2;

        private readonly FakeScreenshotService _screenshots = new FakeScreenshotService();

        private static CancellationToken Ct
        {
            get { return TestContext.Current.CancellationToken; }
        }

        // Every byte of a pixel holds first plus its index, which makes a cut readable.
        private ScreenshotMonitorResponseDto Monitor(int x, byte first)
        {
            byte[] pixels = new byte[WIDTH * HEIGHT * 4];
            for (int index = 0; index < WIDTH * HEIGHT; index++)
                Array.Fill(pixels, (byte)(first + index), index * 4, 4);

            byte[] png = [first];
            _screenshots.Decoded[png] = new RawImage { Width = WIDTH, Height = HEIGHT, Stride = WIDTH * 4, Pixels = pixels };

            return new ScreenshotMonitorResponseDto { Screenshot = png, X = x, Y = 0, Width = WIDTH, Height = HEIGHT };
        }

        private Task<ResultDto<byte[]>> Crop(int x, int width, params ScreenshotMonitorResponseDto[] monitors)
        {
            ScreenshotCropRequestDto request = new ScreenshotCropRequestDto
            {
                Region = new ScreenRectangleDto { X = x, Y = 1, Width = width, Height = 1 },
                Screenshots = monitors.ToList(),
            };

            return new SystemCropScreenshotsHandler(_screenshots).HandleAsync(request, Ct);
        }

        // A monitor left of the primary, and a selection across the seam between them.
        [Fact]
        public async Task A_selection_across_two_monitors_takes_each_part_from_its_own_screenshot()
        {
            ResultDto<byte[]> result = await Crop(-1, 2, Monitor(-WIDTH, 0), Monitor(0, 100));

            result.IsSuccess.ShouldBeTrue();
            result.Data.ShouldBe(new byte[] { 7, 7, 7, 7, 104, 104, 104, 104 });
        }

        [Fact]
        public async Task A_selection_off_every_screenshot_is_refused()
        {
            ResultDto<byte[]> result = await Crop(50, 2, Monitor(0, 100));

            result.IsSuccess.ShouldBeFalse();
        }
    }
}
