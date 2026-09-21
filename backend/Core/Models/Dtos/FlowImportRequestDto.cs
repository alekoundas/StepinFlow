namespace Core.Models.Dtos
{
    public class FlowImportRequestDto
    {
        /// <summary>The .sflw file to read. Templates come from the folder named after it, beside it.</summary>
        public string ScriptPath { get; set; } = string.Empty;
    }
}
