using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Localization;

namespace App.Localization
{
    public class JsonLocalizerFactory : IStringLocalizerFactory
    {
        private readonly string _resourcesPath;

        public JsonLocalizerFactory(IHostEnvironment env)
        {
            // Env-agnostic: Works in debug (src/Core/Translations) or release (Resources)
            _resourcesPath = Path.Combine(env.ContentRootPath, "Resources");
        }

        public IStringLocalizer Create(Type resourceSource) => new JsonLocalizer(_resourcesPath);

        public IStringLocalizer Create(string baseName, string location) => new JsonLocalizer(_resourcesPath);
    }
}