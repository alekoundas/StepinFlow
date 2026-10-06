using Business.FlowScript.Models.Binding;
using Business.FlowScript.Models.Text;
using Core.Enums;
using Core.Models.Database;

namespace Business.FlowScript.Parsers.Steps
{
    internal sealed class BranchParser : StepParser
    {
        public BranchParser(IReadOnlyList<ScriptToken> tokens) : base(tokens)
        {
        }

        public override FlowStepSchemaBindng Parse()
        {
            // Success: | Failure:
            FlowStepTypeEnum type = ExtractKeyword(FlowStepTypeEnum.SUCCESS, FlowStepTypeEnum.FAILURE);
            ExpectEnd();

            return new FlowStepSchemaBindng()
            {
                Step = new FlowStep() { FlowStepType = type },
            };
        }
    }
}
