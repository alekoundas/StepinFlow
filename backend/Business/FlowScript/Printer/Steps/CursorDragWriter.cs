using Business.FlowScript.Catalogs;
using Core.Enums;
using Core.Models.Database;

namespace Business.FlowScript.Text.Steps
{
    internal sealed class CursorDragWriter : BaseStepWriter
    {
        public CursorDragWriter(FlowStep step) : base(step)
        {
        }

        protected override void Compose()
        {
            // Drag  at point <[ A ]> to point <[ B ]>
            WriteKeyword(FlowStepTypeEnum.CURSOR_DRAG);
            WriteGapUntil(KEYWORD_GAP_UNTIL);
            WriteKeyword(ScriptSymbolEnum.AT);
            WriteTarget(Step.FlowPoint, Step.FlowStepReference);
            WriteKeyword(ScriptSymbolEnum.TO);
            WriteTarget(Step.FlowPointEnd, Step.FlowStepReferenceEnd);
        }
    }
}
