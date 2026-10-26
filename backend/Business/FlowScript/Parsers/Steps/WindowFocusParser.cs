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
            // Focus Window  <[ Browser ]>
            ExpectKeyword(FlowStepTypeEnum.WINDOW_FOCUS);
            FlowStep step = new FlowStep() { FlowStepType = FlowStepTypeEnum.WINDOW_FOCUS };

            step.FlowArea = ExtractWindowArea();

            ExpectEnd();

            return step;
        }
    }
}
