using Business.FlowScript.Catalogs;
using Core.Enums;
using Core.Models.Database;

namespace Business.FlowScript.Text.Steps
{
    internal sealed class WindowResizeWriter : BaseStepWriter
    {
        public WindowResizeWriter(FlowStep step) : base(step)
        {
        }

        protected override void Compose()
        {
            // Resize Window  <[ name ]>   <[ Browser ]>   size 1280 720
            WriteKeyword(FlowStepTypeEnum.WINDOW_RESIZE);
            WriteGapUntil(KEYWORD_GAP_UNTIL);
            WriteQuote(Step.Name);

            WriteGap();
            WriteQuote(Step.FlowArea?.Name);

            WriteGap();
            WriteKeyword(ScriptSymbolEnum.SIZE);
            WriteNumber(Step.WindowWidth);
            WriteNumber(Step.WindowHeight);
        }
    }
}
