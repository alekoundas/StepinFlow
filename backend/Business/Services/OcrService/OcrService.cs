using Core.Models.Business;
using Core.Models.Dtos;
using System.ComponentModel;
using System.Diagnostics;
using Windows.Globalization;
using Windows.Graphics.Imaging;
using Windows.Media.Ocr;
using Windows.Security.Cryptography;

namespace Business.Services.OcrService
{
    public sealed class OcrService : IOcrService
    {
        public IReadOnlyList<OcrLanguageDto> GetLanguages()
        {
            HashSet<string> installed = OcrEngine.AvailableRecognizerLanguages
                .Select(x => x.LanguageTag)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            return installed
                .Union(OcrLanguageCatalog.InstallableTags, StringComparer.OrdinalIgnoreCase)
                .Select(tag => new OcrLanguageDto
                {
                    Tag = tag,
                    DisplayName = DisplayName(tag),
                    IsInstalled = installed.Contains(tag),
                })
                .OrderByDescending(x => x.IsInstalled)
                .ThenBy(x => x.DisplayName, StringComparer.CurrentCulture)
                .ToList();
        }

        private static readonly TimeSpan InstallWatchWindow = TimeSpan.FromSeconds(15);

        public async Task<OcrLanguageInstallResultDto> InstallLanguageAsync(string languageTag, CancellationToken ct = default)
        {
            // The tag reaches PowerShell as text, so it is matched against the catalog rather
            // than escaped.
            if (!OcrLanguageCatalog.InstallableTags.Contains(languageTag))
                return new OcrLanguageInstallResultDto { ErrorMessage = $"Windows has no OCR pack for \"{languageTag}\"." };

            string script = $"try {{ Add-WindowsCapability -Online -Name 'Language.OCR~~~{languageTag}~0.0.1.0' -ErrorAction Stop; exit 0 }} catch {{ exit 1 }}";

            ProcessStartInfo startInfo = new ProcessStartInfo
            {
                FileName = "powershell.exe",
                Arguments = $"-NoProfile -NonInteractive -Command \"{script}\"",
                UseShellExecute = true,
                Verb = "runas",
                WindowStyle = ProcessWindowStyle.Hidden,
            };

            try
            {
                using Process? process = Process.Start(startInfo);
                if (process == null)
                    return new OcrLanguageInstallResultDto { ErrorMessage = "Windows did not start the installer." };

                using CancellationTokenSource watch = CancellationTokenSource.CreateLinkedTokenSource(ct);
                watch.CancelAfter(InstallWatchWindow);

                try
                {
                    await process.WaitForExitAsync(watch.Token);
                }
                catch (OperationCanceledException) when (!ct.IsCancellationRequested)
                {
                    return new OcrLanguageInstallResultDto { IsRunning = true };
                }

                return process.ExitCode == 0
                    ? new OcrLanguageInstallResultDto()
                    : new OcrLanguageInstallResultDto { ErrorMessage = "Windows could not install the pack. Add the language from Windows settings instead." };
            }
            catch (Win32Exception ex) when (ex.NativeErrorCode == 1223)
            {
                return new OcrLanguageInstallResultDto { ErrorMessage = "Installing a language pack needs administrator permission." };
            }
            catch (Exception ex)
            {
                return new OcrLanguageInstallResultDto { ErrorMessage = ex.Message };
            }
        }

        public void OpenWindowsLanguageSettings() =>
            Process.Start(new ProcessStartInfo("ms-settings:regionlanguage") { UseShellExecute = true });

        public async Task<string> ReadAsync(RawImage image, string language, CancellationToken ct = default)
        {
            if (image.IsEmpty)
                return string.Empty;

            OcrEngine? engine = string.IsNullOrWhiteSpace(language)
                ? OcrEngine.TryCreateFromUserProfileLanguages()
                : OcrEngine.TryCreateFromLanguage(new Language(language));

            if (engine == null)
                throw new InvalidOperationException($"Windows cannot read \"{language}\" on this machine. Install it from Settings.");

            using SoftwareBitmap bitmap = ToBitmap(image);
            OcrResult result = await engine.RecognizeAsync(bitmap).AsTask(ct);

            return result.Text;
        }


        // ================================================================
        // Private methods
        // ================================================================

        private static string DisplayName(string tag) => Language.IsWellFormed(tag) ? new Language(tag).DisplayName : tag;

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
