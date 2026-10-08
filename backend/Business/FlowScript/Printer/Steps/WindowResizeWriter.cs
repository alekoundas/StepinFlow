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
            // Resize Window  process <[ chrome.exe ]>   [title starts with <[ Swag ]>]   size 1280 720
            WriteKeyword(FlowStepTypeEnum.WINDOW_RESIZE);
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
            WriteKeyword(ScriptSymbolEnum.SIZE);
            WriteNumber(Step.WindowWidth);
            WriteNumber(Step.WindowHeight);
        }
    }
}
