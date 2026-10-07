namespace Core.Models.Dtos.FlowScript
{
    public class FlowScriptErrorDto
    {
        public int Line { get; set; }
        public int Column { get; set; }
        public string Message { get; set; } = string.Empty;

        /// <summary>The message is for reading; these are for deciding what to do about it.</summary>
        public string Code { get; set; } = string.Empty;
        public string Severity { get; set; } = string.Empty;
    }
}
