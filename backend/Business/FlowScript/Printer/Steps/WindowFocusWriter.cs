using Core.Enums;
using Core.Models.Database;

namespace Business.FlowScript.Text.Steps
{
    internal sealed class WindowFocusWriter : BaseStepWriter
    {
        public WindowFocusWriter(FlowStep step) : base(step)
        {
        }

        protected override void Compose()
        {
            // Focus Window  <[ Browser ]>
            WriteKeyword(FlowStepTypeEnum.WINDOW_FOCUS);
            WriteGapUntil(KEYWORD_GAP_UNTIL);
            WriteWindowArea();
        }
    }
}
