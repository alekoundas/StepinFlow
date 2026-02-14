using Microsoft.Extensions.Localization;
using System.Collections.Concurrent;
using System.Globalization;
using System.Text.Json;

namespace App.Translator
{
    public class JsonTranslator : IStringLocalizer
    {

        // ConcurrentDictionary = In-memory Thread-safe(needed because of singleton) cache. IDistributedCache = overkill
        private readonly ConcurrentDictionary<string, JsonElement> _cache = new ConcurrentDictionary<string, JsonElement>();
        private readonly string _translationsPath;

        public JsonTranslator(string translationsPath)
        {
            _translationsPath = translationsPath;
        }

        public LocalizedString this[string name]
        {
            get
            {
                string value = GetString(name);
                LocalizedString translatedValuue = new LocalizedString(name, value ?? name, value == null);
                return translatedValuue;
            }
        }

        public LocalizedString this[string name, params object[] arguments]
        {
            get
            {
                // Try to translate the arguments(Only needed for DTO attribute messages)
                //arguments = arguments.Select(x => this[x.ToString() ?? ""]).ToArray();

                LocalizedString result;
                LocalizedString? actualValue = this[name];

                if (this[name].ResourceNotFound)
                    result = actualValue;
                else
                    result = new LocalizedString(name, string.Format(actualValue.Value, arguments), false);
                return result;
            }
        }


        private string GetString(string key)
        {
            string culture = CultureInfo.CurrentCulture.Name;

            if (_cache.TryGetValue($"{culture}:{key}", out JsonElement jsonElement))
                return jsonElement.GetString() ?? key;


            string? filePath = GetFilePath(culture);
            if (filePath == null) return $"__CANT_FIND_FILE_PATH__";


            string jsonContent = File.ReadAllText(filePath);
            JsonDocument jsonDocument = JsonDocument.Parse(jsonContent);
            if (jsonDocument.RootElement.TryGetProperty(key, out var prop))
            {

                _cache.TryAdd($"{culture}:{key}", prop);
                return prop.GetString() ?? key;
            }

            return "__CANT_FIND_TRANSLATION_PROPERTY__";
        }

        private string? GetFilePath(string culture)
        {
            // Release.
            string filePath = Path.Combine(_translationsPath, "Translations", $"{culture}.json");
            if (File.Exists(filePath))
                return filePath;

            // Debug.
            filePath = "/" + Path.Combine("src", "Core", "Translations", $"{culture}.json");
            if (File.Exists(filePath))
                return filePath;

            return null;
        }

        public IEnumerable<LocalizedString> GetAllStrings(bool includeParentCultures)
        {
            throw new NotImplementedException();
        }
    }
}
