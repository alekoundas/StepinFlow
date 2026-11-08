namespace Core.Models.Dtos
{
    public class OcrLanguageDto
    {
        /// <summary>The reader's id, which is what a step saves.</summary>
        public string Tag { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
        public string Languages { get; set; } = string.Empty;
        public int Megabytes { get; set; }

        public bool IsInstalled { get; set; }
        public bool IsBuiltIn { get; set; }
    }
}
