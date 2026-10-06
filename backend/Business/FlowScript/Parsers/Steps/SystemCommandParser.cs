using Business.FlowScript.Catalogs;
using Business.FlowScript.Models.Binding;
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

        public override FlowStepSchemaBindng Parse()
        {
            // Launch  <[ command ]> | Run  [KILL_PROCESS ...] <[ command ]>
            FlowStepSchemaBindng result = new FlowStepSchemaBindng();
            ScriptKeyword keyword = ExtractStepKeyword(FlowStepTypeEnum.SYSTEM_COMMAND);

            result.Step = new FlowStep() { FlowStepType = FlowStepTypeEnum.SYSTEM_COMMAND, RunCommandPreset = keyword.As<RunCommandPresetEnum>()!.Value };

            // Launch is a preset already. Run may name one before its value.
            if (result.Step.RunCommandPreset == RunCommandPresetEnum.CUSTOM)
                result.Step.RunCommandPreset = ExtractOptionalKeyword<RunCommandPresetEnum>() ?? RunCommandPresetEnum.CUSTOM;

            ExpectKeyword(ScriptSymbolEnum.QUOTE_OPEN);
            result.Step.RunCommandValue = ExtractText();
            ExpectKeyword(ScriptSymbolEnum.QUOTE_CLOSE);

            ExpectEnd();

            return result;
        }
    }
}
