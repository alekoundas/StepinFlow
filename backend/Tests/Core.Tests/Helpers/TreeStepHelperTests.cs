using Core.Enums;
using Core.Helpers;
using Core.Models.Business;
using Core.Models.Database;

namespace Core.Tests.Helpers
{
    public sealed class TreeStepHelperTests
    {
        // 1 Find image
        //   2 Success
        //     3 Click
        //     4 Read text
        //       5 Success
        //         6 Check value
        //       7 Failure
        //         8 Notify
        //   9 Failure
        //     10 Click
        private static readonly Dictionary<int, StepChainNode> Tree = new Dictionary<int, StepChainNode>
        {
            [1] = new StepChainNode(1, null, FlowStepTypeEnum.SEARCH_IMAGE, "Find image"),
            [2] = new StepChainNode(2, 1, FlowStepTypeEnum.SUCCESS, "Success"),
            [3] = new StepChainNode(3, 2, FlowStepTypeEnum.CURSOR_CLICK, "Click"),
            [4] = new StepChainNode(4, 2, FlowStepTypeEnum.SEARCH_TEXT, "Read text"),
            [5] = new StepChainNode(5, 4, FlowStepTypeEnum.SUCCESS, "Success"),
            [6] = new StepChainNode(6, 5, FlowStepTypeEnum.CHECK_VALUE, "Check value"),
            [7] = new StepChainNode(7, 4, FlowStepTypeEnum.FAILURE, "Failure"),
            [8] = new StepChainNode(8, 7, FlowStepTypeEnum.NOTIFY, "Notify"),
            [9] = new StepChainNode(9, 1, FlowStepTypeEnum.FAILURE, "Failure"),
            [10] = new StepChainNode(10, 9, FlowStepTypeEnum.CURSOR_CLICK, "Click"),
        };

        [Theory]
        [InlineData(3, 1, true)]
        [InlineData(6, 4, true)]
        [InlineData(6, 1, true)]
        [InlineData(10, 1, false)]
        [InlineData(8, 4, false)]
        [InlineData(4, 6, false)]
        public void A_step_reads_only_results_it_sits_under_through_success(int from, int reference, bool canRead)
        {
            TreeStepHelper.CanReadResultOf(Tree, from, reference).ShouldBe(canRead);
        }

        [Theory]
        [InlineData(8, 4, true)]
        [InlineData(10, 1, true)]
        [InlineData(3, 1, false)]
        [InlineData(8, 1, false)]
        public void A_step_reports_only_failures_it_sits_under_through_failure(int from, int reference, bool canReport)
        {
            TreeStepHelper.CanReportFailureOf(Tree, from, reference).ShouldBe(canReport);
        }

        [Fact]
        public void Successful_ancestors_come_nearest_first_with_their_depth()
        {
            List<(int Id, int Depth)> ancestors = TreeStepHelper.SuccessfulAncestors(Tree, 6)
                .Select(x => (x.Step.Id, x.Depth))
                .ToList();

            ancestors.ShouldBe([(4, 2), (1, 4)]);
        }

        [Fact]
        public void A_parent_chain_that_loops_ends_rather_than_spinning()
        {
            Dictionary<int, StepChainNode> corrupt = new Dictionary<int, StepChainNode>
            {
                [1] = new StepChainNode(1, 2, FlowStepTypeEnum.SUCCESS, "a"),
                [2] = new StepChainNode(2, 1, FlowStepTypeEnum.SUCCESS, "b"),
            };

            TreeStepHelper.SuccessfulAncestors(corrupt, 1).Count().ShouldBeLessThanOrEqualTo(3);
        }

        [Fact]
        public void A_branching_step_gets_success_then_failure()
        {
            FlowStep search = new FlowStep { FlowStepType = FlowStepTypeEnum.SEARCH_IMAGE, RootId = 7 };

            IReadOnlyList<FlowStep> branches = TreeStepHelper.CreateBranchChildren(search);

            branches.Select(x => (x.FlowStepType, x.OrderNumber, x.RootId)).ShouldBe(
            [
                (FlowStepTypeEnum.SUCCESS, 0, 7),
                (FlowStepTypeEnum.FAILURE, 1, 7),
            ]);
        }

        [Fact]
        public void A_step_that_does_not_branch_gets_no_branches()
        {
            TreeStepHelper.CreateBranchChildren(new FlowStep { FlowStepType = FlowStepTypeEnum.WAIT }).ShouldBeEmpty();
        }

        [Theory]
        [InlineData(FlowStepTypeEnum.SEARCH_IMAGE, true, false, true)]
        [InlineData(FlowStepTypeEnum.CHECK_VALUE, true, false, true)]
        [InlineData(FlowStepTypeEnum.WINDOW_FOCUS, true, false, false)]
        [InlineData(FlowStepTypeEnum.LOOP, false, true, false)]
        [InlineData(FlowStepTypeEnum.END_EXECUTION, false, true, false)]
        [InlineData(FlowStepTypeEnum.CURSOR_CLICK, false, false, false)]
        public void Each_type_branches_contains_or_checks_as_the_tree_expects(FlowStepTypeEnum type, bool branches, bool contains, bool isCheck)
        {
            TreeStepHelper.HasBranchChildren(type).ShouldBe(branches);
            TreeStepHelper.CanContainChildren(type).ShouldBe(contains);
            TreeStepHelper.IsCheck(type).ShouldBe(isCheck);
            TreeStepHelper.IsLeaf(type).ShouldBe(!branches && !contains);
        }
    }
}
