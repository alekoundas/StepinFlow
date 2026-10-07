using Business.FlowScript.Catalogs;
using Business.FlowScript.Models.Text;
using Core.Enums;
using Core.Models.Database;

namespace Business.FlowScript.Parsers
{
    /// <summary>
    /// The clauses more than one step takes, each read the one way the printer writes it.
    ///
    /// A name is resolved against the scope as it is read, so the step comes back linked to the
    /// rows the lines above declared.
    /// </summary>
    internal abstract class BaseStepParser : BaseParser<FlowStep>
    {
        protected BaseStepParser(IReadOnlyList<ScriptToken> tokens, ScriptScope scope) : base(tokens)
        {
            Scope = scope;
        }

        protected ScriptScope Scope { get; }


        // ================================================================
        // Protected methods
        // ================================================================

        // point <[ X ]> | <[ a step ]> | match - the current item of a loop, neither a point nor a step, so both null.
        protected (FlowPoint? Point, FlowStep? Reference) ExtractTarget()
        {
            if (ExpectOptionalKeyword(ScriptSymbolEnum.POINT))
            {
                ExpectKeyword(ScriptSymbolEnum.QUOTE_OPEN);
                ScriptToken pointAt = CurrentToken;
                FlowPoint? point = Scope.Point(ExtractText(), pointAt);
                ExpectKeyword(ScriptSymbolEnum.QUOTE_CLOSE);

                return (point, null);
            }

            if (ExpectOptionalKeyword(ScriptSymbolEnum.MATCH))
                return (null, null);

            return (null, ExtractStepReference());
        }

        // <[ a step ]>
        protected FlowStep? ExtractStepReference()
        {
            ExpectKeyword(ScriptSymbolEnum.QUOTE_OPEN);
            ScriptToken at = CurrentToken;
            FlowStep? reference = Scope.Step(ExtractText(), at);
            ExpectKeyword(ScriptSymbolEnum.QUOTE_CLOSE);

            return reference;
        }

        // is <[ x ]>, between <[ 1 ]> and <[ 9 ]>, is empty, ...
        protected void ExpectCondition(FlowStep step)
        {
            ConditionTypeEnum condition = ExtractKeyword<ConditionTypeEnum>();

            step.ConditionType = condition;
            step.ConditionText = string.Empty;
            step.ConditionTextEnd = string.Empty;

            // Empty and not empty compare against nothing, and only between takes a second value.
            if (condition != ConditionTypeEnum.IS_EMPTY && condition != ConditionTypeEnum.IS_NOT_EMPTY)
            {
                ExpectKeyword(ScriptSymbolEnum.QUOTE_OPEN);
                step.ConditionText = ExtractText();
                ExpectKeyword(ScriptSymbolEnum.QUOTE_CLOSE);
            }

            if (condition == ConditionTypeEnum.BETWEEN)
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
            ScriptToken at = CurrentToken;
            FlowArea? area = Scope.Area(ExtractText(), at);
            ExpectKeyword(ScriptSymbolEnum.QUOTE_CLOSE);

            return area;
        }
    }
}
