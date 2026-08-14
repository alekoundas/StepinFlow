namespace Core.Models.Dtos
{
    public class TextSearchTestResultDto
    {
        public bool IsResolved { get; set; }
        public string? ErrorMessage { get; set; }

        /// <summary>Everything Windows read in the area, so a near miss is visible.</summary>
        public string Text { get; set; } = string.Empty;

        public bool IsMatch { get; set; }

        /// <summary>The value the step would hand to later steps, after extraction.</summary>
        public string ResultValue { get; set; } = string.Empty;
    }
}
