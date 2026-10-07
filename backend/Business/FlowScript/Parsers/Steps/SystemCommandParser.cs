using Business.FlowScript.Catalogs;
using Business.FlowScript.Models.Text;
using Core.Enums;
using Core.Models.Database;

namespace Business.FlowScript.Parsers.Steps
{
    internal sealed class SystemCommandParser : BaseStepParser
    {
        public SystemCommandParser(IReadOnlyList<ScriptToken> tokens) : base(tokens)
        {
        }

        public override FlowStep Parse()
        {
            // Launch  <[ command ]> | Run  [KILL_PROCESS ...] <[ command ]>
            ScriptKeyword keyword = ExtractStepKeyword(FlowStepTypeEnum.SYSTEM_COMMAND);

            FlowStep step = new FlowStep() { FlowStepType = FlowStepTypeEnum.SYSTEM_COMMAND, RunCommandPreset = keyword.As<RunCommandPresetEnum>()!.Value };

            // Launch is a preset already. Run may name one before its value.
            if (step.RunCommandPreset == RunCommandPresetEnum.CUSTOM)
                step.RunCommandPreset = ExtractOptionalKeyword<RunCommandPresetEnum>() ?? RunCommandPresetEnum.CUSTOM;

            ExpectKeyword(ScriptSymbolEnum.QUOTE_OPEN);
            step.RunCommandValue = ExtractText();
            ExpectKeyword(ScriptSymbolEnum.QUOTE_CLOSE);

            ExpectEnd();

            return step;
        }
    }
}
