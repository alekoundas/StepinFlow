using Core.Enums;
using Core.Models.Database;

namespace Business.FlowScript.Text.Steps
{
    internal sealed class SystemActionWriter : BaseStepWriter
    {
        public SystemActionWriter(FlowStep step) : base(step)
        {
        }

        protected override void Compose()
        {
            // System  LOCK_WORKSTATION
            WriteKeyword(FlowStepTypeEnum.SYSTEM_ACTION);
            WriteGapUntil(KEYWORD_GAP_UNTIL);
            WriteKeyword(Step.SystemActionType);
        }
    }
}
