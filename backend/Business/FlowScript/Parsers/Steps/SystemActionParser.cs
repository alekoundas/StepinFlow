using Business.FlowScript.Models.Text;
using Core.Enums;
using Core.Models.Database;

namespace Business.FlowScript.Parsers.Steps
{
    internal sealed class SystemActionParser : BaseStepParser
    {
        public SystemActionParser(IReadOnlyList<ScriptToken> tokens, ScriptScope scope) : base(tokens, scope)
        {
        }

        public override FlowStep Parse()
        {
            // System  LOCK_WORKSTATION
            ExpectKeyword(FlowStepTypeEnum.SYSTEM_ACTION);
            SystemActionTypeEnum action = ExtractKeyword<SystemActionTypeEnum>();
            ExpectEnd();

            return new FlowStep() { FlowStepType = FlowStepTypeEnum.SYSTEM_ACTION, SystemActionType = action };
        }
    }
}
