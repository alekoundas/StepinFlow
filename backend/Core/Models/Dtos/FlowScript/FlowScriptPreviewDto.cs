namespace Core.Models.Dtos.FlowScript
{
    /// <summary>
    /// What importing a .sflw file would do, with nothing saved: the file read as an import reads
    /// it, the templates found beside it, and the flow it would replace or the name it would clash
    /// with.
    /// </summary>
    public class FlowScriptPreviewDto
    {
        public string ScriptPath { get; set; } = string.Empty;
        public string Script { get; set; } = string.Empty;

        /// <summary>The folder named after the file, beside it, that the templates are read from.</summary>
        public string TemplateFolderPath { get; set; } = string.Empty;

        /// <summary>Any of these and nothing can be imported.</summary>
        public List<FlowScriptErrorDto> Errors { get; set; } = [];

        /// <summary>What the file gets away with. An import goes ahead with them.</summary>
        public List<FlowScriptErrorDto> Warnings { get; set; } = [];

        public string FlowName { get; set; } = string.Empty;
        public Guid PublicId { get; set; }
        public string Description { get; set; } = string.Empty;

        /// <summary>Steps, without the Success and Failure branches that hold them.</summary>
        public int StepCount { get; set; }

        public List<FlowScriptInputDto> Inputs { get; set; } = [];

        /// <summary>Each picture once, however many steps search for it.</summary>
        public List<string> TemplateNames { get; set; } = [];

        /// <summary>Named by the file but not a PNG in the folder beside it. Imported without a picture.</summary>
        public List<string> MissingTemplates { get; set; } = [];

        public FlowScriptImportTargetDto Target { get; set; } = new FlowScriptImportTargetDto();

        /// <summary>The flow it replaces as it is now, as script text. Empty for a new flow.</summary>
        public string ReplacedScript { get; set; } = string.Empty;
    }
}
