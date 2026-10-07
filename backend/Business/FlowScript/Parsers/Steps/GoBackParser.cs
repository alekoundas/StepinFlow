using Business.FlowScript.Catalogs;
using Business.FlowScript.Models.Text;
using Core.Enums;
using Core.Models.Database;

namespace Business.FlowScript.Parsers.Steps
{
    internal sealed class GoBackParser : BaseStepParser
    {
        public GoBackParser(IReadOnlyList<ScriptToken> tokens, ScriptScope scope) : base(tokens, scope)
        {
        }

        public override FlowStep Parse()
        {
            // Go Back  to <[ step ]>
            ExpectKeyword(FlowStepTypeEnum.GO_BACK);
            ExpectKeyword(ScriptSymbolEnum.TO);

            FlowStep? reference = ExtractStepReference();

            ExpectEnd();

            return new FlowStep() { FlowStepType = FlowStepTypeEnum.GO_BACK, FlowStepReference = reference };
        }
    }
}
