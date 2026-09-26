using System.Drawing;

using Business.Executions;
using Business.Tests.Executions.Workers;
using Core.Enums;
using Core.Models.Database;

namespace Business.Tests.Executions
{
    /// <summary>
    /// Which step runs next. The walker executes nothing - it is handed each step's result - so a
    /// flow is built in memory, outcomes are decided by name, and the test reads the order the
    /// steps were walked in.
    /// </summary>
    public sealed class ExecutionFlowWalkerTests
    {
        private readonly List<FlowStep> _steps = new List<FlowStep>();
        private readonly HashSet<string> _failing = new HashSet<string>();
        private readonly List<ExecutionStep> _placed = new List<ExecutionStep>();
        private ExecutionCacheService _cache = null!;

        // What a step does when it runs, beyond passing or failing - a search recording its hits.
        private readonly Dictionary<string, Action<FlowStep>> _effects = new Dictionary<string, Action<FlowStep>>();

        private FlowStep Add(string name, FlowStepTypeEnum type = FlowStepTypeEnum.WAIT, FlowStep? parent = null, int flowId = 1)
        {
            FlowStep step = new FlowStep
            {
                Id = _steps.Count + 1,
                Name = name,
                FlowStepType = type,
                RootId = flowId,
                ParentFlowStepId = parent?.Id,
                OrderNumber = _steps.Count(x => x.ParentFlowStepId == parent?.Id && x.RootId == flowId),
            };

            if (parent == null)
                step.FlowId = flowId;

            _steps.Add(step);
            return step;
        }

        private FlowStep Search(string name, FlowStep? parent = null, SearchModeEnum mode = SearchModeEnum.FIND_BEST)
        {
            FlowStep search = Add(name, FlowStepTypeEnum.SEARCH_IMAGE, parent);
            search.SearchMode = mode;
            return search;
        }

        private FlowStep Success(FlowStep step)
        {
            return Add(step.Name + " succeeded", FlowStepTypeEnum.SUCCESS, step);
        }

        private FlowStep Failure(FlowStep step)
        {
            return Add(step.Name + " failed", FlowStepTypeEnum.FAILURE, step);
        }

        // Walks the way the engine does: place the result, record it, ask for the next step, then
        // record any FIND_ALL hits the walker handed out without executing anything.
        private async Task<List<string>> WalkAsync(int maxSteps = 100)
        {
            _cache = await WorkerCache.ForAsync([.. _steps]);
            ExecutionFlowWalker walker = new ExecutionFlowWalker(_cache, TimeProvider.System);
            List<string> walked = new List<string>();

            FlowStep? step = walker.Start(1);
            while (step != null && walked.Count < maxSteps)
            {
                if (_effects.TryGetValue(step.Name, out Action<FlowStep>? effect))
                    effect(step);

                ExecutionStep result = ExecutionStep.Success();
                if (_failing.Contains(step.Name))
                    result = ExecutionStep.Failure();

                walker.PlaceInRun(result, step);
                _cache.RecordExecutionStep(step.Id, result);
                _placed.Add(result);
                walked.Add(step.Name);

                step = walker.Next(step, result);

                foreach (ExecutionStep repeat in walker.TakeMatchRepeats())
                {
                    _placed.Add(repeat);
                    walked.Add($"{repeat.Name} hit {repeat.MatchIndex + 1} of {repeat.MatchCount}");
                }
            }

            return walked;
        }

        // ================================================================
        // Order and branches
        // ================================================================

        [Fact]
        public async Task Top_level_steps_run_in_order()
        {
            Add("A");
            Add("B");
            Add("C");

            (await WalkAsync()).ShouldBe(["A", "B", "C"]);
        }

        [Fact]
        public async Task An_empty_flow_walks_nothing()
        {
            (await WalkAsync()).ShouldBeEmpty();
        }

        [Fact]
        public async Task A_step_that_passes_runs_its_success_branch_and_carries_on()
        {
            FlowStep find = Search("Find");
            Add("Click", parent: Success(find));
            Add("Report", parent: Failure(find));
            Add("After");

            (await WalkAsync()).ShouldBe(["Find", "Click", "After"]);
        }

        [Fact]
        public async Task A_step_that_fails_runs_its_failure_branch_and_carries_on()
        {
            FlowStep find = Search("Find");
            Add("Click", parent: Success(find));
            Add("Report", parent: Failure(find));
            Add("After");
            _failing.Add("Find");

            (await WalkAsync()).ShouldBe(["Find", "Report", "After"]);
        }

        [Fact]
        public async Task Branches_nest_and_the_walk_comes_back_out_to_where_it_was()
        {
            FlowStep outer = Search("Outer");
            FlowStep found = Success(outer);
            FlowStep inner = Search("Inner", found);
            Add("Inner click", parent: Success(inner));
            Add("Outer click", parent: found);
            Add("After");

            (await WalkAsync()).ShouldBe(["Outer", "Inner", "Inner click", "Outer click", "After"]);
        }

        [Fact]
        public async Task Each_step_knows_how_deep_it_ran_and_under_which_step()
        {
            FlowStep outer = Search("Outer");
            Add("Inner", parent: Success(outer));

            await WalkAsync();

            _placed.Select(x => (x.Name, x.Depth, x.ParentSequence)).ShouldBe([("Outer", 0, null), ("Inner", 1, 0)]);
        }

        // ================================================================
        // Loops
        // ================================================================

        [Fact]
        public async Task A_loop_runs_its_body_its_count_and_numbers_each_pass()
        {
            FlowStep loop = Add("Loop", FlowStepTypeEnum.LOOP);
            loop.LoopCount = 3;
            Add("Body", parent: loop);
            Add("After");

            (await WalkAsync()).ShouldBe(["Loop", "Body", "Loop", "Body", "Loop", "Body", "After"]);
            _placed.Where(x => x.Name == "Loop").Select(x => x.LoopPass).ShouldBe([0, 1, 2]);
        }

        [Fact]
        public async Task A_loop_for_ever_does_not_stop_on_its_own()
        {
            FlowStep loop = Add("Loop", FlowStepTypeEnum.LOOP);
            loop.IsLoopInfinite = true;
            Add("Body", parent: loop);

            (await WalkAsync(maxSteps: 40)).Count.ShouldBe(40);
        }

        [Fact]
        public async Task A_loop_run_again_later_starts_counting_from_the_beginning()
        {
            FlowStep outer = Add("Outer", FlowStepTypeEnum.LOOP);
            outer.LoopCount = 2;
            FlowStep inner = Add("Inner", FlowStepTypeEnum.LOOP, outer);
            inner.LoopCount = 2;
            Add("Body", parent: inner);

            (await WalkAsync()).Count(x => x == "Body").ShouldBe(4);
        }

        // ================================================================
        // Go To, End Execution and sub-flows
        // ================================================================

        [Fact]
        public async Task Go_to_carries_on_from_the_step_it_names()
        {
            Add("A");
            FlowStep goTo = Add("Go to D", FlowStepTypeEnum.GO_TO);
            Add("C");
            FlowStep d = Add("D");
            goTo.FlowStepReferenceId = d.Id;

            (await WalkAsync()).ShouldBe(["A", "Go to D", "D"]);
        }

        [Fact]
        public async Task Go_to_with_nowhere_to_go_carries_on_to_the_next_step()
        {
            Add("Go to nowhere", FlowStepTypeEnum.GO_TO);
            Add("B");

            (await WalkAsync()).ShouldBe(["Go to nowhere", "B"]);
        }

        [Fact]
        public async Task End_execution_abandons_everything_pending_but_runs_its_own_cleanup()
        {
            FlowStep find = Search("Find");
            FlowStep end = Add("End", FlowStepTypeEnum.END_EXECUTION, Failure(find));
            Add("Close the browser", parent: end);
            Add("After the search");
            _failing.Add("Find");

            (await WalkAsync()).ShouldBe(["Find", "End", "Close the browser"]);
        }

        [Fact]
        public async Task A_sub_flow_runs_its_steps_and_the_walk_comes_back()
        {
            FlowStep call = Add("Call checkout", FlowStepTypeEnum.SUB_FLOW);
            call.SubFlowId = 2;
            Add("After");
            Add("Pay", flowId: 2);
            Add("Confirm", flowId: 2);

            (await WalkAsync()).ShouldBe(["Call checkout", "Pay", "Confirm", "After"]);
        }

        [Fact]
        public async Task A_flow_that_calls_itself_with_no_way_out_is_stopped()
        {
            FlowStep call = Add("Call myself", FlowStepTypeEnum.SUB_FLOW);
            call.SubFlowId = 1;

            InvalidOperationException error = await Should.ThrowAsync<InvalidOperationException>(WalkAsync(maxSteps: 1000));

            error.Message.ShouldContain("nested more than 50 deep");
        }

        // ================================================================
        // FIND_ALL and results
        // ================================================================

        [Fact]
        public async Task Find_all_runs_its_success_branch_once_per_hit_from_one_screenshot()
        {
            FlowStep find = Search("Find all", mode: SearchModeEnum.FIND_ALL);
            FlowStep click = Add("Click", parent: Success(find));
            Add("After");

            List<Point> hits = [new Point(10, 10), new Point(20, 10), new Point(30, 10)];
            List<Point?> clickedAt = new List<Point?>();
            _effects["Find all"] = x => _cache.RecordMatches(x.Id, hits);
            _effects["Click"] = x => clickedAt.Add(_cache.GetStepLocationFrom(find.Id));

            List<string> walked = await WalkAsync();

            walked.ShouldBe(["Find all", "Click", "Find all hit 2 of 3", "Click", "Find all hit 3 of 3", "Click", "After"]);
            clickedAt.Skip(1).ShouldBe([hits[1], hits[2]]);
        }

        [Fact]
        public async Task Find_all_with_one_hit_is_just_a_search()
        {
            FlowStep find = Search("Find all", mode: SearchModeEnum.FIND_ALL);
            Add("Click", parent: Success(find));
            _effects["Find all"] = x => _cache.RecordMatches(x.Id, [new Point(10, 10)]);

            (await WalkAsync()).ShouldBe(["Find all", "Click"]);
        }

        // What ForgetFrom says it does, and does not: it records the popped step's depth and then
        // forgets that depth, so nothing is ever forgotten and every result stays readable. A decision
        // rather than a fix - FLOW-FORMAT.md reads an earlier sibling's result. See TODO.md.
        [Fact(Skip = "Results are never forgotten today; whether they should be is undecided. See TODO.md.")]
        public async Task A_result_is_readable_below_its_step_and_forgotten_once_the_walk_leaves_it()
        {
            FlowStep find = Search("Find");
            Add("Inside", parent: Success(find));
            Add("After");

            bool readableInside = false;
            bool readableAfter = true;
            _effects["Inside"] = x => readableInside = _cache.GetExecutionStepFrom(find.Id) != null;
            _effects["After"] = x => readableAfter = _cache.GetExecutionStepFrom(find.Id) != null;

            (await WalkAsync()).ShouldBe(["Find", "Inside", "After"]);

            readableInside.ShouldBeTrue();
            readableAfter.ShouldBeFalse();
        }
    }
}
