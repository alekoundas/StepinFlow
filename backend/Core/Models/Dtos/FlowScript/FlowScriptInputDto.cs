namespace Core.Models.Dtos.FlowScript
{
    /// <summary>An input as a script declares it. A secret has no value in the file.</summary>
    public class FlowScriptInputDto
    {
        public string Name { get; set; } = string.Empty;
        public string DefaultValue { get; set; } = string.Empty;
        public bool IsSecret { get; set; }
    }
}
