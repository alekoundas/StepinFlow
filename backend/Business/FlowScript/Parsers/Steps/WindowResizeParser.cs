using Business.FlowScript.Catalogs;
using Business.FlowScript.Models.Text;
using Core.Enums;
using Core.Models.Database;

namespace Business.FlowScript.Parsers.Steps
{
    internal sealed class WindowResizeParser : BaseStepParser
    {
        public WindowResizeParser(IReadOnlyList<ScriptToken> tokens) : base(tokens)
        {
        }

        public override FlowStep Parse()
        {
            // Resize Window  process <[ chrome.exe ]>   [title starts with <[ Swag ]>]   size 1280 720
            ExpectKeyword(FlowStepTypeEnum.WINDOW_RESIZE);
            FlowStep step = new FlowStep() { FlowStepType = FlowStepTypeEnum.WINDOW_RESIZE };

            ExpectWindow(step);

            ExpectKeyword(ScriptSymbolEnum.SIZE);
            step.WindowWidth = ExtractInteger();
            step.WindowHeight = ExtractInteger();

            ExpectEnd();

            return step;
        }
    }
}
