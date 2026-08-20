using Core.Models.Business;
using Core.Models.Dtos;

namespace Business.Services.OcrService
{
    public interface IOcrService
    {
        /// <summary>Every language that can be read now, plus the ones that could be installed.</summary>
        IReadOnlyList<OcrLanguageDto> GetLanguages();

        /// <summary>
        /// Installs a language pack through Windows, which needs elevation: the UAC prompt is the
        /// consent, and nothing is installed without it.
        ///
        /// A pack can take minutes to download, longer than a caller is willing to wait, so this
        /// gives up watching after a while and says the install is still running.
        /// </summary>
        Task<OcrLanguageInstallResultDto> InstallLanguageAsync(string languageTag, CancellationToken ct = default);

        /// <summary>Opens Windows language settings, for when the install cannot run here.</summary>
        void OpenWindowsLanguageSettings();

        /// <summary>
        /// Reads the text in a captured area. Throws when the language is not installed, which is
        /// a machine problem the author has to know about rather than an empty result.
        /// </summary>
        Task<string> ReadAsync(RawImage image, string language, CancellationToken ct = default);
    }
}
