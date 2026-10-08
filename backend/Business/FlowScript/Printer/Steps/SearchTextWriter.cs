using Business.FlowScript.Catalogs;
using Core.Enums;
using Core.Models.Database;

namespace Business.FlowScript.Text.Steps
{
    internal sealed class SearchTextWriter : BaseStepWriter
    {
        public SearchTextWriter(FlowStep step) : base(step)
        {
        }

        protected override void Compose()
        {
            // Check Text  <[ name ]>   contains <[ x ]>   in <[ area ]>   keep <[ pattern ]>   timeout 10000ms
            WriteKeyword(FlowStepTypeEnum.SEARCH_TEXT, Step.SearchMode);
            WriteGapUntil(KEYWORD_GAP_UNTIL);
            WriteQuote(Step.Name);

            if (Step.ConditionType != null)
            {
                WriteGap();
                WriteCondition();
            }

            WriteArea();

            if (!string.IsNullOrWhiteSpace(Step.ResultExtractPattern))
            {
                WriteGap();
                WriteKeyword(ScriptSymbolEnum.KEEP);
                WriteQuote(Step.ResultExtractPattern);
            }

            WriteWaiting();
        }
    }
}
