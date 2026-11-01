using Business.FlowScript.Catalogs;
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
            // Launch  <[ name ]>   <[ command ]> | Launch  <[ name ]>   opens <[ area ]>  [with <[ arguments ]>] | Run  <[ name ]>   [KILL_PROCESS ...] <[ command ]>
            if (Step.RunCommandPreset == RunCommandPresetEnum.LAUNCH_APP)
            {
                WriteKeyword(FlowStepTypeEnum.SYSTEM_COMMAND, RunCommandPresetEnum.LAUNCH_APP);
                WriteGapUntil(KEYWORD_GAP_UNTIL);
                WriteName();

                if (Step.FlowArea == null)
                {
                    WriteQuote(Step.RunCommandValue);
                    return;
                }

                // The area says what starts it.
                WriteKeyword(ScriptSymbolEnum.OPENS);
                WriteQuote(Step.FlowArea.Name);

                if (Step.LaunchArguments.Length > 0)
                {
                    WriteGap();
                    WriteKeyword(ScriptSymbolEnum.WITH);
                    WriteQuote(Step.LaunchArguments);
                }
                return;
            }

            WriteKeyword(FlowStepTypeEnum.SYSTEM_COMMAND, RunCommandPresetEnum.CUSTOM);
            WriteGapUntil(KEYWORD_GAP_UNTIL);
            WriteName();

            if (Step.RunCommandPreset != RunCommandPresetEnum.CUSTOM)
                WriteKeyword(Step.RunCommandPreset);

            WriteQuote(Step.RunCommandValue);
        }


        // ================================================================
        // Private methods
        // ================================================================

        // A check or typed text reads what the command said by its name. Left out with none, and
        // the import names it.
        private void WriteName()
        {
            if (string.IsNullOrWhiteSpace(Step.Name))
                return;

            WriteQuote(Step.Name);
            WriteGap();
        }
    }
}
