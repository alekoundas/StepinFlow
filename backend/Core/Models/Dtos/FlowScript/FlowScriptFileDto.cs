namespace Core.Models.Dtos.FlowScript
{
    /// <summary>A .sflw file in a folder, and how many templates sit in the folder beside it.</summary>
    public class FlowScriptFileDto
    {
        public string Path { get; set; } = string.Empty;
        public string FileName { get; set; } = string.Empty;
        public DateTime ModifiedAt { get; set; }
        public int TemplateCount { get; set; }
    }
}
