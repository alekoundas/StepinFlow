namespace Core.Models.Dtos
{
    /// <summary>
    /// Where an export landed, so the app can say it and open the folder.
    /// </summary>
    public class FlowExportResultDto
    {
        public string ScriptPath { get; set; } = string.Empty;

        /// <summary>The folder holding the template images, beside the script.</summary>
        public string TemplateFolderPath { get; set; } = string.Empty;

        public int TemplateCount { get; set; }

        /// <summary>The script itself, so the app can show it without reading the file back.</summary>
        public string Script { get; set; } = string.Empty;
    }
}
