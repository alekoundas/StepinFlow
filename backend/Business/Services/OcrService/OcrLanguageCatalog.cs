namespace Business.Services.OcrService
{
    /// <summary>
    /// The languages Windows ships an OCR pack for. Only a hint list of what can be offered for
    /// install: what is actually readable always comes from the engine, so a language missing
    /// from here still shows up once installed.
    /// </summary>
    public static class OcrLanguageCatalog
    {
        public static IReadOnlySet<string> InstallableTags { get; } = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "af-ZA", "ar-SA", "bs-Latn-BA", "cs-CZ", "da-DK", "de-DE", "el-GR", "en-GB", "en-US",
            "es-ES", "es-MX", "fi-FI", "fr-CA", "fr-FR", "hr-HR", "hu-HU", "it-IT", "ja-JP",
            "ko-KR", "nb-NO", "nl-NL", "pl-PL", "pt-BR", "pt-PT", "ro-RO", "ru-RU", "sk-SK",
            "sl-SI", "sr-Cyrl-RS", "sr-Latn-RS", "sv-SE", "tr-TR", "zh-CN", "zh-TW",
        };
    }
}
