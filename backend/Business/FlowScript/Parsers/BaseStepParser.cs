using Business.FlowScript.Catalogs;
using Business.FlowScript.Models.Text;
using Business.Validation;
using Core.Enums;
using Core.Models.Database;

namespace Business.FlowScript.Parsers
{
    /// <summary>
    /// The clauses more than one step takes, each read the one way the printer writes it.
    ///
    /// A name is returned as written, as a row holding only that name. ScriptLineParser swaps it
    /// for the row declared above.
    /// </summary>
    internal abstract class BaseStepParser : BaseParser<FlowStep>
    {
        protected BaseStepParser(IReadOnlyList<ScriptToken> tokens) : base(tokens)
        {
        }


        // ================================================================
        // Protected methods
        // ================================================================

        // point <[ X ]> | <[ a step ]> | match - the current item of a loop, neither a point nor a step, so both null.
        protected (FlowPoint? Point, FlowStep? Reference) ExtractTarget()
        {
            if (ExpectOptionalKeyword(ScriptSymbolEnum.POINT))
            {
                ExpectKeyword(ScriptSymbolEnum.QUOTE_OPEN);
                FlowPoint point = new FlowPoint() { Name = ExtractText() };
                ExpectKeyword(ScriptSymbolEnum.QUOTE_CLOSE);

                return (point, null);
            }

            if (ExpectOptionalKeyword(ScriptSymbolEnum.MATCH))
                return (null, null);

            ExpectKeyword(ScriptSymbolEnum.QUOTE_OPEN);
            FlowStep reference = new FlowStep() { Name = ExtractText() };
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
        protected FlowArea? ExtractOptionalArea()
        {
            if (!ExpectOptionalKeyword(ScriptSymbolEnum.IN))
                return null;

            ExpectKeyword(ScriptSymbolEnum.QUOTE_OPEN);
            FlowArea area = new FlowArea() { Name = ExtractText() };
            ExpectKeyword(ScriptSymbolEnum.QUOTE_CLOSE);

            return area;
        }
    }
}
