using Business.FlowScript.Catalogs;
using Business.FlowScript.Diagnostics;
using Business.FlowScript.Models.Text;
using Core.Enums;
using Core.Helpers;
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
            // Launch  <[ name ]>   <[ command ]> | Launch  <[ name ]>   opens <[ area ]>  [with <[ arguments ]>] | Run  <[ name ]>   [KILL_PROCESS ...] <[ command ]>
            ScriptKeyword keyword = ExtractStepKeyword(FlowStepTypeEnum.SYSTEM_COMMAND);

            FlowStep step = new FlowStep() { FlowStepType = FlowStepTypeEnum.SYSTEM_COMMAND, RunCommandPreset = keyword.As<RunCommandPresetEnum>()!.Value };

            // A file from before the line had a name wrote none, and the import names the step. There
            // a lone quote is the command.
            string? first = ExtractOptionalQuote();
            if (first != null && ExpectOptionalEnd())
            {
                step.RunCommandValue = first;
                return step;
            }

            if (first != null && !Scope.IsLanguageDeclared && step.RunCommandPreset == RunCommandPresetEnum.LAUNCH_APP)
                return ReadAsLaunchOf(step, first);

            if (first != null)
                step.Name = first;

            // Launch is a preset already. Run may name one before its value.
            if (step.RunCommandPreset == RunCommandPresetEnum.CUSTOM)
                step.RunCommandPreset = ExtractOptionalKeyword<RunCommandPresetEnum>() ?? RunCommandPresetEnum.CUSTOM;

            // The window it opens: left alone when it is open already, waited for when it is not.
            // The area says what starts it, and the launch only what goes after.
            if (step.RunCommandPreset == RunCommandPresetEnum.LAUNCH_APP)
                step.FlowArea = ExtractOptionalArea(ScriptSymbolEnum.OPENS);

            if (step.FlowArea != null)
            {
                if (ExpectOptionalKeyword(ScriptSymbolEnum.WITH))
                {
                    ExpectKeyword(ScriptSymbolEnum.QUOTE_OPEN);
                    step.LaunchArguments = ExtractText();
                    ExpectKeyword(ScriptSymbolEnum.QUOTE_CLOSE);
                }

                ExpectEnd();
                return step;
            }

            ExpectKeyword(ScriptSymbolEnum.QUOTE_OPEN);
            step.RunCommandValue = ExtractText();
            ExpectKeyword(ScriptSymbolEnum.QUOTE_CLOSE);

            ExpectEnd();

            return step;
        }


        // ================================================================
        // Private methods
        // ================================================================

        // Written before 2026-11-01, a launch of an area carried its whole command line where the
        // name now goes: Launch  <[ command ]>  opens <[ area ]>. Its program is what starts the
        // area, unless the area already says, and the rest are its arguments.
        private FlowStep ReadAsLaunchOf(FlowStep step, string commandLine)
        {
            step.RunCommandValue = commandLine;

            ScriptToken at = CurrentToken;
            FlowArea? opens = ExtractOptionalArea(ScriptSymbolEnum.OPENS);
            ExpectEnd();

            if (opens == null)
                return step;

            string program = CommandLineHelper.SplitProgram(commandLine, out string arguments);
            string quoted = program.Contains(' ', StringComparison.Ordinal) ? $"\"{program}\"" : program;

            if (string.IsNullOrWhiteSpace(opens.LaunchCommand))
                opens.LaunchCommand = quoted;

            string startedBy = CommandLineHelper.SplitProgram(opens.LaunchCommand, out _);
            if (!startedBy.Equals(program, StringComparison.OrdinalIgnoreCase))
                Scope.Warn(DiagnosticCodeEnum.LAUNCH_PROGRAM_DIFFERS, at,
                    $"\"{opens.Name}\" is started by {opens.LaunchCommand}, so this launch starts that, with its arguments, rather than {quoted}.");

            step.FlowArea = opens;
            step.LaunchArguments = arguments;
            step.RunCommandValue = string.Empty;

            return step;
        }
    }
}
