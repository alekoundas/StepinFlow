using Core.Catalogs;
using Core.Models.Business;
using Core.Models.Dtos;
using Core.Ports;

namespace Transport.Tests.Fakes
{
    /// <summary>A machine with only the built-in reader, for validation. Nothing here reads text.</summary>
    public sealed class FakeOcrService : IOcrService
    {
        public IReadOnlyList<OcrLanguageDto> GetLanguages()
        {
            return OcrReaderCatalog.All
                .Select(x => new OcrLanguageDto { Tag = x.Id, DisplayName = x.Name, IsBuiltIn = x.IsBuiltIn, IsInstalled = x.IsBuiltIn })
                .ToList();
        }

        public Task<string> ReadAsync(RawImage image, string language, int dpi, CancellationToken ct = default)
        {
            throw new NotImplementedException();
        }

        public Task<IReadOnlyList<OcrWord>> ReadWordsAsync(RawImage image, string language, int dpi, CancellationToken ct = default)
        {
            throw new NotImplementedException();
        }
    }
}
