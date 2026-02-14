using Microsoft.Extensions.Localization;
using System.Collections.Concurrent;
using System.Globalization;
using System.Text.Json;

namespace App.Localization
{
    public class JsonLocalizer : IStringLocalizer
    {
        // ConcurrentDictionary ==> thread-safe cache for translations.
        private readonly ConcurrentDictionary<string, Dictionary<string, string>> _cache = new();
        private readonly string _resourcesPath;

        public JsonLocalizer(string resourcesPath)
        {
            _resourcesPath = resourcesPath;
        }

        public LocalizedString this[string name]
        {
            get
            {
                string? value = GetString(name);
                return new LocalizedString(name, value ?? name, value == null);
            }
        }

        public LocalizedString this[string name, params object[] arguments]
        {
            get
            {
                LocalizedString? actualValue = this[name];
                if (actualValue.ResourceNotFound) return actualValue;

                // Optional: Translate args if they're keys (for DTO messages)
                //arguments = arguments.Select(arg =>
                //{
                //    if (arg is string key && !actualValue.ResourceNotFound)
                //        return this[key].Value;
                //    return arg;
                //}).ToArray();

                return new LocalizedString(name, string.Format(actualValue.Value, arguments), false);
            }
        }

        private string? GetString(string key)
        {
            var culture = CultureInfo.CurrentCulture.Name;
            if (!_cache.TryGetValue(culture, out var translations))
            {
                translations = LoadTranslations(culture);
                if (translations == null) return null;  // Fallback handled in Load
                _cache.TryAdd(culture, translations);
            }
            return translations.TryGetValue(key, out var value) ? value : null;
        }

        private Dictionary<string, string>? LoadTranslations(string culture)
        {
            var filePath = Path.Combine(_resourcesPath, $"{culture}.json");
            if (!File.Exists(filePath))
            {
                // Fallback to en
                filePath = Path.Combine(_resourcesPath, "en.json");
                if (!File.Exists(filePath)) return null;
            }

            var jsonContent = File.ReadAllText(filePath);
            return JsonSerializer.Deserialize<Dictionary<string, string>>(jsonContent);
        }

        public IEnumerable<LocalizedString> GetAllStrings(bool includeParentCultures)
        {
            throw new NotImplementedException();
        }
    }
}
