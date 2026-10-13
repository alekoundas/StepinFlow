using Business.FlowScript.Catalogs;
using Business.FlowScript.Models.Text;
using Core.Enums;
using Core.Models.Database;

namespace Business.FlowScript.Parsers.Steps
{
    internal sealed class SystemCommandParser : BaseStepParser
    {
        public SystemCommandParser(IReadOnlyList<ScriptToken> tokens, ScriptScope scope) : base(tokens, scope)
        {
        }

        public override FlowStep Parse()
        {
            // Launch  <[ command ]>   [opens <[ area ]>] | Run  [KILL_PROCESS ...] <[ command ]>
            ScriptKeyword keyword = ExtractStepKeyword(FlowStepTypeEnum.SYSTEM_COMMAND);

            FlowStep step = new FlowStep() { FlowStepType = FlowStepTypeEnum.SYSTEM_COMMAND, RunCommandPreset = keyword.As<RunCommandPresetEnum>()!.Value };

            // Launch is a preset already. Run may name one before its value.
            if (step.RunCommandPreset == RunCommandPresetEnum.CUSTOM)
                step.RunCommandPreset = ExtractOptionalKeyword<RunCommandPresetEnum>() ?? RunCommandPresetEnum.CUSTOM;

            ExpectKeyword(ScriptSymbolEnum.QUOTE_OPEN);
            step.RunCommandValue = ExtractText();
            ExpectKeyword(ScriptSymbolEnum.QUOTE_CLOSE);

            // The window it opens: left alone when it is open already, waited for when it is not.
            if (step.RunCommandPreset == RunCommandPresetEnum.LAUNCH_APP)
                step.FlowArea = ExtractOptionalArea(ScriptSymbolEnum.OPENS);

            ExpectEnd();

            return step;
        }
    }
}
