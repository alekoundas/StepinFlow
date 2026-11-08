using Core.Enums;
using Core.Models.Database;

using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace DataAccess.Tests
{
    public sealed class OcrLanguageIsReaderMigrationTests : IDisposable
    {
        private const string BEFORE = "20261107134007_CountStoppedExecutionSteps";
        private const string AFTER = "20261107231538_OcrLanguageIsReader";

        private readonly TestDatabase _database = new TestDatabase();

        public void Dispose()
        {
            _database.Dispose();
        }

        // The real database has one, "Price - Crude Oil" reading en-US.
        [Theory]
        [InlineData("en-US", "latin")]
        [InlineData("de-DE", "latin")]
        [InlineData("sr-Latn-RS", "latin")]
        [InlineData("el-GR", "greek")]
        [InlineData("el", "greek")]
        [InlineData("ru-RU", "eslav")]
        [InlineData("sr-Cyrl-RS", "cyrillic")]
        [InlineData("ja-JP", "chinese")]
        [InlineData("zh-TW", "chinese")]
        [InlineData("ko-KR", "korean")]
        [InlineData("ar-SA", "arabic")]
        public void A_windows_language_becomes_the_reader_for_its_script(string tag, string reader)
        {
            MigrateTo(BEFORE);
            int stepId = SavedTextSearch(tag);
            MigrateTo(AFTER);

            LanguageOf(stepId).ShouldBe(reader);
        }

        [Fact]
        public void None_picked_stays_none_and_reads_with_latin()
        {
            MigrateTo(BEFORE);
            int stepId = SavedTextSearch(string.Empty);
            MigrateTo(AFTER);

            LanguageOf(stepId).ShouldBe(string.Empty);
        }


        // ================================================================
        // Private methods
        // ================================================================

        private void MigrateTo(string migration)
        {
            using AppDbContext db = _database.Open();
            db.GetService<IMigrator>().Migrate(migration);
        }

        private string LanguageOf(int stepId)
        {
            using AppDbContext db = _database.Open();
            return db.FlowSteps.Where(x => x.Id == stepId).Select(x => x.OcrLanguage).Single();
        }

        // The table is the same before and after, so the model can write it.
        private int SavedTextSearch(string language)
        {
            using AppDbContext db = _database.Open();

            Flow flow = new Flow { Name = "Price - Crude Oil", PublicId = Guid.NewGuid() };
            db.Flows.Add(flow);
            db.SaveChanges();

            FlowStep step = new FlowStep { FlowId = flow.Id, FlowStepType = FlowStepTypeEnum.SEARCH_TEXT, Name = "Read the price", OcrLanguage = language };
            db.FlowSteps.Add(step);
            db.SaveChanges();

            return step.Id;
        }
    }
}
