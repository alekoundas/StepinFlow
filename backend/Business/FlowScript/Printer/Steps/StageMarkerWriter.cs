using Core.Enums;
using Core.Models.Database;

namespace Business.FlowScript.Text.Steps
{
    internal sealed class StageMarkerWriter : BaseStepWriter
    {
        public StageMarkerWriter(FlowStep step) : base(step)
        {
        }

        protected override void Compose()
        {
            // ## Sign in
            WriteKeyword(FlowStepTypeEnum.STAGE_MARKER);
            WriteText(Step.Name);
        }
    }
}
