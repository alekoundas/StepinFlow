using Core.Models.Business;

namespace Business.Services.OcrService
{
    public interface IOcrService
    {
        /// <summary>Language tags Windows can currently read, from the installed language packs.</summary>
        IReadOnlyList<string> AvailableLanguages { get; }

        /// <summary>
        /// Reads the text in a captured area. Throws when the language is not installed, which is
        /// a machine problem the author has to know about rather than an empty result.
        /// </summary>
        Task<string> ReadAsync(RawImage image, string language, CancellationToken ct = default);
    }
}
