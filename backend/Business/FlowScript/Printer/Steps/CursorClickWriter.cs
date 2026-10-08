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
            // Click   [right | middle] [double | hold | release]
            // Where the cursor is: a Move on the line above says where that is.
            WriteKeyword(FlowStepTypeEnum.CURSOR_CLICK);

            // The button and what it does, left out entirely when it is a plain left click - which is
            // nearly every click, and saying so on every line would bury the ones that differ.
            CursorButtonTypeEnum button = Step.CursorButtonType ?? CursorButtonTypeEnum.LEFT_BUTTON;
            CursorButtonActionTypeEnum action = Step.CursorButtonActionType ?? CursorButtonActionTypeEnum.SINGLE_CLICK;

            if (button != CursorButtonTypeEnum.LEFT_BUTTON || action != CursorButtonActionTypeEnum.SINGLE_CLICK)
                WriteGapUntil(KEYWORD_GAP_UNTIL);

            if (button != CursorButtonTypeEnum.LEFT_BUTTON)
                WriteKeyword(button);

            if (action != CursorButtonActionTypeEnum.SINGLE_CLICK)
                WriteKeyword(action);
        }
    }
}
