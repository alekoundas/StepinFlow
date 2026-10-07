using Business.FlowScript.Catalogs;
using Business.FlowScript.Models.Text;
using Core.Enums;
using Core.Models.Database;

namespace Business.FlowScript.Parsers.Steps
{
    internal sealed class WindowRelocateParser : BaseStepParser
    {
        public WindowRelocateParser(IReadOnlyList<ScriptToken> tokens) : base(tokens)
        {
        }

        public override FlowStep Parse()
        {
            // Move Window  process <[ chrome.exe ]>   [title starts with <[ Swag ]>]   to point <[ X ]>
            ExpectKeyword(FlowStepTypeEnum.WINDOW_RELOCATE);
            FlowStep step = new FlowStep() { FlowStepType = FlowStepTypeEnum.WINDOW_RELOCATE };

            ExpectWindow(step);

            ExpectKeyword(ScriptSymbolEnum.TO);
            (step.FlowPoint, step.FlowStepReference) = ExtractTarget();

            ExpectEnd();

            return step;
        }
    }
}
