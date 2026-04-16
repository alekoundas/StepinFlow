namespace Core.Models.Dtos
{
    public class ScreenshotRequestDto
    {
        public int? FlowSearchAreaId { get; set; }
        public bool IsVirtualScreen { get; set; }
        public bool IsJPEG { get; set; }
        public int JpegQuality { get; set; }
        public int LocationX { get; set; }
        public int LocationY { get; set; }
        public int Width { get; set; }
        public int Height { get; set; }
    }
}
