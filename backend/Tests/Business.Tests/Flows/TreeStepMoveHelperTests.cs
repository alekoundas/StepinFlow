using Business.Flows;
using Core.Enums;
using Core.Models.Database;
using Core.Models.Dtos;

namespace Business.Tests.Flows
{
    public sealed class TreeStepMoveHelperTests
    {
        // 1 Find image                 (root)
        //   2 Success
        //     3 Click at "Find image"
        //     4 Loop
        //       5 Wait
        //   6 Failure
        // 7 Wait                       (root)
        private static List<FlowStep> Tree()
        {
            return
            [
                new FlowStep { Id = 1, FlowId = 1, FlowStepType = FlowStepTypeEnum.SEARCH_IMAGE, Name = "Find image" },
                new FlowStep { Id = 2, ParentFlowStepId = 1, FlowStepType = FlowStepTypeEnum.SUCCESS, Name = "Success" },
                new FlowStep { Id = 3, ParentFlowStepId = 2, FlowStepType = FlowStepTypeEnum.CURSOR_CLICK, Name = "Click", FlowStepReferenceId = 1, OrderNumber = 0 },
                new FlowStep { Id = 4, ParentFlowStepId = 2, FlowStepType = FlowStepTypeEnum.LOOP, Name = "Loop", OrderNumber = 1 },
                new FlowStep { Id = 5, ParentFlowStepId = 4, FlowStepType = FlowStepTypeEnum.WAIT, Name = "Wait inside" },
                new FlowStep { Id = 6, ParentFlowStepId = 1, FlowStepType = FlowStepTypeEnum.FAILURE, Name = "Failure" },
                new FlowStep { Id = 7, FlowId = 1, FlowStepType = FlowStepTypeEnum.WAIT, Name = "Wait" },
            ];
        }

        [Fact]
        public void A_step_can_move_into_a_container()
        {
            TreeStepMoveHelper.Validate(Tree(), new FlowStepMoveDto { FlowStepId = 7, TargetParentFlowStepId = 4 }).ShouldBeNull();
        }

        [Fact]
        public void A_step_can_move_to_the_root()
        {
            TreeStepMoveHelper.Validate(Tree(), new FlowStepMoveDto { FlowStepId = 3, TargetFlowId = 1 }).ShouldBeNull();
        }

        [Theory]
        [InlineData(99, 4, null, "no longer exists")]
        [InlineData(7, null, null, "needs a destination")]
        [InlineData(7, 4, 1, "not both")]
        [InlineData(4, 4, null, "into itself")]
        [InlineData(7, 99, null, "destination step no longer exists")]
        [InlineData(7, 1, null, "holds steps in its branches")]
        [InlineData(1, 4, null, "inside one of its own children")]
        public void A_move_that_would_corrupt_the_tree_is_refused_with_a_reason(int stepId, int? targetParent, int? targetFlow, string reason)
        {
            string? error = TreeStepMoveHelper.Validate(Tree(), new FlowStepMoveDto { FlowStepId = stepId, TargetParentFlowStepId = targetParent, TargetFlowId = targetFlow });

            error.ShouldNotBeNull();
            error.ShouldContain(reason);
        }

        [Fact]
        public void Descendants_are_everything_below_and_not_the_step_itself()
        {
            TreeStepMoveHelper.GetDescendantIds(Tree(), 1).OrderBy(x => x).ShouldBe([2, 3, 4, 5, 6]);
        }

        [Fact]
        public void Moving_a_reader_out_of_the_success_branch_reports_the_reference_it_breaks()
        {
            List<FlowStepBrokenReferenceDto> broken = TreeStepMoveHelper.FindBrokenReferences(Tree(), new FlowStepMoveDto { FlowStepId = 3, TargetParentFlowStepId = 6 });

            broken.Count.ShouldBe(1);
            broken[0].FlowStepName.ShouldBe("Click");
            broken[0].ReferencedStepName.ShouldBe("Find image");
            broken[0].IsEndReference.ShouldBeFalse();
        }

        [Fact]
        public void Moving_a_reader_deeper_under_the_same_success_breaks_nothing()
        {
            TreeStepMoveHelper.FindBrokenReferences(Tree(), new FlowStepMoveDto { FlowStepId = 3, TargetParentFlowStepId = 4 }).ShouldBeEmpty();
        }

        [Fact]
        public void A_reference_that_was_already_broken_is_not_this_moves_fault()
        {
            List<FlowStep> steps = Tree();
            steps.Single(x => x.Id == 7).FlowStepReferenceId = 1;

            TreeStepMoveHelper.FindBrokenReferences(steps, new FlowStepMoveDto { FlowStepId = 5, TargetFlowId = 1 }).ShouldBeEmpty();
        }

        [Fact]
        public void The_moved_step_is_inserted_at_its_index_and_the_siblings_renumbered()
        {
            FlowStep a = new FlowStep { Id = 1, OrderNumber = 0 };
            FlowStep b = new FlowStep { Id = 2, OrderNumber = 1 };
            FlowStep c = new FlowStep { Id = 3, OrderNumber = 2 };
            FlowStep moved = new FlowStep { Id = 9, OrderNumber = 5 };

            TreeStepMoveHelper.ApplyOrder([c, a, b], moved, 1);

            (a.OrderNumber, moved.OrderNumber, b.OrderNumber, c.OrderNumber).ShouldBe((0, 1, 2, 3));
        }

        [Fact]
        public void A_step_moved_within_its_own_siblings_is_not_counted_twice()
        {
            FlowStep a = new FlowStep { Id = 1, OrderNumber = 0 };
            FlowStep b = new FlowStep { Id = 2, OrderNumber = 1 };
            FlowStep c = new FlowStep { Id = 3, OrderNumber = 2 };

            TreeStepMoveHelper.ApplyOrder([a, b, c], a, 99);

            (b.OrderNumber, c.OrderNumber, a.OrderNumber).ShouldBe((0, 1, 2));
        }
    }
}
