using Business.FlowScript.Models.Text;
using Core.Enums;
using Core.Models.Database;

namespace Business.FlowScript.Parsers.Steps
{
    internal sealed class WindowFocusParser : BaseStepParser
    {
        public WindowFocusParser(IReadOnlyList<ScriptToken> tokens, ScriptScope scope) : base(tokens, scope)
        {
        }

        public override FlowStep Parse()
        {
            // Focus Window  process <[ chrome.exe ]>   [title starts with <[ Swag ]>]
            ExpectKeyword(FlowStepTypeEnum.WINDOW_FOCUS);
            FlowStep step = new FlowStep() { FlowStepType = FlowStepTypeEnum.WINDOW_FOCUS };

            ExpectWindow(step);

            ExpectEnd();

            return step;
        }
    }
}
