namespace Core.Models.Dtos.FlowScript
{
    public class FlowScriptExportRequestDto
    {
        public int FlowId { get; set; }

        /// <summary>Where to write. Empty means the app's own export folder.</summary>
        public string? FolderPath { get; set; }
    }
}
