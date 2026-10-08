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
            // Move Window  process <[ chrome.exe ]>   [title starts with <[ Swag ]>]   to point <[ X ]>
            WriteKeyword(FlowStepTypeEnum.WINDOW_RELOCATE);
            WriteGapUntil(KEYWORD_GAP_UNTIL);
            WriteKeyword(ScriptSymbolEnum.PROCESS);
            WriteQuote(Step.ProcessName);

            if (!string.IsNullOrWhiteSpace(Step.TitlePattern))
            {
                WriteKeyword(ScriptSymbolEnum.TITLE);
                WriteKeyword(Step.TitleMatchMode);
                WriteQuote(Step.TitlePattern);
            }

            WriteGap();
            WriteKeyword(ScriptSymbolEnum.TO);
            WriteTarget(Step.FlowPoint, null);
        }
    }
}
