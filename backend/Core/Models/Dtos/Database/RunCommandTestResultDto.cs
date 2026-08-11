namespace Core.Models.Dtos
{
    public class RunCommandTestResultDto
    {
        public bool IsSuccess { get; set; }
        public string? ErrorMessage { get; set; }

        /// <summary>What actually ran, after the preset and its parameter were applied.</summary>
        public string ResolvedCommand { get; set; } = string.Empty;

        public int ExitCode { get; set; }
        public long DurationMilliseconds { get; set; }
        public string StandardOutput { get; set; } = string.Empty;
        public string StandardError { get; set; } = string.Empty;

        /// <summary>The value the step would hand to later steps, after extraction.</summary>
        public string ResultValue { get; set; } = string.Empty;
    }
}
