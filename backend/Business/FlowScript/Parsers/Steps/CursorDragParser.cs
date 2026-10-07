using Business.FlowScript.Catalogs;
using Business.FlowScript.Models.Text;
using Core.Enums;
using Core.Models.Database;

namespace Business.FlowScript.Parsers.Steps
{
    internal sealed class CursorDragParser : BaseStepParser
    {
        public CursorDragParser(IReadOnlyList<ScriptToken> tokens) : base(tokens)
        {
        }

        public override FlowStep Parse()
        {
            // Drag  at point <[ A ]> to point <[ B ]>
            ExpectKeyword(FlowStepTypeEnum.CURSOR_DRAG);
            FlowStep step = new FlowStep() { FlowStepType = FlowStepTypeEnum.CURSOR_DRAG };

            ExpectKeyword(ScriptSymbolEnum.AT);
            (step.FlowPoint, step.FlowStepReference) = ExtractTarget();

            ExpectKeyword(ScriptSymbolEnum.TO);
            (step.FlowPointEnd, step.FlowStepReferenceEnd) = ExtractTarget();

            ExpectEnd();

            return step;
        }
    }
}
