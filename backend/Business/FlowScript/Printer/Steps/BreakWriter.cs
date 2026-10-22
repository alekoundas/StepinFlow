using Business.FlowScript.Catalogs;
using Core.Enums;
using Core.Models.Database;

namespace Business.FlowScript.Text.Steps
{
    internal sealed class BreakWriter : BaseStepWriter
    {
        public BreakWriter(FlowStep step) : base(step)
        {
        }

        protected override void Compose()
        {
            // Break  out of <[ step ]>
            WriteKeyword(FlowStepTypeEnum.BREAK);
            WriteGapUntil(KEYWORD_GAP_UNTIL);
            WriteKeyword(ScriptSymbolEnum.OUT_OF);
            WriteQuote(Step.FlowStepReference?.Name ?? string.Empty);
        }
    }
}
