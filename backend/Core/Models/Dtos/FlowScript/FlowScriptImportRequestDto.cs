namespace Core.Models.Dtos.FlowScript
{
    public class FlowScriptImportRequestDto
    {
        /// <summary>The .sflw file to read. Templates come from the folder named after it, beside it.</summary>
        public string ScriptPath { get; set; } = string.Empty;
    }
}
