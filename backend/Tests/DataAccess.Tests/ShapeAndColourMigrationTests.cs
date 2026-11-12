using Core.Enums;
using Core.Models.Database;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace DataAccess.Tests
{
    public sealed class ShapeAndColourMigrationTests : IDisposable
    {
        private const string BEFORE = "20261112091311_RenameCsvColumnsToInputs";
        private const string AFTER = "20261112175218_ShapeAndColour";

        private readonly TestDatabase _database = new TestDatabase();

        public void Dispose()
        {
            _database.Dispose();
        }

        // The real database has 24, all at 0.95 but the one that looks for a badge, at 0.98.
        [Fact]
        public void A_shape_and_brightness_step_becomes_shape_and_colour()
        {
            MigrateTo(BEFORE);
            int stepId = SavedSearch(SavedFlow(), "SHAPE_AND_BRIGHTNESS", 0.95f);
            MigrateTo(AFTER);

            ModeOf(stepId).ShouldBe(TemplateMatchModeEnum.SHAPE_AND_COLOUR);
        }

        // Each template a distinct value, so one moved onto another's would show.
        [Fact]
        public void Its_default_accuracy_takes_the_new_default_and_a_stricter_one_a_strict_bar()
        {
            MigrateTo(BEFORE);
            int stepId = SavedSearch(SavedFlow(), "SHAPE_AND_BRIGHTNESS", 0.95f, 0.98f, 0.9f, 1f);
            MigrateTo(AFTER);

            AccuraciesOf(stepId).ShouldBe([0.8f, 0.95f, 0.8f, 0.95f]);
        }

        [Fact]
        public void A_shape_step_and_its_accuracies_stay_as_they_were()
        {
            MigrateTo(BEFORE);
            int stepId = SavedSearch(SavedFlow(), "SHAPE", 0.95f, 0.98f, 0.7f);
            MigrateTo(AFTER);

            ModeOf(stepId).ShouldBe(TemplateMatchModeEnum.SHAPE);
            AccuraciesOf(stepId).ShouldBe([0.95f, 0.98f, 0.7f]);
        }


        // ================================================================
        // Private methods
        // ================================================================

        private void MigrateTo(string migration)
        {
            using AppDbContext db = _database.Open();
            db.GetService<IMigrator>().Migrate(migration);
        }

        private int SavedFlow()
        {
            using AppDbContext db = _database.Open();

            Flow flow = new Flow { Name = "Firestone", PublicId = Guid.NewGuid() };
            db.Flows.Add(flow);
            db.SaveChanges();

            return flow.Id;
        }

        // The tables are the same before and after, so the model writes the rows; the old mode is
        // set as text, since the enum no longer has it.
        private int SavedSearch(int flowId, string mode, params float[] accuracies)
        {
            using AppDbContext db = _database.Open();

            FlowStep step = new FlowStep
            {
                FlowId = flowId,
                FlowStepType = FlowStepTypeEnum.SEARCH_IMAGE,
                Name = "Find a research to start",
                FlowStepTemplates = accuracies.Select((x, index) => new FlowStepTemplate
                {
                    OrderNumber = index,
                    Accuracy = x,
                    FlowTemplate = new FlowTemplate { FlowId = flowId, Name = $"template-{index}.png" },
                }).ToList(),
            };
            db.FlowSteps.Add(step);
            db.SaveChanges();

            db.Database.ExecuteSqlRaw("UPDATE FlowSteps SET TemplateMatchMode = {0} WHERE Id = {1}", mode, step.Id);

            return step.Id;
        }

        private TemplateMatchModeEnum ModeOf(int stepId)
        {
            using AppDbContext db = _database.Open();
            return db.FlowSteps.Where(x => x.Id == stepId).Select(x => x.TemplateMatchMode).Single();
        }

        private List<float> AccuraciesOf(int stepId)
        {
            using AppDbContext db = _database.Open();
            return db.FlowStepTemplates.Where(x => x.FlowStepId == stepId).OrderBy(x => x.OrderNumber).Select(x => x.Accuracy).ToList();
        }
    }
}
