using Business.FlowScript.Catalogs;
using Core.Enums;
using Core.Models.Database;

namespace Business.FlowScript.Text.Steps
{
    internal sealed class GoBackWriter : BaseStepWriter
    {
        public GoBackWriter(FlowStep step) : base(step)
        {
        }

        protected override void Compose()
        {
            // Go Back  to <[ step ]>
            WriteKeyword(FlowStepTypeEnum.GO_BACK);
            WriteGapUntil(KEYWORD_GAP_UNTIL);
            WriteKeyword(ScriptSymbolEnum.TO);
            WriteQuote(Step.FlowStepReference?.Name ?? string.Empty);
        }
    }
}
