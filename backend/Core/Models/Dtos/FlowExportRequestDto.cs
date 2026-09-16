namespace Core.Models.Dtos
{
    public class FlowExportRequestDto
    {
        public int FlowId { get; set; }

        /// <summary>Where to write. Empty means the app's own export folder.</summary>
        public string? FolderPath { get; set; }
    }
}
