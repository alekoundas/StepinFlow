using Business.FlowScript.Catalogs;
using Core.Enums;
using Core.Models.Database;

namespace Business.FlowScript.Text.Steps
{
    internal sealed class WindowRelocateWriter : BaseStepWriter
    {
        public WindowRelocateWriter(FlowStep step) : base(step)
        {
        }

        protected override void Compose()
        {
            // Move Window  <[ Browser ]>   to point <[ X ]>
            WriteKeyword(FlowStepTypeEnum.WINDOW_RELOCATE);
            WriteGapUntil(KEYWORD_GAP_UNTIL);
            WriteWindowArea();

            WriteGap();
            WriteKeyword(ScriptSymbolEnum.TO);
            WriteTarget(Step.FlowPoint, null);
        }
    }
}
