using Business.Flows.DataService;
using Business.FlowScript;
using Business.FlowScript.Diagnostics;
using Business.FlowScript.Scanner;
using Business.Tests.Fakes;
using Business.Tests.FlowScript;
using Core.Enums;
using Core.Helpers;
using Core.Models.Database;
using Core.Models.Dtos;
using Core.Models.Dtos.FlowScript;
using DataAccess;
using static Business.Tests.TestToken;

namespace Business.Tests.Flows
{
    /// <summary>
    /// The rules a change to a flow goes through, the same whether it came from a form or a file.
    /// </summary>
    public sealed class DataServiceTests : IDisposable
    {
        private readonly TestDatabase _database = new TestDatabase();
        private readonly DataService _dataService;

        public DataServiceTests()
        {
            _dataService = TestDataService.For(_database);
        }

        public void Dispose()
        {
            _database.Dispose();
        }

        private async Task<int> NewFlowAsync()
        {
            return (await _dataService.Flow.CreateAsync(new Flow { Name = "Flow" }, Ct)).Data;
        }

        private List<FlowStep> Steps(int flowId)
        {
            using AppDbContext db = _database.CreateDbContext();
            return db.FlowSteps.Where(x => x.RootId == flowId).ToList();
        }

        private List<FlowStepTemplate> Templates(int flowId)
        {
            using AppDbContext db = _database.CreateDbContext();
            return db.FlowStepTemplates.Where(x => x.FlowStep.RootId == flowId).ToList();
        }

        // template-, five of 0-9a-z, then .png.
        private static bool IsTemplateName(string name)
        {
            return name.StartsWith("template-", StringComparison.Ordinal)
                && name.EndsWith(".png", StringComparison.Ordinal)
                && name.Length == 18
                && name[9..14].All(x => char.IsAsciiDigit(x) || char.IsAsciiLetterLower(x));
        }

        private Task<FlowScriptImportResultDto> ImportAsync(string steps)
        {
            string script = "Flow:    Imported\nId:      8f14e45f-ea2b-4c3f-9f1a-77f0d2a3b121\n\nSteps:\n" + steps;
            return new FlowScriptImporter(new Scanner(), _dataService).ImportTextAsync(script, null, Ct);
        }

        // ================================================================
        // From a form
        // ================================================================

        [Fact]
        public async Task A_new_step_takes_a_free_name_and_a_check_gets_its_branch_rows()
        {
            int flowId = await NewFlowAsync();

            await _dataService.FlowStep.CreateAsync(new FlowStep { RootId = flowId, FlowId = flowId, FlowStepType = FlowStepTypeEnum.SEARCH_IMAGE, Name = "Find" }, [], Ct);
            await _dataService.FlowStep.CreateAsync(new FlowStep { RootId = flowId, FlowId = flowId, FlowStepType = FlowStepTypeEnum.SEARCH_IMAGE, Name = "Find", OrderNumber = 1 }, [], Ct);

            List<FlowStep> steps = Steps(flowId);
            steps.Where(x => x.FlowStepType == FlowStepTypeEnum.SEARCH_IMAGE).Select(x => x.Name).Order().ShouldBe(["Find", "Find 2"]);
            steps.Count(x => x.FlowStepType == FlowStepTypeEnum.SUCCESS).ShouldBe(2);
            steps.Count(x => x.FlowStepType == FlowStepTypeEnum.FAILURE).ShouldBe(2);
        }

        // Steps, areas and points are one namespace, so an area cannot take a step's name.
        [Fact]
        public async Task A_new_area_takes_a_name_no_step_has()
        {
            int flowId = await NewFlowAsync();
            await _dataService.FlowStep.CreateAsync(new FlowStep { RootId = flowId, FlowId = flowId, FlowStepType = FlowStepTypeEnum.WAIT, Name = "Browser" }, [], Ct);

            int areaId = (await _dataService.FlowArea.CreateAsync(new FlowArea { FlowId = flowId, Name = "Browser", Type = FlowAreaTypeEnum.MONITOR }, Ct)).Data;

            using AppDbContext db = _database.CreateDbContext();
            db.FlowAreas.Single(x => x.Id == areaId).Name.ShouldBe("Browser 2");
        }

        // Nobody names a template: a new one gets a name no template in the flow has, and keeps it
        // through every save, though the form sends none.
        [Fact]
        public async Task A_new_template_is_named_and_a_save_keeps_the_name()
        {
            int flowId = await NewFlowAsync();
            FlowStep find = new FlowStep { RootId = flowId, FlowId = flowId, FlowStepType = FlowStepTypeEnum.SEARCH_IMAGE, Name = "Find" };
            int stepId = (await _dataService.FlowStep.CreateAsync(find, [new FlowStepTemplateDto(), new FlowStepTemplateDto()], Ct)).Data;

            List<FlowStepTemplate> created = Templates(flowId);
            created.ShouldAllBe(x => IsTemplateName(x.Name));
            created.Select(x => x.Name).Distinct(StringComparer.OrdinalIgnoreCase).Count().ShouldBe(2);

            FlowStepDto dto = new FlowStepDto
            {
                Id = stepId,
                RootId = flowId,
                FlowId = flowId,
                FlowStepType = FlowStepTypeEnum.SEARCH_IMAGE,
                Name = "Find",
                FlowStepTemplates = created.Select(x => new FlowStepTemplateDto { Id = x.Id }).Append(new FlowStepTemplateDto()).ToList(),
            };
            (await _dataService.FlowStep.UpdateAsync(dto, Ct)).IsSuccess.ShouldBeTrue();

            List<FlowStepTemplate> saved = Templates(flowId);
            saved.Where(x => created.Any(y => y.Id == x.Id)).Select(x => x.Name).ShouldBe(created.Select(x => x.Name), ignoreOrder: true);
            saved.Select(x => x.Name).Distinct(StringComparer.OrdinalIgnoreCase).Count().ShouldBe(3);
        }

        [Fact]
        public async Task A_refused_move_changes_nothing()
        {
            int flowId = await NewFlowAsync();
            int stepId = (await _dataService.FlowStep.CreateAsync(new FlowStep { RootId = flowId, FlowId = flowId, FlowStepType = FlowStepTypeEnum.LOOP, Name = "Loop" }, [], Ct)).Data;

            ResultDto<bool> moved = await _dataService.FlowStep.MoveAsync(new FlowStepMoveDto { FlowStepId = stepId, TargetParentFlowStepId = stepId }, Ct);

            moved.IsSuccess.ShouldBeFalse();
            Steps(flowId).Single().ParentFlowStepId.ShouldBeNull();
        }

        // ================================================================
        // A flow's name
        // ================================================================

        // The name is the file it exports to, and on Windows and macOS these would be one file.
        [Fact]
        public async Task A_new_flow_cannot_take_another_flows_name_whatever_the_case()
        {
            await _dataService.Flow.CreateAsync(new Flow { Name = "Login" }, Ct);

            ResultDto<int> created = await _dataService.Flow.CreateAsync(new Flow { Name = "login" }, Ct);

            created.IsSuccess.ShouldBeFalse();
            created.ErrorMessage.ShouldNotBeNull().ShouldContain("\"Login\"");
        }

        [Fact]
        public async Task A_flow_cannot_be_renamed_to_another_flows_name()
        {
            await _dataService.Flow.CreateAsync(new Flow { Name = "Login" }, Ct);
            int flowId = (await _dataService.Flow.CreateAsync(new Flow { Name = "Checkout" }, Ct)).Data;

            ResultDto<Flow> updated = await _dataService.Flow.UpdateAsync(new FlowDto { Id = flowId, Name = "LOGIN" }, Ct);

            updated.IsSuccess.ShouldBeFalse();
        }

        [Fact]
        public async Task A_flow_is_not_a_clash_with_itself()
        {
            int flowId = (await _dataService.Flow.CreateAsync(new Flow { Name = "Login" }, Ct)).Data;

            ResultDto<Flow> updated = await _dataService.Flow.UpdateAsync(new FlowDto { Id = flowId, Name = "LOGIN" }, Ct);

            updated.IsSuccess.ShouldBeTrue(updated.ErrorMessage);
        }

        // ================================================================
        // A tree of new steps - a recording
        // ================================================================

        // Linked the way the draft handler links them: a click under the search's Success row, reading
        // the search, with a point it creates. Every name is already taken once.
        [Fact]
        public async Task A_tree_of_new_steps_is_linked_named_apart_and_placed_at_its_index()
        {
            int flowId = await NewFlowAsync();
            await _dataService.FlowStep.CreateAsync(new FlowStep { RootId = flowId, FlowId = flowId, FlowStepType = FlowStepTypeEnum.WAIT, Name = "Find" }, [], Ct);

            FlowStep find = new FlowStep { FlowStepType = FlowStepTypeEnum.SEARCH_IMAGE, Name = "Find" };
            FlowStep success = TreeStepHelper.CreateBranchChildren(find).First(x => x.FlowStepType == FlowStepTypeEnum.SUCCESS);
            FlowStep click = new FlowStep { FlowStepType = FlowStepTypeEnum.CURSOR_CLICK, Name = "Click", ParentFlowStep = success, FlowStepReference = find, FlowPoint = new FlowPoint { Name = "Find" } };

            ResultDto<int> created = await _dataService.FlowStep.CreateTreeAsync(flowId, null, 0, [find, success, click], Ct);

            created.Data.ShouldBe(flowId);
            using AppDbContext db = _database.CreateDbContext();
            List<FlowStep> steps = db.FlowSteps.Where(x => x.RootId == flowId).ToList();
            FlowStep savedFind = steps.Single(x => x.FlowStepType == FlowStepTypeEnum.SEARCH_IMAGE);
            FlowStep savedClick = steps.Single(x => x.FlowStepType == FlowStepTypeEnum.CURSOR_CLICK);

            savedFind.Name.ShouldBe("Find 2");
            db.FlowPoints.Single(x => x.Id == savedClick.FlowPointId).Name.ShouldBe("Find 3");
            savedClick.FlowStepReferenceId.ShouldBe(savedFind.Id);

            steps.Where(x => x.ParentFlowStepId == savedFind.Id).OrderBy(x => x.OrderNumber).Select(x => x.FlowStepType)
                .ShouldBe([FlowStepTypeEnum.SUCCESS, FlowStepTypeEnum.FAILURE]);
            savedClick.ParentFlowStepId.ShouldBe(steps.Single(x => x.ParentFlowStepId == savedFind.Id && x.FlowStepType == FlowStepTypeEnum.SUCCESS).Id);

            steps.Where(x => x.ParentFlowStepId == null).OrderBy(x => x.OrderNumber).Select(x => x.Name).ShouldBe(["Find 2", "Find"]);
        }

        // A recorded template comes with no name, a pasted one with the name of the one it copies.
        [Fact]
        public async Task A_tree_of_new_steps_names_its_templates_apart_from_the_flows()
        {
            int flowId = await NewFlowAsync();
            await _dataService.FlowStep.CreateAsync(new FlowStep { RootId = flowId, FlowId = flowId, FlowStepType = FlowStepTypeEnum.SEARCH_IMAGE, Name = "Find" }, [new FlowStepTemplateDto()], Ct);
            string existing = Templates(flowId).Single().Name;

            FlowStep find = new FlowStep { FlowStepType = FlowStepTypeEnum.SEARCH_IMAGE, Name = "Find again", FlowStepTemplates = [new FlowStepTemplate { Name = existing }, new FlowStepTemplate()] };
            (await _dataService.FlowStep.CreateTreeAsync(flowId, null, 1, [find], Ct)).IsSuccess.ShouldBeTrue();

            List<FlowStepTemplate> templates = Templates(flowId);
            templates.ShouldAllBe(x => IsTemplateName(x.Name));
            templates.Select(x => x.Name).Distinct(StringComparer.OrdinalIgnoreCase).Count().ShouldBe(3);
        }

        [Fact]
        public async Task A_tree_refused_for_its_target_writes_nothing()
        {
            int flowId = await NewFlowAsync();
            int searchId = (await _dataService.FlowStep.CreateAsync(new FlowStep { RootId = flowId, FlowId = flowId, FlowStepType = FlowStepTypeEnum.SEARCH_IMAGE, Name = "Search" }, [], Ct)).Data;

            ResultDto<int> created = await _dataService.FlowStep.CreateTreeAsync(null, searchId, 0, [new FlowStep { FlowStepType = FlowStepTypeEnum.WAIT, Name = "Wait" }], Ct);

            created.ErrorMessage.ShouldBe("\"Search\" holds steps in its branches, not directly.");
            Steps(flowId).ShouldNotContain(x => x.Name == "Wait");
        }

        // ================================================================
        // From a file
        // ================================================================

        // The printer leaves an empty branch out, and a check imported without one would have no
        // row to add steps under.
        [Fact]
        public async Task An_import_gives_a_check_the_branch_its_file_left_out()
        {
            FlowScriptImportResultDto imported = await ImportAsync("Find Image  <[ Find ]>\n Failure:\n  Wait  800ms\n");

            List<FlowStep> steps = Steps(imported.FlowId);
            FlowStep find = steps.Single(x => x.Name == "Find");

            steps.Where(x => x.ParentFlowStepId == find.Id).OrderBy(x => x.OrderNumber).Select(x => x.FlowStepType)
                .ShouldBe([FlowStepTypeEnum.SUCCESS, FlowStepTypeEnum.FAILURE]);
            steps.Single(x => x.FlowStepType == FlowStepTypeEnum.WAIT).ParentFlowStepId.ShouldBe(steps.Single(x => x.FlowStepType == FlowStepTypeEnum.FAILURE).Id);
        }

        // Validated once saved, the same as after a form: the errors are reported and the flow stays.
        [Fact]
        public async Task An_import_with_errors_is_saved_and_says_what_they_are()
        {
            FlowScriptImportResultDto imported = await ImportAsync("Find Image  <[ Find ]>\n");

            imported.IsSuccess.ShouldBeTrue();
            imported.Validation.ShouldNotBeNull();
            imported.Validation.Issues.Select(x => x.Code).ShouldContain(FlowValidationCodeEnum.NO_TEMPLATES);
            Steps(imported.FlowId).ShouldContain(x => x.Name == "Find");
        }

        // A different flow, by its Id, so the import would leave two flows exporting to one file.
        [Fact]
        public async Task An_import_cannot_take_another_flows_name()
        {
            await _dataService.Flow.CreateAsync(new Flow { Name = "imported" }, Ct);

            FlowScriptImportResultDto imported = await ImportAsync("Wait  800ms\n");

            imported.IsSuccess.ShouldBeFalse();
            imported.Errors.ShouldHaveSingleItem().Code.ShouldBe(nameof(DiagnosticCodeEnum.FLOW_NAME_TAKEN));
        }

        // ================================================================
        // Extracting a sub-flow
        // ================================================================

        // A click on a point measured from Header, inside Browser - an area no step searches in.
        private async Task<(int FlowId, int SubFlowId, int ClickId)> ExtractClickAsync()
        {
            int flowId = await NewFlowAsync();
            int clickId;

            using (AppDbContext db = _database.CreateDbContext())
            {
                FlowArea browser = new FlowArea { FlowId = flowId, Name = "Browser", Type = FlowAreaTypeEnum.APPLICATION, ProcessName = "chrome.exe", TitlePattern = "Swag", UseClientArea = false, ScalesWith = ScalesWithEnum.DPI, AuthoredDpi = 120 };
                FlowArea header = new FlowArea { FlowId = flowId, Name = "Header", Type = FlowAreaTypeEnum.CUSTOM, ParentFlowArea = browser, SizingMode = AreaSizingModeEnum.RATIO, RatioWidth = 1f, RatioHeight = 0.1f };
                FlowPoint button = new FlowPoint { FlowId = flowId, Name = "Button", FlowArea = header, OffsetMode = AreaSizingModeEnum.RATIO, RatioX = 0.9f, RatioY = 0.5f, AuthoredDpi = 120 };
                FlowStep click = new FlowStep { RootId = flowId, FlowId = flowId, FlowStepType = FlowStepTypeEnum.CURSOR_CLICK, Name = "Click", FlowPoint = button };

                db.AddRange(browser, header, button, click);
                db.SaveChanges();
                clickId = click.Id;
            }

            ResultDto<ExtractSubFlowResultDto> extracted = await _dataService.Flow.ExtractSubFlowAsync(
                new ExtractSubFlowDto { FlowStepId = clickId, Name = "Press the button", SourceRootId = flowId, SourceFlowId = flowId, SourceOrderNumber = 0 }, Ct);

            extracted.IsSuccess.ShouldBeTrue(extracted.ErrorMessage);
            return (flowId, extracted.Data!.SubFlowId, clickId);
        }

        [Fact]
        public async Task An_extraction_cannot_take_another_flows_name()
        {
            int flowId = await NewFlowAsync();
            int waitId = (await _dataService.FlowStep.CreateAsync(new FlowStep { RootId = flowId, FlowId = flowId, FlowStepType = FlowStepTypeEnum.WAIT, Name = "Wait" }, [], Ct)).Data;

            ResultDto<ExtractSubFlowResultDto> extracted = await _dataService.Flow.ExtractSubFlowAsync(
                new ExtractSubFlowDto { FlowStepId = waitId, Name = "FLOW", SourceRootId = flowId, SourceFlowId = flowId, SourceOrderNumber = 0 }, Ct);

            extracted.IsSuccess.ShouldBeFalse();
            Steps(flowId).ShouldHaveSingleItem().Id.ShouldBe(waitId);
        }

        // The point's own area and every area above it come along, or the copy would be measured
        // from nothing and land in screen coordinates.
        [Fact]
        public async Task An_extracted_step_takes_copies_of_its_point_and_every_area_above_it()
        {
            (int flowId, int subFlowId, int clickId) = await ExtractClickAsync();

            using AppDbContext db = _database.CreateDbContext();
            List<FlowArea> areas = db.FlowAreas.Where(x => x.FlowId == subFlowId).ToList();
            FlowPoint button = db.FlowPoints.Single(x => x.FlowId == subFlowId);

            db.FlowSteps.Single(x => x.Id == clickId).FlowPointId.ShouldBe(button.Id);
            button.FlowAreaId.ShouldBe(areas.Single(x => x.Name == "Header").Id);
            areas.Single(x => x.Name == "Header").ParentFlowAreaId.ShouldBe(areas.Single(x => x.Name == "Browser").Id);

            db.FlowAreas.Count(x => x.FlowId == flowId).ShouldBe(2);
            db.FlowPoints.Count(x => x.FlowId == flowId).ShouldBe(1);
        }

        // Copied whole, so a column added later cannot be left behind.
        [Fact]
        public async Task An_extracted_area_and_point_keep_every_column()
        {
            (int flowId, int subFlowId, _) = await ExtractClickAsync();

            using AppDbContext db = _database.CreateDbContext();

            foreach (FlowArea original in db.FlowAreas.Where(x => x.FlowId == flowId).ToList())
                ScriptRows.Differences(original, db.FlowAreas.Single(x => x.FlowId == subFlowId && x.Name == original.Name)).ShouldBeEmpty(original.Name);

            ScriptRows.Differences(db.FlowPoints.Single(x => x.FlowId == flowId), db.FlowPoints.Single(x => x.FlowId == subFlowId)).ShouldBeEmpty();
        }
    }
}
