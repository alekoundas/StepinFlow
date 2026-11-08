namespace Core.Models.Business.Ocr
{
    /// <summary>
    /// The PaddleOCR's text reader.
    /// </summary>
    public sealed class OcrReader
    {
        public string Id { get; init; } = string.Empty;
        public string Name { get; init; } = string.Empty;
        public string Languages { get; init; } = string.Empty;

        /// <summary>Shipped with the app. The rest are downloaded from Settings.</summary>
        public bool IsBuiltIn { get; init; }

        public long ModelBytes { get; init; }

        /// <summary>Checked after a download, before the file is used.</summary>
        public string ModelSha256 { get; init; } = string.Empty;

        public string ModelFileName { get => $"ocr-reader-{Id}.onnx"; }
        public string CharactersFileName { get => $"ocr-reader-{Id}.yml"; }
    }
}
