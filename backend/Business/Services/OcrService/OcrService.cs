using Core.Models.Business;
using Windows.Globalization;
using Windows.Graphics.Imaging;
using Windows.Media.Ocr;
using Windows.Security.Cryptography;

namespace Business.Services.OcrService
{
    public sealed class OcrService : IOcrService
    {
        public IReadOnlyList<string> AvailableLanguages => OcrEngine.AvailableRecognizerLanguages.Select(x => x.LanguageTag).ToList();

        public async Task<string> ReadAsync(RawImage image, string language, CancellationToken ct = default)
        {
            if (image.IsEmpty)
                return string.Empty;

            OcrEngine? engine = string.IsNullOrWhiteSpace(language)
                ? OcrEngine.TryCreateFromUserProfileLanguages()
                : OcrEngine.TryCreateFromLanguage(new Language(language));

            if (engine == null)
                throw new InvalidOperationException($"Windows cannot read \"{language}\" on this machine. Installed: {string.Join(", ", AvailableLanguages)}.");

            using SoftwareBitmap bitmap = ToBitmap(image);
            OcrResult result = await engine.RecognizeAsync(bitmap).AsTask(ct);

            return result.Text;
        }


        // ================================================================
        // Private methods
        // ================================================================

        // Alpha is ignored rather than premultiplied: the desktop is opaque, and trusting whatever
        // the capture left in that byte is what turns a screenshot black.
        private static SoftwareBitmap ToBitmap(RawImage image)
        {
            byte[] pixels = Pack(image);

            return SoftwareBitmap.CreateCopyFromBuffer(
                CryptographicBuffer.CreateFromByteArray(pixels),
                BitmapPixelFormat.Bgra8,
                image.Width,
                image.Height,
                BitmapAlphaMode.Ignore);
        }

        /// <summary>A capture row can be padded, and the buffer copy expects rows back to back.</summary>
        private static byte[] Pack(RawImage image)
        {
            int rowBytes = image.Width * 4;
            if (image.Stride == rowBytes)
                return image.Pixels;

            byte[] packed = new byte[rowBytes * image.Height];
            for (int row = 0; row < image.Height; row++)
                Buffer.BlockCopy(image.Pixels, row * image.Stride, packed, row * rowBytes, rowBytes);

            return packed;
        }
    }
}
