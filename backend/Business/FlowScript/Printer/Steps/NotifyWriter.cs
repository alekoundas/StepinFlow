using Business.FlowScript.Catalogs;
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
            // Notify  <[ message ]>   [with result of <[ a step ]>]
            WriteKeyword(FlowStepTypeEnum.NOTIFY);
            WriteGapUntil(KEYWORD_GAP_UNTIL);
            WriteQuote(Step.Message);

            if (Step.FlowStepReference == null)
                return;

            WriteGap();
            WriteKeyword(ScriptSymbolEnum.WITH_RESULT_OF);
            WriteQuote(Step.FlowStepReference.Name);
        }
    }
}
