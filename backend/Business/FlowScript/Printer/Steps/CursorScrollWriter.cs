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
            // Scroll  down 3   in <[ area ]>

            WriteKeyword(FlowStepTypeEnum.CURSOR_SCROLL);
            WriteGapUntil(KEYWORD_GAP_UNTIL);

            // No direction goes down, which is what a scroll did before there was one.
            WriteKeyword(Step.CursorScrollDirectionType ?? CursorScrollDirectionTypeEnum.DOWN);
            WriteNumber(Step.LoopCount);

            // The area to scroll inside, not a point: a target would fall through to "match" for a
            // scroll that names neither, which is a line the parser cannot read back.
            WriteArea();
        }
    }
}
