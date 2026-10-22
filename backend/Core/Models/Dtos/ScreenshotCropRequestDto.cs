namespace Core.Models.Dtos
{
    /// <summary>
    /// A region to cut from the screenshots of the monitors it lies on, as the overlay was given
    /// them. All in physical pixels.
    /// </summary>
    public class ScreenshotCropRequestDto
    {
        public ScreenRectangleDto Region { get; set; } = new ScreenRectangleDto();
        public List<ScreenshotMonitorResponseDto> Screenshots { get; set; } = new List<ScreenshotMonitorResponseDto>();
    }
}
