using Business.FlowScript.Catalogs;
using Business.FlowScript.Syntax;
using Core.Enums;
using Core.Models.Database;
using Core.Models.Dtos;

namespace Business.Validation.Rules
{
    /// <summary>
    /// Text the flow script could not hold. The script writes text between <c>&lt;[</c> and
    /// <c>]&gt;</c> and escapes nothing, so a name, a message or a command holding either one would
    /// end its text early on export. The forms refuse it as it is typed; this catches what arrived
    /// another way.
    /// </summary>
    public static class ScriptTextValidator
    {
        public static void Validate(IReadOnlyList<FlowStep> authoredSteps, IReadOnlyList<FlowArea> areas, IReadOnlyList<string> flowNames, FlowValidationResultDto result)
        {
            foreach (FlowStep step in authoredSteps)
            {
                Check(result, step.Id, step.Name, "Its name", step.Name);
                Check(result, step.Id, step.Name, "The message", step.Message);
                Check(result, step.Id, step.Name, "The text to type", step.KeyboardInputText);
                Check(result, step.Id, step.Name, "The command", step.RunCommandValue);
                Check(result, step.Id, step.Name, "The working directory", step.RunCommandWorkingDirectory);
                Check(result, step.Id, step.Name, "The value to check against", step.ConditionText);
                Check(result, step.Id, step.Name, "The upper bound", step.ConditionTextEnd);
                Check(result, step.Id, step.Name, "The pattern to keep", step.ResultExtractPattern);
                Check(result, step.Id, step.Name, "The process name", step.ProcessName);
                Check(result, step.Id, step.Name, "The window title", step.TitlePattern);
            }

            // Areas, points and inputs by name, then what an area matches a window or monitor by.
            foreach (string name in flowNames)
                Check(result, null, name, "The name", name);

            foreach (FlowArea area in areas)
            {
                Check(result, null, area.Name, "The process name", area.ProcessName);
                Check(result, null, area.Name, "The window title", area.TitlePattern);
                Check(result, null, area.Name, "The tab to match", area.TabMatchValue);
                Check(result, null, area.Name, "The monitor's device name", area.MonitorDeviceName);
            }
        }


        // ================================================================
        // Private methods
        // ================================================================

        private static void Check(FlowValidationResultDto result, int? stepId, string ownerName, string field, string text)
        {
            if (!SyntaxFacts.HasTextDelimiter(text))
                return;

            string textStart = SyntaxFacts.Symbol(ScriptSymbolEnum.TEXT_START);
            string textEnd = SyntaxFacts.Symbol(ScriptSymbolEnum.TEXT_END);

            result.Add(stepId, ownerName, ValidationSeverityEnum.ERROR, FlowValidationCodeEnum.TEXT_DELIMITER,
                $"{field} can't contain \"{textStart}\" or \"{textEnd}\": the flow script marks where text starts and ends with them.");
        }
    }
}
