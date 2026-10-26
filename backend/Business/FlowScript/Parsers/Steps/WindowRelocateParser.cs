using Business.FlowScript.Catalogs;
using Business.FlowScript.Models.Text;
using Core.Enums;
using Core.Models.Database;

namespace Business.FlowScript.Parsers.Steps
{
    internal sealed class WindowRelocateParser : BaseStepParser
    {
        public WindowRelocateParser(IReadOnlyList<ScriptToken> tokens, ScriptScope scope) : base(tokens, scope)
        {
        }

        public override FlowStep Parse()
        {
            // Move Window  <[ Browser ]>   to point <[ X ]>
            ExpectKeyword(FlowStepTypeEnum.WINDOW_RELOCATE);
            FlowStep step = new FlowStep() { FlowStepType = FlowStepTypeEnum.WINDOW_RELOCATE };

            step.FlowArea = ExtractWindowArea();

            ExpectKeyword(ScriptSymbolEnum.TO);
            (step.FlowPoint, step.FlowStepReference) = ExtractTarget();

            ExpectEnd();

            return step;
        }
    }
}
