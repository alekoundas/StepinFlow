using Business.FlowScript.Models.Text;
using Core.Enums;
using Core.Models.Database;

namespace Business.FlowScript.Parsers.Steps
{
    internal sealed class CursorScrollParser : BaseStepParser
    {
        public CursorScrollParser(IReadOnlyList<ScriptToken> tokens) : base(tokens)
        {
        }

        public override FlowStep Parse()
        {
            // Scroll  down 3   in <[ area ]>
            ExpectKeyword(FlowStepTypeEnum.CURSOR_SCROLL);
            FlowStep step = new FlowStep() { FlowStepType = FlowStepTypeEnum.CURSOR_SCROLL };

            step.CursorScrollDirectionType = ExtractKeyword<CursorScrollDirectionTypeEnum>();
            step.LoopCount = ExtractInteger();
            step.FlowArea = ExtractOptionalArea();

            ExpectEnd();

            return step;
        }
    }
}
