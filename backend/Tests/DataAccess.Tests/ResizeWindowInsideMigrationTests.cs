using Core.Enums;
using Core.Models.Database;

using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace DataAccess.Tests
{
    public sealed class ResizeWindowInsideMigrationTests : IDisposable
    {
        private const string BEFORE = "20261112215935_DropStageHotkey";
        private const string AFTER = "20261117101522_ResizeWindowInside";

        private readonly TestDatabase _database = new TestDatabase();

        public void Dispose()
        {
            _database.Dispose();
        }

        // Firestone's: 1771 x 956 at 125% left the 1753 x 909 its template images were captured at.
        [Fact]
        public void A_resize_takes_the_inside_most_of_its_area_s_template_images_were_captured_at()
        {
            MigrateTo(BEFORE);
            int flowId = SavedFlow();
            int window = SavedArea(flowId, "Firestone", FlowAreaTypeEnum.APPLICATION, true);
            int region = SavedArea(flowId, "Research missions", FlowAreaTypeEnum.CUSTOM, true);
            int resize = SavedResize(flowId, window, 1771, 956, "Resize window to 1771x956");
            SavedSearch(flowId, window, (1753, 909), (1753, 909), (1749, 900), (1571, 640));
            SavedSearch(flowId, region, (1752, 911), (1752, 911), (1752, 911));
            MigrateTo(AFTER);

            Read(resize).ShouldBe(("Resize window to 1753x909", 1753, 909));
        }

        [Fact]
        public void With_no_template_images_it_takes_the_window_less_the_title_bar_and_borders_at_100_percent()
        {
            MigrateTo(BEFORE);
            int flowId = SavedFlow();
            int window = SavedArea(flowId, "Notepad", FlowAreaTypeEnum.APPLICATION, true);
            int resize = SavedResize(flowId, window, 1296, 759, "Make room for the sidebar");
            MigrateTo(AFTER);

            Read(resize).ShouldBe(("Make room for the sidebar", 1280, 720));
        }

        // Captured before the size was changed, they say nothing about its inside.
        [Fact]
        public void Template_images_captured_at_another_size_are_not_taken_for_its_inside()
        {
            MigrateTo(BEFORE);
            int flowId = SavedFlow();
            int window = SavedArea(flowId, "Game", FlowAreaTypeEnum.APPLICATION, true);
            int resize = SavedResize(flowId, window, 1771, 956, "Resize window to 1771x956");
            SavedSearch(flowId, window, (1280, 720), (1280, 720));
            MigrateTo(AFTER);

            Read(resize).ShouldBe(("Resize window to 1755x917", 1755, 917));
        }

        [Fact]
        public void A_numbered_name_keeps_its_number()
        {
            MigrateTo(BEFORE);
            int flowId = SavedFlow();
            int window = SavedArea(flowId, "Notepad", FlowAreaTypeEnum.APPLICATION, true);
            int resize = SavedResize(flowId, window, 1616, 939, "Resize window to 1616x939 (2)");
            MigrateTo(AFTER);

            Read(resize).ShouldBe(("Resize window to 1600x900 (2)", 1600, 900));
        }

        [Fact]
        public void A_browser_s_window_and_one_whose_area_keeps_its_frame_stay_whole()
        {
            MigrateTo(BEFORE);
            int flowId = SavedFlow();
            int tab = SavedArea(flowId, "Crude oil page", FlowAreaTypeEnum.BROWSER_TAB, true);
            int withFrame = SavedArea(flowId, "Editor", FlowAreaTypeEnum.APPLICATION, false);
            int tabResize = SavedResize(flowId, tab, 1950, 2100, "Resize window to 1950x2100");
            int frameResize = SavedResize(flowId, withFrame, 1366, 768, "Resize window to 1366x768");
            SavedSearch(flowId, withFrame, (1350, 729));
            MigrateTo(AFTER);

            Read(tabResize).ShouldBe(("Resize window to 1950x2100", 1950, 2100));
            Read(frameResize).ShouldBe(("Resize window to 1366x768", 1366, 768));
        }


        // ================================================================
        // Private methods
        // ================================================================

        private void MigrateTo(string migration)
        {
            using AppDbContext db = _database.Open();
            db.GetService<IMigrator>().Migrate(migration);
        }

        // The tables are the same before and after, so the model writes the rows.
        private int SavedFlow()
        {
            using AppDbContext db = _database.Open();

            Flow flow = new Flow { Name = "Firestone", PublicId = Guid.NewGuid() };
            db.Flows.Add(flow);
            db.SaveChanges();

            return flow.Id;
        }

        private int SavedArea(int flowId, string name, FlowAreaTypeEnum type, bool ignoreWindowBorders)
        {
            using AppDbContext db = _database.Open();

            FlowArea area = new FlowArea { FlowId = flowId, Name = name, Type = type, ProcessName = name, IgnoreWindowBorders = ignoreWindowBorders };
            db.FlowAreas.Add(area);
            db.SaveChanges();

            return area.Id;
        }

        private int SavedResize(int flowId, int flowAreaId, int width, int height, string name)
        {
            using AppDbContext db = _database.Open();

            FlowStep step = new FlowStep
            {
                FlowId = flowId,
                RootId = flowId,
                FlowStepType = FlowStepTypeEnum.WINDOW_RESIZE,
                FlowAreaId = flowAreaId,
                Name = name,
                WindowWidth = width,
                WindowHeight = height,
            };
            db.FlowSteps.Add(step);
            db.SaveChanges();

            return step.Id;
        }

        // One template image per size given, each used by the search.
        private void SavedSearch(int flowId, int flowAreaId, params (int Width, int Height)[] captured)
        {
            using AppDbContext db = _database.Open();

            FlowStep step = new FlowStep
            {
                FlowId = flowId,
                RootId = flowId,
                FlowStepType = FlowStepTypeEnum.SEARCH_IMAGE,
                FlowAreaId = flowAreaId,
                Name = $"Find in area {flowAreaId}",
                FlowStepTemplates = captured.Select((x, index) => new FlowStepTemplate
                {
                    OrderNumber = index,
                    Accuracy = 0.8f,
                    FlowTemplate = new FlowTemplate
                    {
                        FlowId = flowId,
                        Name = $"template-{flowAreaId}-{index}.png",
                        AuthoredFlowAreaWidth = x.Width,
                        AuthoredFlowAreaHeight = x.Height,
                    },
                }).ToList(),
            };
            db.FlowSteps.Add(step);
            db.SaveChanges();
        }

        private (string Name, int Width, int Height) Read(int stepId)
        {
            using AppDbContext db = _database.Open();

            FlowStep step = db.FlowSteps.Single(x => x.Id == stepId);
            return (step.Name, step.WindowWidth, step.WindowHeight);
        }
    }
}
