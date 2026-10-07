using Business.FlowScript.Models.Text;
using Core.Enums;
using Core.Models.Database;

namespace Business.FlowScript.Parsers.Steps
{
    internal sealed class MarkerParser : BaseStepParser
    {
        public MarkerParser(IReadOnlyList<ScriptToken> tokens, ScriptScope scope) : base(tokens, scope)
        {
        }

        public override FlowStep Parse()
        {
            // ## Sign in
            ExpectKeyword(FlowStepTypeEnum.MARKER);
            string name = ExtractText();
            ExpectEnd();

            return new FlowStep() { FlowStepType = FlowStepTypeEnum.MARKER, Name = name };
        }
    }
}
