namespace Core.Models.Dtos
{
    public class OcrLanguageDto
    {
        public string Tag { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;

        /// <summary>Only an installed language can be read. The rest are offers to install.</summary>
        public bool IsInstalled { get; set; }
    }
}
