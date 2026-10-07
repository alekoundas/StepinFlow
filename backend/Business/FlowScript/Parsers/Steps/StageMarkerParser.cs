using Business.FlowScript.Models.Text;
using Core.Enums;
using Core.Models.Database;

namespace Business.FlowScript.Parsers.Steps
{
    internal sealed class StageMarkerParser : BaseStepParser
    {
        public StageMarkerParser(IReadOnlyList<ScriptToken> tokens, ScriptScope scope) : base(tokens, scope)
        {
        }

        public override FlowStep Parse()
        {
            // ## Sign in
            ExpectKeyword(FlowStepTypeEnum.STAGE_MARKER);
            string name = ExtractText();
            ExpectEnd();

            return new FlowStep() { FlowStepType = FlowStepTypeEnum.STAGE_MARKER, Name = name };
        }
    }
}
