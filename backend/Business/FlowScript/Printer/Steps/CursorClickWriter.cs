using Business.FlowScript.Catalogs;
using Core.Enums;
using Core.Models.Database;

namespace Business.FlowScript.Text.Steps
{
    internal sealed class CursorClickWriter : BaseStepWriter
    {
        public CursorClickWriter(FlowStep step) : base(step)
        {
        }

        protected override void Compose()
        {
            // Click  at <[ step ]> | at point <[ X ]> | at match   [right | middle] [double | hold | release]
            WriteKeyword(FlowStepTypeEnum.CURSOR_CLICK);
            WriteGapUntil(KEYWORD_GAP_UNTIL);
            WriteKeyword(ScriptSymbolEnum.AT);
            WriteTarget(Step.FlowPoint, Step.FlowStepReference);

            // The button and what it does, left out entirely when it is a plain left click - which is
            // nearly every click, and saying so on every line would bury the ones that differ.
            CursorButtonTypeEnum button = Step.CursorButtonType ?? CursorButtonTypeEnum.LEFT_BUTTON;
            CursorButtonActionTypeEnum action = Step.CursorButtonActionType ?? CursorButtonActionTypeEnum.SINGLE_CLICK;

            if (button != CursorButtonTypeEnum.LEFT_BUTTON || action != CursorButtonActionTypeEnum.SINGLE_CLICK)
                WriteGap();

            if (button != CursorButtonTypeEnum.LEFT_BUTTON)
                WriteKeyword(button);

            if (action != CursorButtonActionTypeEnum.SINGLE_CLICK)
                WriteKeyword(action);
        }
    }
}
