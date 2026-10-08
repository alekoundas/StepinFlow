using Core.Models.Database;

namespace Business.FlowScript.Text.Steps
{
    internal sealed class BranchWriter : BaseStepWriter
    {
        public BranchWriter(FlowStep step) : base(step)
        {
        }

        protected override void Compose()
        {
            // Success: | Failure:
            WriteKeyword(Step.FlowStepType);
        }
    }
}
