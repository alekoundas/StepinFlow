using Business.FlowScript.Catalogs;
using Core.Enums;
using Core.Models.Database;

namespace Business.FlowScript.Text.Steps
{
    internal sealed class LoopWriter : BaseStepWriter
    {
        public LoopWriter(FlowStep step) : base(step)
        {
        }

        protected override void Compose()
        {
            // Loop  5 times | forever | each match in <[ search ]>
            WriteKeyword(FlowStepTypeEnum.LOOP);
            WriteGapUntil(KEYWORD_GAP_UNTIL);

            // Three sources, and which one is in play is readable from the row rather than stored:
            // a reference means each match, no count means forever.
            if (Step.FlowStepReference != null)
            {
                WriteKeyword(ScriptSymbolEnum.EACH);
                WriteKeyword(ScriptSymbolEnum.MATCH);
                WriteKeyword(ScriptSymbolEnum.IN);
                WriteQuote(Step.FlowStepReference.Name);
                return;
            }

            if (Step.IsLoopInfinite)
            {
                WriteKeyword(ScriptSymbolEnum.FOREVER);
                return;
            }

            WriteNumber(Step.LoopCount);
            WriteKeyword(ScriptSymbolEnum.TIMES);
        }
    }
}
