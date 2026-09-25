using Business.Executions;
using Core.Models.Database;

namespace Business.Tests.Executions
{
    public sealed class FlowStructureHasherTests
    {
        private static List<FlowStep> Steps()
        {
            return
            [
                new FlowStep { Id = 1, OrderNumber = 0, Name = "Find" },
                new FlowStep { Id = 2, ParentFlowStepId = 1, OrderNumber = 0, Name = "Success" },
                new FlowStep { Id = 3, OrderNumber = 1, Name = "Wait" },
            ];
        }

        [Fact]
        public void Renaming_a_step_keeps_the_hash()
        {
            List<FlowStep> renamed = Steps();
            renamed[0].Name = "Find the login button";
            renamed[0].CodeComment = "changed";

            FlowStructureHasher.Hash(renamed).ShouldBe(FlowStructureHasher.Hash(Steps()));
        }

        [Fact]
        public void The_order_the_steps_arrive_in_does_not_matter()
        {
            List<FlowStep> shuffled = Steps();
            shuffled.Reverse();

            FlowStructureHasher.Hash(shuffled).ShouldBe(FlowStructureHasher.Hash(Steps()));
        }

        [Fact]
        public void Reordering_steps_changes_the_hash()
        {
            List<FlowStep> reordered = Steps();
            reordered[0].OrderNumber = 1;
            reordered[2].OrderNumber = 0;

            FlowStructureHasher.Hash(reordered).ShouldNotBe(FlowStructureHasher.Hash(Steps()));
        }

        [Fact]
        public void Moving_a_step_to_another_parent_changes_the_hash()
        {
            List<FlowStep> moved = Steps();
            moved[2].ParentFlowStepId = 1;

            FlowStructureHasher.Hash(moved).ShouldNotBe(FlowStructureHasher.Hash(Steps()));
        }
    }
}
