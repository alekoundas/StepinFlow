using Business.Flows.DataService;
using Business.FlowScript;
using Business.FlowScript.Text;
using Business.Tests.Fakes;
using Core.Enums;
using Core.Models.Database;
using Core.Models.Dtos.FlowScript;
using static Business.Tests.TestToken;

namespace Business.Tests.FlowScript
{
    /// <summary>
    /// A step's form shows the line its step will be in the script, before the step is saved.
    /// </summary>
    public sealed class FlowStepScriptLineServiceTests : IDisposable
    {
        private readonly TestDatabase _database = new TestDatabase();
        private readonly DataService _dataService;
        private readonly FlowStepScriptLineService _service;

        public FlowStepScriptLineServiceTests()
        {
            _dataService = TestDataService.For(_database);
            _service = new FlowStepScriptLineService(_database, new Printer());
        }

        public void Dispose()
        {
            _database.Dispose();
        }

        private static List<string> Lines(FlowStepScriptLineDto line)
        {
            return line.Script.Split('\n').Select(x => x.TrimEnd('\r')).ToList();
        }

        // The form has the step it reads only as an id, and the line names it.
        [Fact]
        public async Task The_line_names_what_the_step_points_at_under_its_comment()
        {
            int flowId = (await _dataService.Flow.CreateAsync(new Flow { Name = "Flow" }, Ct)).Data;
            int findId = (await _dataService.FlowStep.CreateAsync(new FlowStep { RootId = flowId, FlowId = flowId, FlowStepType = FlowStepTypeEnum.SEARCH_IMAGE, Name = "Find the field" }, [], Ct)).Data;

            FlowStep move = new FlowStep { FlowStepType = FlowStepTypeEnum.CURSOR_RELOCATE, Name = "Move", FlowStepReferenceId = findId, CodeComment = "Into the field\nbefore typing" };

            FlowStepScriptLineDto line = await _service.WriteAsync(move, Ct);

            Lines(line).ShouldBe(["# Into the field", "# before typing", "Move            to <[ Find the field ]>"]);
            line.ReferenceName.ShouldBe("Find the field");
        }

        [Fact]
        public async Task A_branch_prints_its_comment_over_its_own_line()
        {
            FlowStep failure = new FlowStep { FlowStepType = FlowStepTypeEnum.FAILURE, Name = "Failure", CodeComment = "Behind the menu on a narrow window." };

            FlowStepScriptLineDto line = await _service.WriteAsync(failure, Ct);

            Lines(line).ShouldBe(["# Behind the menu on a narrow window.", "Failure:"]);
        }

        [Fact]
        public async Task A_sub_flow_step_names_the_file_its_sub_flow_exports_to()
        {
            int subFlowId = (await _dataService.Flow.CreateAsync(new Flow { Name = "Sign in" }, Ct)).Data;

            FlowStepScriptLineDto line = await _service.WriteAsync(new FlowStep { FlowStepType = FlowStepTypeEnum.SUB_FLOW, Name = "Sign in", SubFlowId = subFlowId }, Ct);

            line.Script.ShouldEndWith(".sflw ]>");
            line.SubFlowName.ShouldBe("Sign in");
        }
    }
}
