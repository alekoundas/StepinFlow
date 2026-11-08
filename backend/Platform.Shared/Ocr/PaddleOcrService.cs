using Core.Catalogs;
using Core.Models.Business;
using Core.Models.Business.Ocr;
using Core.Models.Dtos;
using Core.Ports;
using Microsoft.ML.OnnxRuntime;
using OpenCvSharp;
using Platform.Shared.Ocr.CharacterExtractor;
using Platform.Shared.Ocr.Text;

namespace Platform.Shared.Ocr
{
    /// <summary>
    /// Text read by PaddleOCR PP-OCRv5 models on the ONNX Runtime, the same on every OS.
    /// 2 models, a line finder and a reader (per language family).
    /// Line finder and Latin reader ships with the app
    /// Extra readers downloaded from Settings are in the app's data folder.
    ///
    /// A model is loaded the first time it is needed and kept, about 40 MB for the line finder and
    /// Latin. What a read works in - hundreds of MB for a whole screen - goes back after each read.
    /// </summary>
    public sealed class PaddleOcrService : IOcrService, IDisposable
    {
        private const int DEFAULT_DPI = 96;

        private readonly string _builtInFolder;
        private readonly string _downloadFolder;
        private readonly object _lockObj = new object(); // two reads never load the same model twice.
        private readonly Dictionary<string, TextLineReader> _readers = new Dictionary<string, TextLineReader>(StringComparer.OrdinalIgnoreCase);

        private TextLineFinder? _lineFinder;

        public PaddleOcrService(string builtInFolder, string downloadFolder)
        {
            _builtInFolder = builtInFolder;
            _downloadFolder = downloadFolder;
        }


        // ================================================================
        // Public methods
        // ================================================================

        public IReadOnlyList<OcrLanguageDto> GetLanguages()
        {
            return OcrReaderCatalog.All
                .Select(x => new OcrLanguageDto
                {
                    Tag = x.Id,
                    DisplayName = x.Name,
                    Languages = x.Languages,
                    Megabytes = (int)Math.Ceiling(x.ModelBytes / 1_000_000d),
                    IsInstalled = FolderOf(x) != null,
                    IsBuiltIn = x.IsBuiltIn,
                })
                .ToList();
        }

        public async Task<string> ReadAsync(RawImage image, string language, int dpi, CancellationToken ct = default)
        {
            IReadOnlyList<OcrWord> words = await ReadWordsAsync(image, language, dpi, ct);
            return string.Join(' ', words.Select(x => x.Text));
        }

        public async Task<IReadOnlyList<OcrWord>> ReadWordsAsync(RawImage image, string language, int dpi, CancellationToken ct = default)
        {
            if (image.IsEmpty)
                return [];

            // The models are CPU work that can take a second on a whole screen, so not on the caller's thread.
            return await Task.Run(() => Read(image, language, dpi), ct);
        }

        public void Dispose()
        {
            lock (_lockObj)
            {
                _lineFinder?.Dispose();
                _lineFinder = null;

                foreach (TextLineReader reader in _readers.Values)
                    reader.Dispose();
                _readers.Clear();
            }
        }


        // ================================================================
        // Private methods
        // ================================================================

        private List<OcrWord> Read(RawImage image, string language, int dpi)
        {
            OcrReader reader = OcrReaderCatalog.Find(language)
                ?? throw new InvalidOperationException($"\"{language}\" is not a text reader this app knows. Pick one in the step.");

            OcrModels ocrModels = Load(reader);

            // Convert image to Mat and drop alpha channel.
            using Mat bgra = Mat.FromPixelData(image.Height, image.Width, MatType.CV_8UC4, image.Pixels, image.Stride); // Convert image to Mat.
            using Mat bgr = new Mat(); 
            Cv2.CvtColor(bgra, bgr, ColorConversionCodes.BGRA2BGR); // Drop the alpha channel (transparency) and save image to bgr.

            // Shrink if image is scaled over 100%.
            double shrink = 1;
            if (dpi > DEFAULT_DPI)
                shrink = (double)dpi / DEFAULT_DPI;

            List<Point2f[]> boxes = ocrModels.LineFinder.Find(bgr, shrink);
            List<OcrWord> ocrWords = ocrModels.LineReader.Read(bgr, boxes);
            return ocrWords;
        }

        // Load the LineFinder and Reader models into memory.
        private OcrModels Load(OcrReader reader)
        {
            lock (_lockObj)
            {
                if (_lineFinder == null)
                {
                    // Find the path of LineFinder model.
                    string lineFinderPath = Path.Combine(_builtInFolder, OcrReaderCatalog.LINE_FINDER_FILE_NAME);
                    if (!File.Exists(lineFinderPath))
                        throw new InvalidOperationException($"The text recognition model is missing from {_builtInFolder}. Reinstall the app; working on it, run npm run models.");

                    // Load model into memory
                    InferenceSession session = NewSession(lineFinderPath, keepsMemory: false);
                    _lineFinder = new TextLineFinder(session);
                }

                if (!_readers.TryGetValue(reader.Id, out TextLineReader? lineReader))
                {
                    // Find folder of reader model.
                    string readerfolder = FolderOf(reader)
                        ?? throw new InvalidOperationException($"The {reader.Name} text reader is not downloaded. Download it in Settings, under Text recognition.");

                    // Load model into memory
                    string readerPath = Path.Combine(readerfolder, reader.ModelFileName);
                    string[] characters = OcrCharacterExtractor.Load(Path.Combine(readerfolder, reader.CharactersFileName));
                    InferenceSession session = NewSession(readerPath, keepsMemory: true);
                    try
                    {
                        lineReader = new TextLineReader(session, characters);
                    }
                    catch
                    {
                        session.Dispose();
                        throw;
                    }

                    _readers[reader.Id] = lineReader;
                }

                return new OcrModels(_lineFinder, lineReader);
            }
        }

        // Find the model path from the 2 store locations.
        private string? FolderOf(OcrReader reader)
        {
            foreach (string folder in new[] { _builtInFolder, _downloadFolder })
            {
                string modelPath = Path.Combine(folder, reader.ModelFileName);
                string modelCharactersPath = Path.Combine(folder, reader.CharactersFileName);
                if (File.Exists(modelPath) && File.Exists(modelCharactersPath))
                    return folder;
            }

            return null;
        }


        // ONNX Runtime session with the model.
        // InferenceSession is the loaded copy of a model.
        // ONNX Runtime holds the block of memory(arena) and reuses it instead of re-allocating.
        // Line finder = OFF : it looks at the whole picture, so a 4K screen would keep gigabytes.
        // Line reader - ON  : looks at one line 48 pixels tall, so a small image will keep a few megabytes.
        // ORT_ENABLE_ALL    : lets it merge operations that follow one another into one,
        //                     drop work whose result never changes,
        //                     choose faster forms of the same calculation.
        private static InferenceSession NewSession(string path, bool keepsMemory)
        {
            using SessionOptions options = new SessionOptions
            {
                GraphOptimizationLevel = GraphOptimizationLevel.ORT_ENABLE_ALL,
                LogSeverityLevel = OrtLoggingLevel.ORT_LOGGING_LEVEL_ERROR,
                EnableCpuMemArena = keepsMemory,
                EnableMemoryPattern = keepsMemory,
            };

            return new InferenceSession(path, options);
        }





        // ================================================================
        // Private types
        // ================================================================

        private sealed record OcrModels(TextLineFinder LineFinder, TextLineReader LineReader);
    }
}
