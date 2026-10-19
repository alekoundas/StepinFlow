namespace Core.Models.Dtos.FlowScript
{
    /// <summary>
    /// The app's export folder, where an export goes unless told otherwise and an import's file
    /// dialog opens, with the scripts already in it, newest first.
    /// </summary>
    public class FlowScriptExportFolderDto
    {
        public string FolderPath { get; set; } = string.Empty;
        public List<FlowScriptFileDto> Scripts { get; set; } = [];
    }
}
