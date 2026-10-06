using Business.FlowScript.Models.Binding;
using Business.FlowScript.Models.Text;
using Core.Enums;
using Core.Models.Database;

namespace Business.FlowScript.Parsers.Steps
{
    internal sealed class CursorScrollParser : StepParser
    {
        public CursorScrollParser(IReadOnlyList<ScriptToken> tokens) : base(tokens)
        {
        }

        public override FlowStepSchemaBindng Parse()
        {
            // Scroll  down 3   in <[ area ]>
            FlowStepSchemaBindng result = new FlowStepSchemaBindng();

            ExpectKeyword(FlowStepTypeEnum.CURSOR_SCROLL);
            result.Step = new FlowStep() { FlowStepType = FlowStepTypeEnum.CURSOR_SCROLL };

            result.Step.CursorScrollDirectionType = ExtractKeyword<CursorScrollDirectionTypeEnum>();
            result.Step.LoopCount = ExtractInteger();
            result.AreaName = ExtractOptionalArea();

            ExpectEnd();

            return result;
        }
    }
}
