using Business.FlowScript.Catalogs;
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
            // Focus Window  process <[ chrome.exe ]>   [title starts with <[ Swag ]>]
            WriteKeyword(FlowStepTypeEnum.WINDOW_FOCUS);
            WriteGapUntil(KEYWORD_GAP_UNTIL);
            WriteKeyword(ScriptSymbolEnum.PROCESS);
            WriteQuote(Step.ProcessName);

            if (!string.IsNullOrWhiteSpace(Step.TitlePattern))
            {
                WriteKeyword(ScriptSymbolEnum.TITLE);
                WriteKeyword(Step.TitleMatchMode);
                WriteQuote(Step.TitlePattern);
            }
        }
    }
}
