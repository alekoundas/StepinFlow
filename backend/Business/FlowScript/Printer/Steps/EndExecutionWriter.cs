using Business.FlowScript.Catalogs;
using Core.Enums;
using Core.Models.Database;

namespace Business.FlowScript.Text.Steps
{
    internal sealed class EndExecutionWriter : BaseStepWriter
    {
        public EndExecutionWriter(FlowStep step) : base(step)
        {
        }

        protected override void Compose()
        {
            // End Execution  passed | failed <[ reason ]>
            WriteKeyword(FlowStepTypeEnum.END_EXECUTION);
            WriteGapUntil(KEYWORD_GAP_UNTIL);

            if (Step.EndExecutionAsSuccess)
            {
                WriteKeyword(ScriptSymbolEnum.PASSED);
                return;
            }

            WriteKeyword(ScriptSymbolEnum.FAILED);
            WriteGap(2);
            WriteQuote(Step.Message);
        }
    }
}
