using Business.FlowScript.Catalogs;
using Business.FlowScript.Models.Binding;
using Business.FlowScript.Models.Text;
using Core.Enums;
using Core.Models.Database;

namespace Business.FlowScript.Parsers.Steps
{
    internal sealed class SearchTextParser : BaseStepParser
    {
        public SearchTextParser(IReadOnlyList<ScriptToken> tokens) : base(tokens)
        {
        }

        public override FlowStepSchemaBindng Parse()
        {
            // Check Text  <[ name ]>   contains <[ x ]>   in <[ area ]>   keep <[ pattern ]>   timeout 10000ms
            FlowStepSchemaBindng result = new FlowStepSchemaBindng();
            ScriptKeyword keyword = ExtractStepKeyword(FlowStepTypeEnum.SEARCH_TEXT);

            ExpectKeyword(ScriptSymbolEnum.QUOTE_OPEN);
            string name = ExtractText();
            ExpectKeyword(ScriptSymbolEnum.QUOTE_CLOSE);

            result.Step = new FlowStep()
            {
                FlowStepType = FlowStepTypeEnum.SEARCH_TEXT,
                SearchMode = keyword.As<SearchModeEnum>()!.Value,
                Name = name
            };

            ExpectCondition(result.Step);
            result.AreaName = ExtractOptionalArea();

            if (ExpectOptionalKeyword(ScriptSymbolEnum.KEEP))
            {
                ExpectKeyword(ScriptSymbolEnum.QUOTE_OPEN);
                result.Step.ResultExtractPattern = ExtractText();
                ExpectKeyword(ScriptSymbolEnum.QUOTE_CLOSE);
            }

            // Check Optional Timeout.
            if (ExpectOptionalKeyword(ScriptSymbolEnum.TIMEOUT))
                result.Step.TimeoutMilliseconds = ExtractMilliseconds();
            else if (ExpectOptionalKeyword(ScriptSymbolEnum.NO_TIMEOUT))
                result.Step.TimeoutMilliseconds = 0;

            ExpectEnd();

            return result;
        }
    }
}
