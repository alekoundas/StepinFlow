using Business.FlowScript.Models.Text;
using Core.Enums;
using Core.Models.Database;

namespace Business.FlowScript.Parsers.Steps
{
    internal sealed class BranchParser : BaseStepParser
    {
        public BranchParser(IReadOnlyList<ScriptToken> tokens, ScriptScope scope) : base(tokens, scope)
        {
        }

        public override FlowStep Parse()
        {
            // Success: | Failure:
            FlowStepTypeEnum type = ExtractKeyword(FlowStepTypeEnum.SUCCESS, FlowStepTypeEnum.FAILURE);
            ExpectEnd();

            return new FlowStep() { FlowStepType = type };
        }
    }
}
