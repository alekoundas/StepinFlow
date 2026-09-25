using Core.Models.Business;
using Core.Models.Dtos;
using Core.Ports;

namespace Business.Tests.Fakes
{
    /// <summary>Reads the next text in the queue each time, and the last one for ever after.</summary>
    public sealed class FakeOcrService : IOcrService
    {
        private readonly Queue<string> _reads = new Queue<string>();
        private string _last = string.Empty;

        public int ReadCount { get; private set; }

        public FakeOcrService Reads(params string[] texts)
        {
            foreach (string text in texts)
                _reads.Enqueue(text);

            return this;
        }

        public Task<string> ReadAsync(RawImage image, string language, CancellationToken ct = default)
        {
            ReadCount++;

            if (_reads.Count > 0)
                _last = _reads.Dequeue();

            return Task.FromResult(_last);
        }

        public IReadOnlyList<OcrLanguageDto> GetLanguages()
        {
            throw new NotImplementedException();
        }

        public Task<OcrLanguageInstallResultDto> InstallLanguageAsync(string languageTag, CancellationToken ct = default)
        {
            throw new NotImplementedException();
        }

        public void OpenWindowsLanguageSettings()
        {
            throw new NotImplementedException();
        }
    }
}
