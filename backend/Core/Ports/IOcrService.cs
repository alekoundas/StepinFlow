using Core.Models.Business;
using Core.Models.Dtos;

namespace Core.Ports
{
    // Dependency Inversion Principle(DIP)
    public interface IOcrService
    {
        
        /// <summary>Every reader: built in, downloaded, or available to download from Settings.</summary>
        IReadOnlyList<OcrLanguageDto> GetLanguages();


        /// <summary>Every word in the image with its rectangle, in the order read. Throws when the reader doesnt exist.</summary>
        Task<IReadOnlyList<OcrWord>> ReadWordsAsync(RawImage image, string language, int dpi, CancellationToken ct = default);


        /// <summary>
        /// Reads the text in a captured area, line after line. 
        /// Throws when the reader doesnt exist.
        /// The screen's dpi used for text on a scaled screen to be looked at the size it would have at 100%.
        /// </summary>
        Task<string> ReadAsync(RawImage image, string language, int dpi, CancellationToken ct = default);
    }
}
