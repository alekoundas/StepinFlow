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
using Microsoft.EntityFrameworkCore;
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

        private async Task<int> NewFlowAsync(string name = "Flow")
        {
            return (await _dataService.Flow.CreateAsync(new Flow { Name = name }, Ct)).Data;
        }

        private List<FlowStep> Steps(int flowId)
        {
            using AppDbContext db = _database.CreateDbContext();
            return db.FlowSteps.Where(x => x.RootId == flowId).ToList();
        }

        private List<FlowStepTemplate> Templates(int flowId)
        {
            using AppDbContext db = _database.CreateDbContext();
            return db.FlowStepTemplates.Include(x => x.FlowTemplate).Where(x => x.FlowStep.RootId == flowId).ToList();
        }

        private List<FlowTemplate> Pictures(int flowId)
        {
            using AppDbContext db = _database.CreateDbContext();
            return db.FlowTemplates.Where(x => x.FlowId == flowId).ToList();
        }

        // A search step with one new picture, and the id of that picture.
        private async Task<(int StepId, int PictureId)> SearchWithPictureAsync(int flowId, string name, byte[] image)
        {
            FlowStep find = new FlowStep { RootId = flowId, FlowId = flowId, FlowStepType = FlowStepTypeEnum.SEARCH_IMAGE, Name = name };
            FlowStepTemplateDto template = new FlowStepTemplateDto { FlowTemplate = new FlowTemplateDto { TemplateImage = image, ClickOffsetX = 5, ClickOffsetY = 6 } };
            int stepId = (await _dataService.FlowStep.CreateAsync(find, [template], Ct)).Data;

            return (stepId, Templates(flowId).Single(x => x.FlowStepId == stepId).FlowTemplateId);
        }

        // The step's form as it saves: its own fields and its links.
        private static FlowStepDto SearchDto(int flowId, int stepId, string name, params FlowStepTemplateDto[] templates)
        {
            return new FlowStepDto { Id = stepId, RootId = flowId, FlowId = flowId, FlowStepType = FlowStepTypeEnum.SEARCH_IMAGE, Name = name, FlowStepTemplates = templates.ToList() };
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
            return new FlowScriptImporter(new Scanner(), _dataService).ImportTextAsync(script, new Dictionary<string, byte[]>(), Ct);
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
            await _dataService.FlowStep.CreateAsync(new FlowStep { RootId = flowId, FlowId = flowId, FlowStepType = FlowStepTypeEnum.LOOP, Name = "Browser" }, [], Ct);

            int areaId = (await _dataService.FlowArea.CreateAsync(new FlowArea { FlowId = flowId, Name = "Browser", Type = FlowAreaTypeEnum.MONITOR }, Ct)).Data;

            using AppDbContext db = _database.CreateDbContext();
            db.FlowAreas.Single(x => x.Id == areaId).Name.ShouldBe("Browser 2");
        }

        // Areas and points are saved from their own tabs, so the flow form's save leaves them as
        // they are, whatever lists it sends.
        [Fact]
        public async Task The_flow_form_leaves_areas_and_points_as_they_are()
        {
            int flowId = await NewFlowAsync();
            int areaId = (await _dataService.FlowArea.CreateAsync(new FlowArea { FlowId = flowId, Name = "Browser", Type = FlowAreaTypeEnum.MONITOR }, Ct)).Data;
            await _dataService.FlowPoint.CreateAsync(new FlowPoint { FlowId = flowId, Name = "Origin", FlowAreaId = areaId }, Ct);

            ResultDto<Flow> updated = await _dataService.Flow.UpdateAsync(new FlowDto { Id = flowId, Name = "Renamed" }, Ct);

            updated.IsSuccess.ShouldBeTrue();
            using AppDbContext db = _database.CreateDbContext();
            db.FlowAreas.Count(x => x.FlowId == flowId).ShouldBe(1);
            db.FlowPoints.Single(x => x.FlowId == flowId).FlowAreaId.ShouldBe(areaId);
        }

        // Areas go one level deep, from every form that gives an area a parent.
        [Fact]
        public async Task A_new_area_cannot_go_inside_one_already_inside_another()
        {
            int flowId = await NewFlowAsync();
            int browserId = (await _dataService.FlowArea.CreateAsync(new FlowArea { FlowId = flowId, Name = "Browser", Type = FlowAreaTypeEnum.MONITOR }, Ct)).Data;
            int headerId = (await _dataService.FlowArea.CreateAsync(new FlowArea { FlowId = flowId, Name = "Header", Type = FlowAreaTypeEnum.CUSTOM, ParentFlowAreaId = browserId }, Ct)).Data;

            ResultDto<int> created = await _dataService.FlowArea.CreateAsync(new FlowArea { FlowId = flowId, Name = "Logo", Type = FlowAreaTypeEnum.CUSTOM, ParentFlowAreaId = headerId }, Ct);

            created.IsSuccess.ShouldBeFalse();
        }

        [Fact]
        public async Task An_area_with_areas_inside_it_cannot_go_inside_another()
        {
            int flowId = await NewFlowAsync();
            int screenId = (await _dataService.FlowArea.CreateAsync(new FlowArea { FlowId = flowId, Name = "Screen", Type = FlowAreaTypeEnum.MONITOR }, Ct)).Data;
            int boxId = (await _dataService.FlowArea.CreateAsync(new FlowArea { FlowId = flowId, Name = "Box", Type = FlowAreaTypeEnum.CUSTOM }, Ct)).Data;
            await _dataService.FlowArea.CreateAsync(new FlowArea { FlowId = flowId, Name = "Inner", Type = FlowAreaTypeEnum.CUSTOM, ParentFlowAreaId = boxId }, Ct);

            ResultDto<FlowArea> updated = await _dataService.FlowArea.UpdateAsync(new FlowAreaDto { Id = boxId, FlowId = flowId, Name = "Box", Type = FlowAreaTypeEnum.CUSTOM, ParentFlowAreaId = screenId }, Ct);

            updated.IsSuccess.ShouldBeFalse();
            using AppDbContext db = _database.CreateDbContext();
            db.FlowAreas.Single(x => x.Id == boxId).ParentFlowAreaId.ShouldBeNull();
        }

        // A flow works in one window, and the main area is that window, never a part of one.
        [Fact]
        public async Task A_new_area_cannot_be_main_when_another_is()
        {
            int flowId = await NewFlowAsync();
            await _dataService.FlowArea.CreateAsync(new FlowArea { FlowId = flowId, Name = "Browser", Type = FlowAreaTypeEnum.APPLICATION, IsMain = true }, Ct);

            ResultDto<int> created = await _dataService.FlowArea.CreateAsync(new FlowArea { FlowId = flowId, Name = "Editor", Type = FlowAreaTypeEnum.APPLICATION, IsMain = true }, Ct);

            created.IsSuccess.ShouldBeFalse();
        }

        [Fact]
        public async Task An_area_inside_another_cannot_be_made_main()
        {
            int flowId = await NewFlowAsync();
            int browserId = (await _dataService.FlowArea.CreateAsync(new FlowArea { FlowId = flowId, Name = "Browser", Type = FlowAreaTypeEnum.APPLICATION }, Ct)).Data;
            int headerId = (await _dataService.FlowArea.CreateAsync(new FlowArea { FlowId = flowId, Name = "Header", Type = FlowAreaTypeEnum.CUSTOM, ParentFlowAreaId = browserId }, Ct)).Data;

            ResultDto<FlowArea> updated = await _dataService.FlowArea.UpdateAsync(new FlowAreaDto { Id = headerId, FlowId = flowId, Name = "Header", Type = FlowAreaTypeEnum.CUSTOM, ParentFlowAreaId = browserId, IsMain = true }, Ct);

            updated.IsSuccess.ShouldBeFalse();
            using AppDbContext db = _database.CreateDbContext();
            db.FlowAreas.Single(x => x.Id == headerId).IsMain.ShouldBeFalse();
        }

        [Fact]
        public async Task The_main_area_can_be_saved_again_as_main()
        {
            int flowId = await NewFlowAsync();
            int browserId = (await _dataService.FlowArea.CreateAsync(new FlowArea { FlowId = flowId, Name = "Browser", Type = FlowAreaTypeEnum.APPLICATION, IsMain = true }, Ct)).Data;

            ResultDto<FlowArea> updated = await _dataService.FlowArea.UpdateAsync(new FlowAreaDto { Id = browserId, FlowId = flowId, Name = "Browser 2", Type = FlowAreaTypeEnum.APPLICATION, IsMain = true }, Ct);

            updated.IsSuccess.ShouldBeTrue();
        }

        // Inputs share the one namespace too: a step types {{email}} and has to know what it means.
        [Fact]
        public async Task A_new_input_takes_a_name_no_area_has()
        {
            int flowId = await NewFlowAsync();
            await _dataService.FlowArea.CreateAsync(new FlowArea { FlowId = flowId, Name = "email", Type = FlowAreaTypeEnum.MONITOR }, Ct);

            int inputId = (await _dataService.FlowCsvColumn.CreateAsync(new FlowCsvColumn { FlowId = flowId, Name = "email" }, Ct)).Data;

            using AppDbContext db = _database.CreateDbContext();
            db.FlowCsvColumns.Single(x => x.Id == inputId).Name.ShouldBe("email 2");
        }

        // What was typed for a secret is never stored, whichever form sends it.
        [Fact]
        public async Task A_secret_input_keeps_no_value()
        {
            int flowId = await NewFlowAsync();
            int inputId = (await _dataService.FlowCsvColumn.CreateAsync(new FlowCsvColumn { FlowId = flowId, Name = "password", DefaultValue = "hunter2" }, Ct)).Data;

            await _dataService.FlowCsvColumn.UpdateAsync(new FlowCsvColumnDto { Id = inputId, FlowId = flowId, Name = "password", DefaultValue = "hunter2", IsSecret = true }, Ct);

            using AppDbContext db = _database.CreateDbContext();
            FlowCsvColumn saved = db.FlowCsvColumns.Single(x => x.Id == inputId);
            saved.IsSecret.ShouldBeTrue();
            saved.DefaultValue.ShouldBeEmpty();
        }

        // Its own name is not in its way, so saving an input unchanged keeps its name.
        [Fact]
        public async Task An_input_saved_under_its_own_name_keeps_it()
        {
            int flowId = await NewFlowAsync();
            int inputId = (await _dataService.FlowCsvColumn.CreateAsync(new FlowCsvColumn { FlowId = flowId, Name = "email" }, Ct)).Data;

            ResultDto<FlowCsvColumn> updated = await _dataService.FlowCsvColumn.UpdateAsync(new FlowCsvColumnDto { Id = inputId, FlowId = flowId, Name = "email", DefaultValue = "ops@example.com" }, Ct);

            updated.Data!.Name.ShouldBe("email");
            updated.Data.DefaultValue.ShouldBe("ops@example.com");
        }

        // Nobody names a template: a new one gets a name no template in the flow has, and keeps it
        // through every save, though the form sends none.
        [Fact]
        public async Task A_new_template_is_named_and_a_save_keeps_the_name()
        {
            int flowId = await NewFlowAsync();
            FlowStep find = new FlowStep { RootId = flowId, FlowId = flowId, FlowStepType = FlowStepTypeEnum.SEARCH_IMAGE, Name = "Find" };
            int stepId = (await _dataService.FlowStep.CreateAsync(find, [new FlowStepTemplateDto { FlowTemplate = new FlowTemplateDto() }, new FlowStepTemplateDto { FlowTemplate = new FlowTemplateDto() }], Ct)).Data;

            List<FlowStepTemplate> created = Templates(flowId);
            created.ShouldAllBe(x => IsTemplateName(x.FlowTemplate.Name));
            created.Select(x => x.FlowTemplate.Name).Distinct(StringComparer.OrdinalIgnoreCase).Count().ShouldBe(2);

            FlowStepDto dto = new FlowStepDto
            {
                Id = stepId,
                RootId = flowId,
                FlowId = flowId,
                FlowStepType = FlowStepTypeEnum.SEARCH_IMAGE,
                Name = "Find",
                FlowStepTemplates = created.Select(x => new FlowStepTemplateDto { Id = x.Id, FlowTemplateId = x.FlowTemplateId }).Append(new FlowStepTemplateDto { FlowTemplate = new FlowTemplateDto() }).ToList(),
            };
            (await _dataService.FlowStep.UpdateAsync(dto, Ct)).IsSuccess.ShouldBeTrue();

            List<FlowStepTemplate> saved = Templates(flowId);
            saved.Where(x => created.Any(y => y.Id == x.Id)).Select(x => x.FlowTemplate.Name).ShouldBe(created.Select(x => x.FlowTemplate.Name), ignoreOrder: true);
            saved.Select(x => x.FlowTemplate.Name).Distinct(StringComparer.OrdinalIgnoreCase).Count().ShouldBe(3);
        }

        // The point of a flow's templates: one picture, many steps, and a fix to it made once.
        [Fact]
        public async Task Two_steps_link_one_picture_and_an_edit_through_either_reaches_both()
        {
            int flowId = await NewFlowAsync();
            (int firstId, int pictureId) = await SearchWithPictureAsync(flowId, "Find", [1, 2, 3]);
            FlowStep second = new FlowStep { RootId = flowId, FlowId = flowId, FlowStepType = FlowStepTypeEnum.SEARCH_IMAGE, Name = "Find again" };
            int secondId = (await _dataService.FlowStep.CreateAsync(second, [new FlowStepTemplateDto { FlowTemplateId = pictureId, Accuracy = 0.9f }], Ct)).Data;

            FlowStepTemplate link = Templates(flowId).Single(x => x.FlowStepId == secondId);
            FlowStepTemplateDto moved = new FlowStepTemplateDto { Id = link.Id, FlowTemplateId = pictureId, Accuracy = 0.9f, FlowTemplate = new FlowTemplateDto { Id = pictureId, ClickOffsetX = 40, ClickOffsetY = 2 } };
            (await _dataService.FlowStep.UpdateAsync(SearchDto(flowId, secondId, "Find again", moved), Ct)).IsSuccess.ShouldBeTrue();

            FlowTemplate picture = Pictures(flowId).ShouldHaveSingleItem();
            (picture.ClickOffsetX, picture.ClickOffsetY).ShouldBe((40, 2));
            picture.TemplateImage.ShouldBe([1, 2, 3]);
            Templates(flowId).Where(x => x.FlowStepId == firstId || x.FlowStepId == secondId).ShouldAllBe(x => x.FlowTemplateId == pictureId);
            Templates(flowId).Single(x => x.FlowStepId == firstId).Accuracy.ShouldBe(0.8f);
        }

        // A link with no id brings a picture of its own, so one step can change what it looks for
        // without the others noticing.
        [Fact]
        public async Task A_copy_of_a_shared_picture_is_the_steps_alone()
        {
            int flowId = await NewFlowAsync();
            (int firstId, int pictureId) = await SearchWithPictureAsync(flowId, "Find", [1, 2, 3]);
            FlowStep second = new FlowStep { RootId = flowId, FlowId = flowId, FlowStepType = FlowStepTypeEnum.SEARCH_IMAGE, Name = "Find again" };
            int secondId = (await _dataService.FlowStep.CreateAsync(second, [new FlowStepTemplateDto { FlowTemplateId = pictureId }], Ct)).Data;

            FlowStepTemplate link = Templates(flowId).Single(x => x.FlowStepId == secondId);
            FlowStepTemplateDto copied = new FlowStepTemplateDto { Id = link.Id, FlowTemplate = new FlowTemplateDto { TemplateImage = [9, 9] } };
            (await _dataService.FlowStep.UpdateAsync(SearchDto(flowId, secondId, "Find again", copied), Ct)).IsSuccess.ShouldBeTrue();

            List<FlowStepTemplate> links = Templates(flowId);
            links.Single(x => x.FlowStepId == firstId).FlowTemplate.TemplateImage.ShouldBe([1, 2, 3]);
            links.Single(x => x.FlowStepId == secondId).FlowTemplate.TemplateImage.ShouldBe([9, 9]);
            links.Single(x => x.FlowStepId == secondId).Id.ShouldBe(link.Id);
            Pictures(flowId).Count.ShouldBe(2);
        }

        // So the flow, its script and its folder hold the same pictures.
        [Fact]
        public async Task A_picture_goes_when_the_last_step_stops_using_it()
        {
            int flowId = await NewFlowAsync();
            (int firstId, int pictureId) = await SearchWithPictureAsync(flowId, "Find", [1, 2, 3]);
            FlowStep second = new FlowStep { RootId = flowId, FlowId = flowId, FlowStepType = FlowStepTypeEnum.SEARCH_IMAGE, Name = "Find again" };
            int secondId = (await _dataService.FlowStep.CreateAsync(second, [new FlowStepTemplateDto { FlowTemplateId = pictureId }], Ct)).Data;

            (await _dataService.FlowStep.UpdateAsync(SearchDto(flowId, firstId, "Find"), Ct)).IsSuccess.ShouldBeTrue();
            Pictures(flowId).ShouldHaveSingleItem().Id.ShouldBe(pictureId);

            (await _dataService.FlowStep.DeleteAsync(secondId, Ct)).IsSuccess.ShouldBeTrue();
            Pictures(flowId).ShouldBeEmpty();
        }

        [Fact]
        public async Task A_step_cannot_link_another_flows_picture()
        {
            int flowId = await NewFlowAsync();
            (_, int pictureId) = await SearchWithPictureAsync(flowId, "Find", [1, 2, 3]);
            int otherId = await NewFlowAsync("Other");

            FlowStep find = new FlowStep { RootId = otherId, FlowId = otherId, FlowStepType = FlowStepTypeEnum.SEARCH_IMAGE, Name = "Find" };
            (await _dataService.FlowStep.CreateAsync(find, [new FlowStepTemplateDto { FlowTemplateId = pictureId }], Ct)).IsSuccess.ShouldBeTrue();

            Templates(otherId).ShouldBeEmpty();
        }

        // From the Templates tab: the picture changes for every step, and its name stays the file's.
        [Fact]
        public async Task Editing_a_template_on_its_own_keeps_its_name_and_reaches_every_step()
        {
            int flowId = await NewFlowAsync();
            (_, int pictureId) = await SearchWithPictureAsync(flowId, "Find", [1, 2, 3]);
            string name = Pictures(flowId).Single().Name;

            ResultDto<FlowTemplate> updated = await _dataService.FlowTemplate.UpdateAsync(new FlowTemplateDto { Id = pictureId, Name = "renamed.png", TemplateImage = [7], ClickOffsetX = 1, ClickOffsetY = 1 }, Ct);

            updated.IsSuccess.ShouldBeTrue();
            FlowTemplate picture = Pictures(flowId).Single();
            picture.Name.ShouldBe(name);
            picture.TemplateImage.ShouldBe([7]);
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
        // Named by their settings
        // ================================================================

        // Whatever name the form sends. Saved unchanged, a step keeps its number.
        [Fact]
        public async Task A_step_named_by_its_settings_is_renamed_when_they_change()
        {
            int flowId = await NewFlowAsync();
            int firstId = (await _dataService.FlowStep.CreateAsync(new FlowStep { RootId = flowId, FlowId = flowId, FlowStepType = FlowStepTypeEnum.WAIT, Name = "Typed", WaitForMilliseconds = 2000 }, [], Ct)).Data;
            await _dataService.FlowStep.CreateAsync(new FlowStep { RootId = flowId, FlowId = flowId, FlowStepType = FlowStepTypeEnum.WAIT, WaitForMilliseconds = 2000, OrderNumber = 1 }, [], Ct);

            Steps(flowId).Select(x => x.Name).Order().ShouldBe(["Wait 2s", "Wait 2s (2)"]);

            FlowStepDto unchanged = new FlowStepDto { Id = firstId, RootId = flowId, FlowId = flowId, FlowStepType = FlowStepTypeEnum.WAIT, Name = "Wait 2s", WaitForMilliseconds = 2000 };
            (await _dataService.FlowStep.UpdateAsync(unchanged, Ct)).IsSuccess.ShouldBeTrue();
            Steps(flowId).Single(x => x.Id == firstId).Name.ShouldBe("Wait 2s");

            FlowStepDto changed = new FlowStepDto { Id = firstId, RootId = flowId, FlowId = flowId, FlowStepType = FlowStepTypeEnum.WAIT, Name = "Wait 2s", WaitForMilliseconds = 1500 };
            (await _dataService.FlowStep.UpdateAsync(changed, Ct)).IsSuccess.ShouldBeTrue();
            Steps(flowId).Single(x => x.Id == firstId).Name.ShouldBe("Wait 1s 500ms");
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

        // The form sends no Id, which reached the flow as the empty Guid, so the second flow made
        // from the form broke the unique index.
        [Fact]
        public async Task A_new_flow_gets_an_Id_of_its_own_whatever_it_came_with()
        {
            int first = (await _dataService.Flow.CreateAsync(new Flow { Name = "Login", PublicId = Guid.Empty }, Ct)).Data;
            ResultDto<int> second = await _dataService.Flow.CreateAsync(new Flow { Name = "Checkout", PublicId = Guid.Empty }, Ct);

            second.IsSuccess.ShouldBeTrue(second.ErrorMessage);

            using AppDbContext db = _database.CreateDbContext();
            List<Guid> ids = db.Flows.Where(x => x.Id == first || x.Id == second.Data).Select(x => x.PublicId).ToList();
            ids.ShouldAllBe(x => x != Guid.Empty);
            ids.Distinct().Count().ShouldBe(2);
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

        // Linked the way the draft handler links them: a move under the search's Success row, reading
        // the search, with a point it creates. Every name is already taken once.
        [Fact]
        public async Task A_tree_of_new_steps_is_linked_named_apart_and_placed_at_its_index()
        {
            int flowId = await NewFlowAsync();
            await _dataService.FlowStep.CreateAsync(new FlowStep { RootId = flowId, FlowId = flowId, FlowStepType = FlowStepTypeEnum.LOOP, Name = "Find" }, [], Ct);

            FlowStep find = new FlowStep { FlowStepType = FlowStepTypeEnum.SEARCH_IMAGE, Name = "Find" };
            FlowStep success = TreeStepHelper.CreateBranchChildren(find).First(x => x.FlowStepType == FlowStepTypeEnum.SUCCESS);
            FlowStep move = new FlowStep { FlowStepType = FlowStepTypeEnum.CURSOR_RELOCATE, Name = "Move", ParentFlowStep = success, FlowStepReference = find, FlowPoint = new FlowPoint { Name = "Find" } };

            ResultDto<int> created = await _dataService.FlowStep.CreateTreeAsync(flowId, null, 0, [find, success, move], [], Ct);

            created.Data.ShouldBe(flowId);
            using AppDbContext db = _database.CreateDbContext();
            List<FlowStep> steps = db.FlowSteps.Where(x => x.RootId == flowId).ToList();
            FlowStep savedFind = steps.Single(x => x.FlowStepType == FlowStepTypeEnum.SEARCH_IMAGE);
            FlowStep savedMove = steps.Single(x => x.FlowStepType == FlowStepTypeEnum.CURSOR_RELOCATE);

            savedFind.Name.ShouldBe("Find 2");
            db.FlowPoints.Single(x => x.Id == savedMove.FlowPointId).Name.ShouldBe("Find 3");
            savedMove.Name.ShouldBe("Move cursor");
            savedMove.FlowStepReferenceId.ShouldBe(savedFind.Id);

            steps.Where(x => x.ParentFlowStepId == savedFind.Id).OrderBy(x => x.OrderNumber).Select(x => x.FlowStepType)
                .ShouldBe([FlowStepTypeEnum.SUCCESS, FlowStepTypeEnum.FAILURE]);
            savedMove.ParentFlowStepId.ShouldBe(steps.Single(x => x.ParentFlowStepId == savedFind.Id && x.FlowStepType == FlowStepTypeEnum.SUCCESS).Id);

            steps.Where(x => x.ParentFlowStepId == null).OrderBy(x => x.OrderNumber).Select(x => x.Name).ShouldBe(["Find 2", "Find"]);
        }

        // A new picture keeps the name it came with while the flow has none by it.
        [Fact]
        public async Task A_tree_of_new_steps_names_its_templates_apart_from_the_flows()
        {
            int flowId = await NewFlowAsync();
            await _dataService.FlowStep.CreateAsync(new FlowStep { RootId = flowId, FlowId = flowId, FlowStepType = FlowStepTypeEnum.SEARCH_IMAGE, Name = "Find" }, [new FlowStepTemplateDto { FlowTemplate = new FlowTemplateDto() }], Ct);
            string existing = Pictures(flowId).Single().Name;

            FlowStep find = new FlowStep { FlowStepType = FlowStepTypeEnum.SEARCH_IMAGE, Name = "Find again", FlowStepTemplates = [new FlowStepTemplate { FlowTemplate = new FlowTemplate { Name = existing } }, new FlowStepTemplate { FlowTemplate = new FlowTemplate() }] };
            (await _dataService.FlowStep.CreateTreeAsync(flowId, null, 1, [find], [], Ct)).IsSuccess.ShouldBeTrue();

            List<FlowTemplate> pictures = Pictures(flowId);
            pictures.ShouldAllBe(x => IsTemplateName(x.Name));
            pictures.Select(x => x.Name).Distinct(StringComparer.OrdinalIgnoreCase).Count().ShouldBe(3);
        }

        // Clicks on one element share its picture, so the recording hands over one row for both.
        [Fact]
        public async Task New_steps_sharing_a_picture_save_it_once()
        {
            int flowId = await NewFlowAsync();
            FlowTemplate picture = new FlowTemplate { TemplateImage = [4, 5] };
            FlowStep first = new FlowStep { FlowStepType = FlowStepTypeEnum.SEARCH_IMAGE, Name = "Find", FlowStepTemplates = [new FlowStepTemplate { FlowTemplate = picture }] };
            FlowStep second = new FlowStep { FlowStepType = FlowStepTypeEnum.SEARCH_IMAGE, Name = "Find again", FlowStepTemplates = [new FlowStepTemplate { FlowTemplate = picture }] };

            (await _dataService.FlowStep.CreateTreeAsync(flowId, null, 0, [first, second], [], Ct)).IsSuccess.ShouldBeTrue();

            IsTemplateName(Pictures(flowId).ShouldHaveSingleItem().Name).ShouldBeTrue();
            Templates(flowId).Select(x => x.FlowTemplateId).Distinct().ShouldHaveSingleItem();
        }

        [Fact]
        public async Task A_tree_refused_for_its_target_writes_nothing()
        {
            int flowId = await NewFlowAsync();
            int searchId = (await _dataService.FlowStep.CreateAsync(new FlowStep { RootId = flowId, FlowId = flowId, FlowStepType = FlowStepTypeEnum.SEARCH_IMAGE, Name = "Search" }, [], Ct)).Data;

            ResultDto<int> created = await _dataService.FlowStep.CreateTreeAsync(null, searchId, 0, [new FlowStep { FlowStepType = FlowStepTypeEnum.WAIT, Name = "Wait" }], [], Ct);

            created.ErrorMessage.ShouldBe("\"Search\" holds steps in its branches, not directly.");
            Steps(flowId).ShouldNotContain(x => x.Name == "Wait");
        }

        // ================================================================
        // Copy and paste
        // ================================================================

        // A search with a template, its Success row, and a move under it reading the search.
        private async Task<List<FlowStep>> SearchWithMoveAsync(int flowId)
        {
            FlowStep find = new FlowStep { FlowStepType = FlowStepTypeEnum.SEARCH_IMAGE, Name = "Find", FlowStepTemplates = [new FlowStepTemplate { FlowTemplate = new FlowTemplate { TemplateImage = [1, 2, 3] } }] };
            FlowStep success = TreeStepHelper.CreateBranchChildren(find).First(x => x.FlowStepType == FlowStepTypeEnum.SUCCESS);
            FlowStep move = new FlowStep { FlowStepType = FlowStepTypeEnum.CURSOR_RELOCATE, Name = "Move", ParentFlowStep = success, FlowStepReference = find };

            await _dataService.FlowStep.CreateTreeAsync(flowId, null, 0, [find, success, move], [], Ct);

            return Steps(flowId);
        }

        [Fact]
        public async Task A_pasted_step_brings_everything_under_it_named_apart_and_reading_the_copy()
        {
            int flowId = await NewFlowAsync();
            List<FlowStep> original = await SearchWithMoveAsync(flowId);
            FlowStep find = original.Single(x => x.FlowStepType == FlowStepTypeEnum.SEARCH_IMAGE);

            ResultDto<int> pasted = await _dataService.FlowStep.PasteAsync(new FlowStepPasteDto { FlowStepId = find.Id, TargetFlowId = flowId, TargetIndex = 1 }, Ct);

            pasted.IsSuccess.ShouldBeTrue(pasted.ErrorMessage);
            List<FlowStep> steps = Steps(flowId);
            FlowStep copy = steps.Single(x => x.Id == pasted.Data);
            FlowStep copySuccess = steps.Single(x => x.ParentFlowStepId == copy.Id && x.FlowStepType == FlowStepTypeEnum.SUCCESS);
            FlowStep copyMove = steps.Single(x => x.ParentFlowStepId == copySuccess.Id);

            copy.Name.ShouldBe("Find 2");
            copyMove.Name.ShouldBe("Move cursor 2");
            copyMove.FlowStepReferenceId.ShouldBe(copy.Id);
            steps.Where(x => x.ParentFlowStepId == copy.Id).Select(x => x.FlowStepType).ShouldBe([FlowStepTypeEnum.SUCCESS, FlowStepTypeEnum.FAILURE], ignoreOrder: true);
            steps.Where(x => x.ParentFlowStepId == null).OrderBy(x => x.OrderNumber).Select(x => x.Name).ShouldBe(["Find", "Find 2"]);

            // The copy links to the picture rather than copying it: fixing it once fixes both.
            List<FlowStepTemplate> templates = Templates(flowId);
            templates.Count.ShouldBe(2);
            templates.Select(x => x.FlowTemplateId).Distinct().ShouldHaveSingleItem();
            templates.Single(x => x.FlowStepId == copy.Id).FlowTemplate.TemplateImage.ShouldBe([1, 2, 3]);
            Pictures(flowId).ShouldHaveSingleItem();
        }

        // Where it lands decides whether the step can still read it, and the validator says when not.
        [Fact]
        public async Task A_pasted_step_keeps_a_reference_to_a_step_outside_the_copy()
        {
            int flowId = await NewFlowAsync();
            List<FlowStep> original = await SearchWithMoveAsync(flowId);
            FlowStep find = original.Single(x => x.FlowStepType == FlowStepTypeEnum.SEARCH_IMAGE);
            FlowStep move = original.Single(x => x.FlowStepType == FlowStepTypeEnum.CURSOR_RELOCATE);
            FlowStep failure = original.Single(x => x.FlowStepType == FlowStepTypeEnum.FAILURE);

            ResultDto<int> pasted = await _dataService.FlowStep.PasteAsync(new FlowStepPasteDto { FlowStepId = move.Id, TargetParentFlowStepId = failure.Id }, Ct);

            pasted.IsSuccess.ShouldBeTrue(pasted.ErrorMessage);
            FlowStep copy = Steps(flowId).Single(x => x.Id == pasted.Data);
            copy.ParentFlowStepId.ShouldBe(failure.Id);
            copy.FlowStepReferenceId.ShouldBe(find.Id);
        }

        [Fact]
        public async Task A_step_is_pasted_only_into_the_flow_it_was_copied_from()
        {
            int flowId = await NewFlowAsync();
            int otherFlowId = (await _dataService.Flow.CreateAsync(new Flow { Name = "Other" }, Ct)).Data;
            FlowStep find = (await SearchWithMoveAsync(flowId)).Single(x => x.FlowStepType == FlowStepTypeEnum.SEARCH_IMAGE);

            ResultDto<int> pasted = await _dataService.FlowStep.PasteAsync(new FlowStepPasteDto { FlowStepId = find.Id, TargetFlowId = otherFlowId }, Ct);

            pasted.IsSuccess.ShouldBeFalse();
            Steps(otherFlowId).ShouldBeEmpty();
        }

        [Fact]
        public async Task A_branch_is_not_pasted_without_its_step()
        {
            int flowId = await NewFlowAsync();
            FlowStep success = (await SearchWithMoveAsync(flowId)).Single(x => x.FlowStepType == FlowStepTypeEnum.SUCCESS);

            ResultDto<int> pasted = await _dataService.FlowStep.PasteAsync(new FlowStepPasteDto { FlowStepId = success.Id, TargetFlowId = flowId }, Ct);

            pasted.IsSuccess.ShouldBeFalse();
            Steps(flowId).Count(x => x.FlowStepType == FlowStepTypeEnum.SUCCESS).ShouldBe(1);
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

        // The script writes no name for a step its settings name, and the import leaves none unnamed.
        [Fact]
        public async Task An_import_names_the_steps_its_file_writes_no_name_for()
        {
            FlowScriptImportResultDto imported = await ImportAsync("Find Image  <[ Find ]>\n Success:\n  Move to <[ Find ]>\n  Click\nWait  800ms\nWait  800ms\n");

            Steps(imported.FlowId)
                .Where(x => FlowStepNameHelper.IsNamedBySettings(x.FlowStepType))
                .Select(x => x.Name)
                .ShouldBe(["Move cursor", "Click", "Wait 800ms", "Wait 800ms (2)"], ignoreOrder: true);
            imported.Validation!.Issues.Select(x => x.Code).ShouldNotContain(FlowValidationCodeEnum.NAME_MISSING);
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

        // A move to a point measured from Header, inside Browser - an area no step searches in.
        private async Task<(int FlowId, int SubFlowId, int MoveId)> ExtractMoveAsync()
        {
            int flowId = await NewFlowAsync();
            int moveId;

            using (AppDbContext db = _database.CreateDbContext())
            {
                FlowArea browser = new FlowArea { FlowId = flowId, Name = "Browser", Type = FlowAreaTypeEnum.APPLICATION, ProcessName = "chrome.exe", TitlePattern = "Swag", UseClientArea = false, ScalesWith = ScalesWithEnum.DPI, AuthoredDpi = 120 };
                FlowArea header = new FlowArea { FlowId = flowId, Name = "Header", Type = FlowAreaTypeEnum.CUSTOM, ParentFlowArea = browser, SizingMode = AreaSizingModeEnum.RATIO, RatioWidth = 1f, RatioHeight = 0.1f };
                FlowPoint button = new FlowPoint { FlowId = flowId, Name = "Button", FlowArea = header, OffsetMode = AreaSizingModeEnum.RATIO, RatioX = 0.9f, RatioY = 0.5f, AuthoredDpi = 120 };
                FlowStep move = new FlowStep { RootId = flowId, FlowId = flowId, FlowStepType = FlowStepTypeEnum.CURSOR_RELOCATE, Name = "Move", FlowPoint = button };

                db.AddRange(browser, header, button, move);
                db.SaveChanges();
                moveId = move.Id;
            }

            ResultDto<ExtractSubFlowResultDto> extracted = await _dataService.Flow.ExtractSubFlowAsync(
                new ExtractSubFlowDto { FlowStepId = moveId, Name = "Press the button", SourceRootId = flowId, SourceFlowId = flowId, SourceOrderNumber = 0 }, Ct);

            extracted.IsSuccess.ShouldBeTrue(extracted.ErrorMessage);
            return (flowId, extracted.Data!.SubFlowId, moveId);
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
            (int flowId, int subFlowId, int moveId) = await ExtractMoveAsync();

            using AppDbContext db = _database.CreateDbContext();
            List<FlowArea> areas = db.FlowAreas.Where(x => x.FlowId == subFlowId).ToList();
            FlowPoint button = db.FlowPoints.Single(x => x.FlowId == subFlowId);

            db.FlowSteps.Single(x => x.Id == moveId).FlowPointId.ShouldBe(button.Id);
            button.FlowAreaId.ShouldBe(areas.Single(x => x.Name == "Header").Id);
            areas.Single(x => x.Name == "Header").ParentFlowAreaId.ShouldBe(areas.Single(x => x.Name == "Browser").Id);

            db.FlowAreas.Count(x => x.FlowId == flowId).ShouldBe(2);
            db.FlowPoints.Count(x => x.FlowId == flowId).ShouldBe(1);
        }

        // Copied whole, so a column added later cannot be left behind.
        [Fact]
        public async Task An_extracted_area_and_point_keep_every_column()
        {
            (int flowId, int subFlowId, _) = await ExtractMoveAsync();

            using AppDbContext db = _database.CreateDbContext();

            foreach (FlowArea original in db.FlowAreas.Where(x => x.FlowId == flowId).ToList())
                ScriptRows.Differences(original, db.FlowAreas.Single(x => x.FlowId == subFlowId && x.Name == original.Name)).ShouldBeEmpty(original.Name);

            ScriptRows.Differences(db.FlowPoints.Single(x => x.FlowId == flowId), db.FlowPoints.Single(x => x.FlowId == subFlowId)).ShouldBeEmpty();
        }

        // A sub-flow keeps its own pictures, as it keeps its own areas. One the flow still uses
        // stays there too; one only the extracted steps used leaves with them.
        [Fact]
        public async Task An_extracted_step_takes_copies_of_its_pictures()
        {
            int flowId = await NewFlowAsync();
            (int keptId, int sharedId) = await SearchWithPictureAsync(flowId, "Find", [1, 2, 3]);
            FlowStep leaving = new FlowStep { RootId = flowId, FlowId = flowId, FlowStepType = FlowStepTypeEnum.SEARCH_IMAGE, Name = "Find again", OrderNumber = 1 };
            FlowStepTemplateDto own = new FlowStepTemplateDto { FlowTemplate = new FlowTemplateDto { TemplateImage = [8] } };
            int leavingId = (await _dataService.FlowStep.CreateAsync(leaving, [new FlowStepTemplateDto { FlowTemplateId = sharedId }, own], Ct)).Data;

            ResultDto<ExtractSubFlowResultDto> extracted = await _dataService.Flow.ExtractSubFlowAsync(
                new ExtractSubFlowDto { FlowStepId = leavingId, Name = "Look again", SourceRootId = flowId, SourceFlowId = flowId, SourceOrderNumber = 1 }, Ct);
            extracted.IsSuccess.ShouldBeTrue(extracted.ErrorMessage);

            List<FlowTemplate> flowPictures = Pictures(flowId);
            flowPictures.ShouldHaveSingleItem().Id.ShouldBe(sharedId);

            List<FlowTemplate> subFlowPictures = Pictures(extracted.Data!.SubFlowId);
            subFlowPictures.Select(x => x.TemplateImage).ShouldBe([[1, 2, 3], [8]], ignoreOrder: true);
            Templates(extracted.Data.SubFlowId).ShouldAllBe(x => subFlowPictures.Any(y => y.Id == x.FlowTemplateId));
            Templates(flowId).Single(x => x.FlowStepId == keptId).FlowTemplateId.ShouldBe(sharedId);
        }
    }
}
