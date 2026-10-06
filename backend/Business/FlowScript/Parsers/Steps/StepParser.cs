using Business.FlowScript.Catalogs;
using Business.FlowScript.Models.Binding;
using Business.FlowScript.Models.Text;
using Business.Validation;
using Core.Enums;
using Core.Models.Database;

namespace Business.FlowScript.Parsers.Steps
{
    /// <summary>
    /// The clauses more than one step takes, each read the one way the printer writes it.
    /// </summary>
    internal abstract class StepParser : TokenParser<FlowStepSchemaBindng>
    {
        protected StepParser(IReadOnlyList<ScriptToken> tokens) : base(tokens)
        {
        }


        // ================================================================
        // Protected methods
        // ================================================================

        // point <[ X ]> | <[ a step ]> | match - the current item of a loop, neither a point nor a step, so both null.
        protected (string? PointName, string? ReferenceName) ExtractTarget()
        {
            if (ExpectOptionalKeyword(ScriptSymbolEnum.POINT))
            {
                ExpectKeyword(ScriptSymbolEnum.QUOTE_OPEN);
                string point = ExtractText();
                ExpectKeyword(ScriptSymbolEnum.QUOTE_CLOSE);

                return (point, null);
            }

            if (ExpectOptionalKeyword(ScriptSymbolEnum.MATCH))
                return (null, null);

            ExpectKeyword(ScriptSymbolEnum.QUOTE_OPEN);
            string reference = ExtractText();
            ExpectKeyword(ScriptSymbolEnum.QUOTE_CLOSE);

            return (null, reference);
        }

        // is <[ x ]>, between <[ 1 ]> and <[ 9 ]>, is empty, ...
        protected void ExpectCondition(FlowStep step)
        {
            ConditionTypeEnum condition = ExtractKeyword<ConditionTypeEnum>();

            step.ConditionType = condition;
            step.ConditionText = string.Empty;
            step.ConditionTextEnd = string.Empty;

            if (ConditionHelper.NeedsValue(condition))
            {
                ExpectKeyword(ScriptSymbolEnum.QUOTE_OPEN);
                step.ConditionText = ExtractText();
                ExpectKeyword(ScriptSymbolEnum.QUOTE_CLOSE);
            }

            if (ConditionHelper.NeedsSecondValue(condition))
            {
                ExpectKeyword(ScriptSymbolEnum.AND);
                ExpectKeyword(ScriptSymbolEnum.QUOTE_OPEN);
                step.ConditionTextEnd = ExtractText();
                ExpectKeyword(ScriptSymbolEnum.QUOTE_CLOSE);
            }
        }

        // process <[ chrome.exe ]>, and optionally title starts with <[ Swag ]>.
        protected void ExpectWindow(FlowStep step)
        {
            ExpectKeyword(ScriptSymbolEnum.PROCESS);
            ExpectKeyword(ScriptSymbolEnum.QUOTE_OPEN);
            step.ProcessName = ExtractText();
            ExpectKeyword(ScriptSymbolEnum.QUOTE_CLOSE);

            if (ExpectOptionalKeyword(ScriptSymbolEnum.TITLE))
            {
                step.TitleMatchMode = ExtractKeyword<TitleMatchModeEnum>();
                ExpectKeyword(ScriptSymbolEnum.QUOTE_OPEN);
                step.TitlePattern = ExtractText();
                ExpectKeyword(ScriptSymbolEnum.QUOTE_CLOSE);
            }
        }

        // in <[ area ]>, or null.
        protected string? ExtractOptionalArea()
        {
            if (!ExpectOptionalKeyword(ScriptSymbolEnum.IN))
                return null;

            ExpectKeyword(ScriptSymbolEnum.QUOTE_OPEN);
            string area = ExtractText();
            ExpectKeyword(ScriptSymbolEnum.QUOTE_CLOSE);

            return area;
        }
    }
}
