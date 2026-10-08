using Core.Enums;
using Core.Models.Database;

namespace Business.FlowScript.Text.Steps
{
    internal sealed class CursorScrollWriter : BaseStepWriter
    {
        public CursorScrollWriter(FlowStep step) : base(step)
        {
        }

        protected override void Compose()
        {
            // Scroll  down 3
            // Where the cursor is, like a click.

            WriteKeyword(FlowStepTypeEnum.CURSOR_SCROLL);
            WriteGapUntil(KEYWORD_GAP_UNTIL);

            // No direction goes down, which is what a scroll did before there was one.
            WriteKeyword(Step.CursorScrollDirectionType ?? CursorScrollDirectionTypeEnum.DOWN);
            WriteNumber(Step.LoopCount);
        }
    }
}
