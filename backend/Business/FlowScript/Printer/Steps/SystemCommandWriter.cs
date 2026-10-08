using Core.Enums;
using Core.Models.Database;

namespace Business.FlowScript.Text.Steps
{
    internal sealed class SystemCommandWriter : BaseStepWriter
    {
        public SystemCommandWriter(FlowStep step) : base(step)
        {
        }

        protected override void Compose()
        {
            // Launch  <[ command ]> | Run  [KILL_PROCESS ...] <[ command ]>
            if (Step.RunCommandPreset == RunCommandPresetEnum.LAUNCH_APP)
            {
                WriteKeyword(FlowStepTypeEnum.SYSTEM_COMMAND, RunCommandPresetEnum.LAUNCH_APP);
                WriteGapUntil(KEYWORD_GAP_UNTIL);
                WriteQuote(Step.RunCommandValue);
                return;
            }

            WriteKeyword(FlowStepTypeEnum.SYSTEM_COMMAND, RunCommandPresetEnum.CUSTOM);
            WriteGapUntil(KEYWORD_GAP_UNTIL);

            if (Step.RunCommandPreset != RunCommandPresetEnum.CUSTOM)
                WriteKeyword(Step.RunCommandPreset);

            WriteQuote(Step.RunCommandValue);
        }
    }
}
