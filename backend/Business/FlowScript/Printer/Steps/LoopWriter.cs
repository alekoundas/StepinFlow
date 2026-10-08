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
            // Loop  5 times | forever
            // Working through every hit of a search is the search's own Success branch, not a loop.
            WriteKeyword(FlowStepTypeEnum.LOOP);
            WriteGapUntil(KEYWORD_GAP_UNTIL);

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
