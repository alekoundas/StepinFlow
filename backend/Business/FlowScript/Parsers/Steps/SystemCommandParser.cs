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
            // Launch  <[ name ]>   <[ command ]> | Launch  <[ name ]>   opens <[ area ]>  [with <[ arguments ]>] | Shell  <[ name ]>   [KILL_PROCESS ...] <[ command ]>
            ScriptKeyword keyword = ExtractStepKeyword(FlowStepTypeEnum.SYSTEM_COMMAND);

            FlowStep step = new FlowStep() { FlowStepType = FlowStepTypeEnum.SYSTEM_COMMAND, ShellCommandMode = keyword.As<ShellCommandModeEnum>()!.Value };

            ExpectKeyword(ScriptSymbolEnum.QUOTE_OPEN);
            step.Name = ExtractText();
            ExpectKeyword(ScriptSymbolEnum.QUOTE_CLOSE);

            // Launch is a mode already. Shell may name one before its value.
            if (step.ShellCommandMode == ShellCommandModeEnum.CUSTOM)
                step.ShellCommandMode = ExtractOptionalKeyword<ShellCommandModeEnum>() ?? ShellCommandModeEnum.CUSTOM;

            // The window it opens: left alone when it is open already, waited for when it is not.
            // The area says what starts it, and the launch only what goes after.
            if (step.ShellCommandMode == ShellCommandModeEnum.LAUNCH_APP)
                step.FlowArea = ExtractOptionalArea(ScriptSymbolEnum.OPENS);

            if (step.FlowArea != null)
            {
                if (ExpectOptionalKeyword(ScriptSymbolEnum.WITH))
                {
                    ExpectKeyword(ScriptSymbolEnum.QUOTE_OPEN);
                    step.ShellArguments = ExtractText();
                    ExpectKeyword(ScriptSymbolEnum.QUOTE_CLOSE);
                }

                ExpectEnd();
                return step;
            }

            ExpectKeyword(ScriptSymbolEnum.QUOTE_OPEN);
            step.ShellCommandValue = ExtractText();
            ExpectKeyword(ScriptSymbolEnum.QUOTE_CLOSE);

            ExpectEnd();

            return step;
        }
    }
}
