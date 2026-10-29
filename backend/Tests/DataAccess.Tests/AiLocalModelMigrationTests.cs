using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace DataAccess.Tests
{
    /// <summary>
    /// One AI provider became a local model plus a cloud one on top. The settings saved under the
    /// old keys are carried to the new ones, and back.
    /// </summary>
    public sealed class AiLocalModelMigrationTests : IDisposable
    {
        private const string BEFORE = "20261026173627_WindowStepArea";
        private const string AFTER = "20261029145802_AiLocalModel";

        private readonly TestDatabase _database = new TestDatabase();

        public void Dispose()
        {
            _database.Dispose();
        }

        [Fact]
        public void An_Ollama_model_becomes_the_local_model()
        {
            Dictionary<string, string> settings = Migrate(new Dictionary<string, string>
            {
                ["AI_PROVIDER"] = "OLLAMA",
                ["AI_MODEL"] = "qwen3.5:4b",
            });

            settings.ShouldBe(new Dictionary<string, string> { ["AI_OLLAMA_MODEL"] = "qwen3.5:4b" }, ignoreOrder: true);
        }

        [Fact]
        public void An_OpenAI_model_becomes_the_cloud_model_with_its_key()
        {
            Dictionary<string, string> settings = Migrate(new Dictionary<string, string>
            {
                ["AI_PROVIDER"] = "OPENAI",
                ["AI_MODEL"] = "gpt-4o-mini",
                ["AI_API_KEY"] = "sk-test",
            });

            settings.ShouldBe(new Dictionary<string, string>
            {
                ["AI_CLOUD_PROVIDER"] = "OPENAI",
                ["AI_CLOUD_MODEL"] = "gpt-4o-mini",
                ["AI_API_KEY"] = "sk-test",
            }, ignoreOrder: true);
        }

        [Theory]
        [InlineData("OLLAMA", "qwen3.5:4b")]
        [InlineData("OPENAI", "gpt-4o-mini")]
        public void Going_back_restores_the_one_provider(string provider, string model)
        {
            Dictionary<string, string> before = new Dictionary<string, string>
            {
                ["AI_PROVIDER"] = provider,
                ["AI_MODEL"] = model,
            };

            Migrate(before);
            MigrateTo(BEFORE);

            Read().ShouldBe(before, ignoreOrder: true);
        }


        // ================================================================
        // Private methods
        // ================================================================

        private Dictionary<string, string> Migrate(Dictionary<string, string> oldSettings)
        {
            MigrateTo(BEFORE);

            using (AppDbContext db = _database.Open())
            {
                foreach (KeyValuePair<string, string> setting in oldSettings)
                    db.Database.ExecuteSql($"INSERT INTO AppSettings (\"Key\", Value) VALUES ({setting.Key}, {setting.Value})");
            }

            MigrateTo(AFTER);

            return Read();
        }

        private void MigrateTo(string migration)
        {
            using AppDbContext db = _database.Open();
            db.GetService<IMigrator>().Migrate(migration);
        }

        // Raw, because a key the enum no longer has would not load as an entity.
        private Dictionary<string, string> Read()
        {
            using AppDbContext db = _database.Open();

            return db.Database
                .SqlQueryRaw<SettingRow>("SELECT \"Key\", Value FROM AppSettings")
                .ToList()
                .ToDictionary(x => x.Key, x => x.Value);
        }


        // ================================================================
        // Private types
        // ================================================================

        private sealed record SettingRow(string Key, string Value);
    }
}
