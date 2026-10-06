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
            [1] = new StepChainNode(1, null, FlowStepTypeEnum.SEARCH_IMAGE, "Find image", 0),
            [2] = new StepChainNode(2, 1, FlowStepTypeEnum.SUCCESS, "Success", 0),
            [3] = new StepChainNode(3, 2, FlowStepTypeEnum.CURSOR_CLICK, "Click", 0),
            [4] = new StepChainNode(4, 2, FlowStepTypeEnum.SEARCH_TEXT, "Read text", 1),
            [5] = new StepChainNode(5, 4, FlowStepTypeEnum.SUCCESS, "Success", 0),
            [6] = new StepChainNode(6, 5, FlowStepTypeEnum.CHECK_VALUE, "Check value", 0),
            [7] = new StepChainNode(7, 4, FlowStepTypeEnum.FAILURE, "Failure", 1),
            [8] = new StepChainNode(8, 7, FlowStepTypeEnum.NOTIFY, "Notify", 0),
            [9] = new StepChainNode(9, 1, FlowStepTypeEnum.FAILURE, "Failure", 1),
            [10] = new StepChainNode(10, 9, FlowStepTypeEnum.CURSOR_CLICK, "Click", 0),
        };

        // 1 Open the page
        // 2 Loop
        //   3 Type
        //   4 Wait
        // 5 Find image
        //   6 Success
        //     7 Click
        //     8 Go back
        //   9 Failure
        //     10 Retry
        // 11 Go back
        private static readonly Dictionary<int, StepChainNode> GoBackTree = new Dictionary<int, StepChainNode>
        {
            [1] = new StepChainNode(1, null, FlowStepTypeEnum.WAIT, "Open the page", 0),
            [2] = new StepChainNode(2, null, FlowStepTypeEnum.LOOP, "Loop", 1),
            [3] = new StepChainNode(3, 2, FlowStepTypeEnum.KEYBOARD_INPUT, "Type", 0),
            [4] = new StepChainNode(4, 2, FlowStepTypeEnum.WAIT, "Wait", 1),
            [5] = new StepChainNode(5, null, FlowStepTypeEnum.SEARCH_IMAGE, "Find image", 2),
            [6] = new StepChainNode(6, 5, FlowStepTypeEnum.SUCCESS, "Success", 0),
            [7] = new StepChainNode(7, 6, FlowStepTypeEnum.CURSOR_CLICK, "Click", 0),
            [8] = new StepChainNode(8, 6, FlowStepTypeEnum.GO_BACK, "Go back", 1),
            [9] = new StepChainNode(9, 5, FlowStepTypeEnum.FAILURE, "Failure", 1),
            [10] = new StepChainNode(10, 9, FlowStepTypeEnum.WAIT, "Retry", 0),
            [11] = new StepChainNode(11, null, FlowStepTypeEnum.GO_BACK, "Go back", 3),
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
                [1] = new StepChainNode(1, 2, FlowStepTypeEnum.SUCCESS, "a", 0),
                [2] = new StepChainNode(2, 1, FlowStepTypeEnum.SUCCESS, "b", 0),
            };

            TreeStepHelper.SuccessfulAncestors(corrupt, 1).Count().ShouldBeLessThanOrEqualTo(3);
        }

        [Fact]
        public void Go_back_targets_come_nearest_first()
        {
            TreeStepHelper.GoBackTargets(GoBackTree, 8).Select(x => x.Id).ShouldBe([7, 5, 2, 1]);
        }

        [Theory]
        [InlineData(8, 7, true)]
        [InlineData(8, 5, true)]
        [InlineData(8, 1, true)]
        [InlineData(11, 5, true)]
        [InlineData(4, 2, true)]
        [InlineData(8, 10, false)] // the other branch
        [InlineData(8, 3, false)]  // inside an earlier block
        [InlineData(8, 6, false)]  // a branch row
        [InlineData(7, 8, false)]  // forward
        [InlineData(8, 8, false)]  // itself
        public void A_go_back_returns_only_to_a_step_it_passed_on_the_way_here(int from, int target, bool canGoBack)
        {
            TreeStepHelper.CanGoBackTo(GoBackTree, from, target).ShouldBe(canGoBack);
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
