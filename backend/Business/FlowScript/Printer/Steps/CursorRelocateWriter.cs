using Business.FlowScript.Catalogs;
using Core.Enums;
using Core.Models.Database;

namespace Business.FlowScript.Text.Steps
{
    internal sealed class CursorRelocateWriter : BaseStepWriter
    {
        public CursorRelocateWriter(FlowStep step) : base(step)
        {
        }

        protected override void Compose()
        {
            // Move  to point <[ X ]> | to <[ step ]> | to match
            WriteKeyword(FlowStepTypeEnum.CURSOR_RELOCATE);
            WriteGapUntil(KEYWORD_GAP_UNTIL);
            WriteKeyword(ScriptSymbolEnum.TO);
            WriteTarget(Step.FlowPoint, Step.FlowStepReference);
        }
    }
}
