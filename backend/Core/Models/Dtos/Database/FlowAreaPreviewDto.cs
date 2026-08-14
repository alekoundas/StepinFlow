namespace Core.Models.Dtos
{
    /// <summary>
    /// What an area resolves to on this machine right now, with an optional screenshot of it.
    /// Powers the Preview button and, later, the import binding report.
    /// </summary>
    public class FlowAreaPreviewDto
    {
        public bool IsResolved { get; set; }
        public string? ErrorMessage { get; set; }

        public int LocationX { get; set; }
        public int LocationY { get; set; }
        public int Width { get; set; }
        public int Height { get; set; }

        /// <summary>JPEG bytes, arrives in the renderer as base64.</summary>
        public byte[]? Screenshot { get; set; }
    }
}
