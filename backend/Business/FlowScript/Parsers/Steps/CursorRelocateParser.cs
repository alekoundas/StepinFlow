using Business.FlowScript.Catalogs;
using Business.FlowScript.Models.Text;
using Core.Enums;
using Core.Models.Database;

namespace Business.FlowScript.Parsers.Steps
{
    internal sealed class CursorRelocateParser : BaseStepParser
    {
        public CursorRelocateParser(IReadOnlyList<ScriptToken> tokens) : base(tokens)
        {
        }

        public override FlowStep Parse()
        {
            // Move  to point <[ X ]> | to <[ step ]> | to match
            ExpectKeyword(FlowStepTypeEnum.CURSOR_RELOCATE);
            FlowStep step = new FlowStep() { FlowStepType = FlowStepTypeEnum.CURSOR_RELOCATE };

            ExpectKeyword(ScriptSymbolEnum.TO);
            (step.FlowPoint, step.FlowStepReference) = ExtractTarget();

            ExpectEnd();

            return step;
        }
    }
}
