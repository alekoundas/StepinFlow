namespace Core.Models.Dtos
{
    public class OcrLanguageInstallResultDto
    {
        /// <summary>Windows is still downloading. The list has to be polled for the result.</summary>
        public bool IsRunning { get; set; }

        /// <summary>Set when the install was refused or failed outright.</summary>
        public string? ErrorMessage { get; set; }
    }
}
