using Core.Enums;
using Core.Models.Database;

namespace Business.FlowScript.Text.Steps
{
    internal sealed class NotifyWriter : BaseStepWriter
    {
        public NotifyWriter(FlowStep step) : base(step)
        {
        }

        protected override void Compose()
        {
            // Notify  <[ message ]>
            WriteKeyword(FlowStepTypeEnum.NOTIFY);
            WriteGapUntil(KEYWORD_GAP_UNTIL);
            WriteQuote(Step.Message);
        }
    }
}
