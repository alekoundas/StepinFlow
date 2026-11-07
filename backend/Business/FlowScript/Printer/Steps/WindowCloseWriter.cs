using Business.FlowScript.Catalogs;
using Core.Enums;
using Core.Models.Database;

namespace Business.FlowScript.Text.Steps
{
    internal sealed class WindowCloseWriter : BaseStepWriter
    {
        public WindowCloseWriter(FlowStep step) : base(step)
        {
        }

        protected override void Compose()
        {
            // Close Window  <[ name ]>   <[ Notepad ]>   timeout 10000ms   then force
            WriteKeyword(FlowStepTypeEnum.WINDOW_CLOSE);
            WriteGapUntil(KEYWORD_GAP_UNTIL);
            WriteQuote(Step.Name);

            WriteGap();
            WriteQuote(Step.FlowArea?.Name);


            // Zero is the five seconds a close waits unless told otherwise.
            if (Step.TimeoutMilliseconds > 0)
            {
                WriteGap();
                WriteKeyword(ScriptSymbolEnum.TIMEOUT);
                WriteMilliseconds(Step.TimeoutMilliseconds);
            }

            if (Step.IsCloseForced)
            {
                WriteGap();
                WriteKeyword(ScriptSymbolEnum.THEN_FORCE);
            }
        }
    }
}
