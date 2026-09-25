using Core.Enums;
using Core.Models.Database;

using Microsoft.EntityFrameworkCore;

namespace DataAccess.Tests
{
    /// <summary>
    /// What the database itself guarantees: the migrations build it, they match the model, and a
    /// delete does what the docs promise. Deletes go through ExecuteDelete, straight to SQLite, so
    /// it is the database's foreign keys doing the work and not EF's change tracker.
    /// </summary>
    public sealed class SchemaTests : IDisposable
    {
        private readonly TestDatabase _database = new TestDatabase();

        private static CancellationToken Ct
        {
            get { return TestContext.Current.CancellationToken; }
        }

        public void Dispose()
        {
            _database.Dispose();
        }

        // ================================================================
        // Migrations
        // ================================================================

        [Fact]
        public void Every_migration_applies_to_an_empty_database()
        {
            using AppDbContext db = _database.Open();

            db.Database.GetPendingMigrations().ShouldBeEmpty();
            db.Database.GetAppliedMigrations().ShouldBe(db.Database.GetMigrations());
        }

        // An entity changed and nobody added a migration: the app would start against a schema
        // that is missing a column.
        [Fact]
        public void The_model_has_no_change_without_a_migration()
        {
            using AppDbContext db = _database.Open();

            db.Database.HasPendingModelChanges().ShouldBeFalse();
        }

        // ================================================================
        // Deletes
        // ================================================================

        private sealed record Seeded(int FlowId, int AreaId, int ChildAreaId, int PointId, int SearchId, int ClickId, int TemplateId, int ExecutionStepId);

        private async Task<Seeded> SeedAsync()
        {
            await using AppDbContext db = _database.Open();

            Flow flow = new Flow { Name = "Login", PublicId = Guid.NewGuid() };
            db.Flows.Add(flow);
            await db.SaveChangesAsync(Ct);

            FlowArea area = new FlowArea { FlowId = flow.Id, Name = "Browser", Type = FlowAreaTypeEnum.APPLICATION };
            db.FlowAreas.Add(area);
            await db.SaveChangesAsync(Ct);

            FlowArea child = new FlowArea { FlowId = flow.Id, Name = "Header", Type = FlowAreaTypeEnum.CUSTOM, ParentFlowAreaId = area.Id };
            FlowPoint point = new FlowPoint { FlowId = flow.Id, Name = "Menu", FlowAreaId = area.Id };
            db.FlowAreas.Add(child);
            db.FlowPoints.Add(point);
            await db.SaveChangesAsync(Ct);

            FlowStep search = new FlowStep { FlowId = flow.Id, RootId = flow.Id, Name = "Find", FlowStepType = FlowStepTypeEnum.SEARCH_IMAGE, FlowAreaId = area.Id };
            db.FlowSteps.Add(search);
            await db.SaveChangesAsync(Ct);

            FlowStep click = new FlowStep { RootId = flow.Id, ParentFlowStepId = search.Id, FlowStepType = FlowStepTypeEnum.CURSOR_CLICK, FlowPointId = point.Id, FlowPointEndId = point.Id, FlowStepReferenceId = search.Id };
            FlowStepTemplate template = new FlowStepTemplate { FlowStepId = search.Id, Name = "button" };
            db.FlowSteps.Add(click);
            db.FlowStepTemplates.Add(template);
            await db.SaveChangesAsync(Ct);

            Execution execution = new Execution { FlowId = flow.Id };
            db.Executions.Add(execution);
            await db.SaveChangesAsync(Ct);

            ExecutionStep ran = new ExecutionStep { ExecutionId = execution.Id, FlowStepId = search.Id, Name = "Find", FlowStepType = FlowStepTypeEnum.SEARCH_IMAGE };
            db.ExecutionSteps.Add(ran);
            await db.SaveChangesAsync(Ct);

            return new Seeded(flow.Id, area.Id, child.Id, point.Id, search.Id, click.Id, template.Id, ran.Id);
        }

        [Fact]
        public async Task Deleting_an_area_clears_it_from_the_steps_and_keeps_the_steps()
        {
            Seeded seeded = await SeedAsync();
            await using AppDbContext db = _database.Open();

            await db.FlowAreas.Where(x => x.Id == seeded.AreaId).ExecuteDeleteAsync(Ct);

            FlowStep search = await db.FlowSteps.AsNoTracking().SingleAsync(x => x.Id == seeded.SearchId, Ct);
            search.FlowAreaId.ShouldBeNull();
        }

        [Fact]
        public async Task Deleting_an_area_detaches_the_regions_inside_it_rather_than_deleting_them()
        {
            Seeded seeded = await SeedAsync();
            await using AppDbContext db = _database.Open();

            await db.FlowAreas.Where(x => x.Id == seeded.AreaId).ExecuteDeleteAsync(Ct);

            FlowArea child = await db.FlowAreas.AsNoTracking().SingleAsync(x => x.Id == seeded.ChildAreaId, Ct);
            child.ParentFlowAreaId.ShouldBeNull();
        }

        [Fact]
        public async Task Deleting_a_point_clears_both_ends_of_the_steps_that_used_it()
        {
            Seeded seeded = await SeedAsync();
            await using AppDbContext db = _database.Open();

            await db.FlowPoints.Where(x => x.Id == seeded.PointId).ExecuteDeleteAsync(Ct);

            FlowStep click = await db.FlowSteps.AsNoTracking().SingleAsync(x => x.Id == seeded.ClickId, Ct);
            (click.FlowPointId, click.FlowPointEndId).ShouldBe((null, null));
        }

        [Fact]
        public async Task Deleting_a_step_takes_its_children_and_templates_with_it()
        {
            Seeded seeded = await SeedAsync();
            await using AppDbContext db = _database.Open();

            await db.FlowSteps.Where(x => x.Id == seeded.SearchId).ExecuteDeleteAsync(Ct);

            (await db.FlowSteps.AnyAsync(x => x.Id == seeded.ClickId, Ct)).ShouldBeFalse();
            (await db.FlowStepTemplates.AnyAsync(x => x.Id == seeded.TemplateId, Ct)).ShouldBeFalse();
        }

        // History is keyed on the step's name, so a re-import keeps the trend as long as the name does.
        [Fact]
        public async Task Deleting_a_step_keeps_the_history_of_what_it_did()
        {
            Seeded seeded = await SeedAsync();
            await using AppDbContext db = _database.Open();

            await db.FlowSteps.Where(x => x.Id == seeded.SearchId).ExecuteDeleteAsync(Ct);

            ExecutionStep ran = await db.ExecutionSteps.AsNoTracking().SingleAsync(x => x.Id == seeded.ExecutionStepId, Ct);
            ran.FlowStepId.ShouldBeNull();
            ran.Name.ShouldBe("Find");
        }
    }
}
