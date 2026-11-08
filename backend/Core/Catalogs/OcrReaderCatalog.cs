using Core.Models.Business.Ocr;

namespace Core.Catalogs
{
    /// <summary>
    /// Every text reader model the app offers, from PaddleOCR's PP-OCRv5 (Apache-2.0). 
    /// Latin ships with the app.
    /// The rest are downloaded from the app's own `ocr-v1` release.
    /// </summary>
    public static class OcrReaderCatalog
    {
        public const string RELEASE_URL = "https://github.com/alekoundas/StepinFlow/releases/download/ocr-v1";
        public const string LINE_FINDER_FILE_NAME = "ocr-line-finder.onnx";
        public const string DEFAULT_READER = "latin";

        public static IReadOnlyList<OcrReader> All { get; } =
        [
            new OcrReader
            {
                Id = "latin",
                Name = "Latin",
                Languages = "English, French, German, Spanish, Italian, Portuguese, Dutch, Polish, Turkish and other languages written in Latin letters",
                IsBuiltIn = true,
                ModelBytes = 8_042_023,
                ModelSha256 = "7888113072263cb471b93f66dd5e2ad70548dc526fa1ace760d0d973dd121498",
            },
            new OcrReader
            {
                Id = "greek",
                Name = "Greek",
                Languages = "Greek and English",
                ModelBytes = 7_808_735,
                ModelSha256 = "2acf17fcaea2bc81b878e311e6263b8885f48bb03796f75f9f30ed3242bbaa6d",
            },
            new OcrReader
            {
                Id = "eslav",
                Name = "East Slavic",
                Languages = "Russian, Ukrainian and Belarusian",
                ModelBytes = 7_887_627,
                ModelSha256 = "b3018ef2b09a0250b6e0c8e871c927098363e5fd4df890cc68e8358eb0aaf1bd",
            },
            new OcrReader
            {
                Id = "cyrillic",
                Name = "Cyrillic",
                Languages = "Russian, Serbian, Bulgarian, Mongolian and other languages written in Cyrillic",
                ModelBytes = 8_048_799,
                ModelSha256 = "5371ee1ddaa7983cc62d0818d99e982b6804638c85e4f960d59a574094e172e5",
            },
            new OcrReader
            {
                Id = "chinese",
                Name = "Chinese and Japanese",
                Languages = "Chinese, simplified and traditional, Japanese and English",
                ModelBytes = 16_534_782,
                ModelSha256 = "da72dc72ca4dc220df0dfde68c1dedc31c58d3e76a25871122e5056227d50092",
            },
            new OcrReader
            {
                Id = "korean",
                Name = "Korean",
                Languages = "Korean",
                ModelBytes = 13_418_787,
                ModelSha256 = "92f0b7785e64fc9090106a241cf4c1eb97472824558272751b88a2a4476d3a08",
            },
            new OcrReader
            {
                Id = "arabic",
                Name = "Arabic",
                Languages = "Arabic, Persian, Urdu and other languages written in Arabic script",
                ModelBytes = 7_998_947,
                ModelSha256 = "799113ebf267fbe742deb99eb36e8d42c9ddc5291ceacf92add41b4d52a59110",
            },
            new OcrReader
            {
                Id = "devanagari",
                Name = "Devanagari",
                Languages = "Hindi, Marathi, Nepali and other languages written in Devanagari",
                ModelBytes = 7_912_311,
                ModelSha256 = "cb789212ce96c69d3e74728ae4309d179281d68cb3945d0616b67cafab41c986",
            },
            new OcrReader
            {
                Id = "thai",
                Name = "Thai",
                Languages = "Thai",
                ModelBytes = 7_891_015,
                ModelSha256 = "27618be66018f8598ac0a526a593f9f1cebf794e7eded93428e8fb016e537f5f",
            },
            new OcrReader
            {
                Id = "tamil",
                Name = "Tamil",
                Languages = "Tamil",
                ModelBytes = 7_885_691,
                ModelSha256 = "c6d2b682d2a0ea4cb1fccdba295976f93fd439964d16cdc666cadef531accbee",
            },
            new OcrReader
            {
                Id = "telugu",
                Name = "Telugu",
                Languages = "Telugu",
                ModelBytes = 7_898_759,
                ModelSha256 = "8238bfc46d4cffe720ed6706e3842802467343497428693ff2bfb4e6b3caa36b",
            },
        ];



        // ================================================================
        // Public methods
        // ================================================================

        /// <summary>Find reader by id. Latin when null.</summary>
        public static OcrReader? Find(string? id)
        {
            string wanted = DEFAULT_READER;

            if (!string.IsNullOrWhiteSpace(id))
                wanted = id;

            return All.FirstOrDefault(x => string.Equals(x.Id, wanted, StringComparison.OrdinalIgnoreCase));
        }
    }
}
