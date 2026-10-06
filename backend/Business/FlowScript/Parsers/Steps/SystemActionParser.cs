using Business.FlowScript.Models.Binding;
using Business.FlowScript.Models.Text;
using Core.Enums;
using Core.Models.Database;

namespace Business.FlowScript.Parsers.Steps
{
    internal sealed class SystemActionParser : StepParser
    {
        public SystemActionParser(IReadOnlyList<ScriptToken> tokens) : base(tokens)
        {
        }

        public override FlowStepSchemaBindng Parse()
        {
            // System  LOCK_WORKSTATION
            ExpectKeyword(FlowStepTypeEnum.SYSTEM_ACTION);
            SystemActionTypeEnum action = ExtractKeyword<SystemActionTypeEnum>();
            ExpectEnd();

            return new FlowStepSchemaBindng()
            {
                Step = new FlowStep() { FlowStepType = FlowStepTypeEnum.SYSTEM_ACTION, SystemActionType = action },
            };
        }
    }
}
