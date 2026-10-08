using Business.FlowScript.Catalogs;
using Core.Enums;
using Core.Models.Database;

namespace Business.FlowScript.Text.Steps
{
    internal sealed class SearchImageWriter : BaseStepWriter
    {
        public SearchImageWriter(FlowStep step) : base(step)
        {
        }

        protected override void Compose()
        {
            // Find Image  <[ name ]>   template <[ a.png ]> accuracy 0.9 required ...   match shape   in <[ area ]>   timeout 10000ms
            WriteKeyword(FlowStepTypeEnum.SEARCH_IMAGE, Step.SearchMode);
            WriteGapUntil(KEYWORD_GAP_UNTIL);
            WriteQuote(Step.Name);

            // The first a clause apart from the name, the rest closer, so they read as one list.
            int gap = 3;
            foreach (FlowStepTemplate template in Step.FlowStepTemplates.OrderBy(x => x.OrderNumber))
            {
                WriteGap(gap);
                WriteTemplate(template);
                gap = 2;
            }

            // Only when it is not the default, which is nearly every step.
            if (Step.TemplateMatchMode != TemplateMatchModeEnum.SHAPE)
            {
                WriteGap();
                WriteKeyword(ScriptSymbolEnum.MATCH);
                WriteKeyword(Step.TemplateMatchMode);
            }

            WriteArea();
            WriteWaiting();
        }


        // ================================================================
        // Private methods
        // ================================================================

        // Straight after its template, so it reads as that template's and not the step's.
        private void WriteTemplate(FlowStepTemplate template)
        {
            WriteKeyword(ScriptSymbolEnum.TEMPLATE);
            WriteQuote(template.Name);
            WriteKeyword(ScriptSymbolEnum.ACCURACY);
            WriteNumber(template.Accuracy);

            if (template.IsRequired)
                WriteKeyword(ScriptSymbolEnum.REQUIRED);
        }
    }
}
