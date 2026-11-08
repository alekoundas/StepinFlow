using Core.Catalogs;
using Core.Models.Business;
using Core.Models.Dtos;
using Core.Ports;

namespace Business.Tests.Fakes
{
    /// <summary>Reads the next text, or the next screen of words, in the queue each time, and the last one for ever after.</summary>
    public sealed class FakeOcrService : IOcrService
    {
        private readonly Queue<string> _reads = new Queue<string>();
        private string _last = string.Empty;
        private readonly Queue<IReadOnlyList<OcrWord>> _wordReads = new Queue<IReadOnlyList<OcrWord>>();
        private IReadOnlyList<OcrWord> _lastWords = [];
        private readonly HashSet<string> _downloaded = new HashSet<string>();

        public int ReadCount { get; private set; }
        public int WordReadCount { get; private set; }

        public FakeOcrService Reads(params string[] texts)
        {
            foreach (string text in texts)
                _reads.Enqueue(text);

            return this;
        }

        public FakeOcrService ReadsWords(params IReadOnlyList<OcrWord>[] screens)
        {
            foreach (IReadOnlyList<OcrWord> words in screens)
                _wordReads.Enqueue(words);

            return this;
        }

        /// <summary>The dpi of the last read, as the caller gave it.</summary>
        public int LastDpi { get; private set; }

        public Task<IReadOnlyList<OcrWord>> ReadWordsAsync(RawImage image, string language, int dpi, CancellationToken ct = default)
        {
            WordReadCount++;
            LastDpi = dpi;

            if (_wordReads.Count > 0)
                _lastWords = _wordReads.Dequeue();

            return Task.FromResult(_lastWords);
        }

        public Task<string> ReadAsync(RawImage image, string language, int dpi, CancellationToken ct = default)
        {
            ReadCount++;
            LastDpi = dpi;

            if (_reads.Count > 0)
                _last = _reads.Dequeue();

            return Task.FromResult(_last);
        }

        /// <summary>Readers on this machine besides the built-in one, which is always there.</summary>
        public FakeOcrService Downloaded(params string[] readers)
        {
            foreach (string reader in readers)
                _downloaded.Add(reader);

            return this;
        }

        public IReadOnlyList<OcrLanguageDto> GetLanguages()
        {
            return OcrReaderCatalog.All
                .Select(x => new OcrLanguageDto
                {
                    Tag = x.Id,
                    DisplayName = x.Name,
                    IsBuiltIn = x.IsBuiltIn,
                    IsInstalled = x.IsBuiltIn || _downloaded.Contains(x.Id),
                })
                .ToList();
        }
    }
}
