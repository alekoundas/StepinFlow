using Business.FlowScript.Catalogs;
using Business.FlowScript.Models.Text;
using Core.Enums;
using Core.Models.Database;

namespace Business.FlowScript.Parsers.Steps
{
    internal sealed class SearchTextParser : BaseStepParser
    {
        public SearchTextParser(IReadOnlyList<ScriptToken> tokens, ScriptScope scope) : base(tokens, scope)
        {
        }

        public override FlowStep Parse()
        {
            // Check Text  <[ name ]>   contains <[ x ]>   in <[ area ]>   keep <[ pattern ]>   timeout 10000ms
            ScriptKeyword keyword = ExtractStepKeyword(FlowStepTypeEnum.SEARCH_TEXT);

            ExpectKeyword(ScriptSymbolEnum.QUOTE_OPEN);
            string name = ExtractText();
            ExpectKeyword(ScriptSymbolEnum.QUOTE_CLOSE);

            FlowStep step = new FlowStep()
            {
                FlowStepType = FlowStepTypeEnum.SEARCH_TEXT,
                SearchMode = keyword.As<SearchModeEnum>()!.Value,
                Name = name
            };

            ExpectCondition(step);
            step.FlowArea = ExtractOptionalArea();

            if (ExpectOptionalKeyword(ScriptSymbolEnum.KEEP))
            {
                ExpectKeyword(ScriptSymbolEnum.QUOTE_OPEN);
                step.ResultExtractPattern = ExtractText();
                ExpectKeyword(ScriptSymbolEnum.QUOTE_CLOSE);
            }

            // Check Optional Timeout.
            if (ExpectOptionalKeyword(ScriptSymbolEnum.TIMEOUT))
                step.TimeoutMilliseconds = ExtractMilliseconds();
            else if (ExpectOptionalKeyword(ScriptSymbolEnum.NO_TIMEOUT))
                step.TimeoutMilliseconds = 0;

            ExpectEnd();

            return step;
        }
    }
}
