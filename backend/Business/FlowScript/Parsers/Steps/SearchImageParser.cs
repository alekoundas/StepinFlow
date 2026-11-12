using Business.FlowScript.Catalogs;
using Business.FlowScript.Models.Text;
using Core.Enums;
using Core.Models.Database;

namespace Business.FlowScript.Parsers.Steps
{
    internal sealed class SearchImageParser : BaseStepParser
    {
        public SearchImageParser(IReadOnlyList<ScriptToken> tokens, ScriptScope scope) : base(tokens, scope)
        {
        }

        public override FlowStep Parse()
        {
            // Find Image  <[ name ]>   template <[ a.png ]> accuracy 0.9 ...   match shape   in <[ area ]>   timeout 10000ms
            ScriptKeyword keyword = ExtractStepKeyword(FlowStepTypeEnum.SEARCH_IMAGE);

            ExpectKeyword(ScriptSymbolEnum.QUOTE_OPEN);
            string name = ExtractText();
            ExpectKeyword(ScriptSymbolEnum.QUOTE_CLOSE);

            FlowStep step = new FlowStep()
            {
                FlowStepType = FlowStepTypeEnum.SEARCH_IMAGE,
                SearchMode = keyword.As<SearchModeEnum>()!.Value,
                Name = name
            };

            // A template is named by its file, and comes with what the Templates lines say about it.
            // A template with no accuracy keeps the default, which is the same in both modes.
            List<FlowStepTemplate> templates = new List<FlowStepTemplate>();
            while (ExpectOptionalKeyword(ScriptSymbolEnum.TEMPLATE))
            {
                ExpectKeyword(ScriptSymbolEnum.QUOTE_OPEN);
                ScriptToken at = CurrentToken;
                string fileName = ExtractText();
                ExpectKeyword(ScriptSymbolEnum.QUOTE_CLOSE);

                // The step's own link, to the picture the Templates line describes: every step that
                // names the file shares it, while the accuracy is the step's.
                FlowTemplate picture = Scope.Template(fileName, at) ?? new FlowTemplate() { Name = fileName };
                FlowStepTemplate template = new FlowStepTemplate() { FlowTemplate = picture, OrderNumber = templates.Count };

                if (ExpectOptionalKeyword(ScriptSymbolEnum.ACCURACY))
                    template.Accuracy = ExtractFloat();

                templates.Add(template);
            }

            step.FlowStepTemplates = templates;

            if (ExpectOptionalKeyword(ScriptSymbolEnum.MATCH))
                step.TemplateMatchMode = ExtractKeyword<TemplateMatchModeEnum>();

            step.FlowArea = ExtractOptionalArea();

            // Optional timeout.
            if (ExpectOptionalKeyword(ScriptSymbolEnum.TIMEOUT))
                step.TimeoutMilliseconds = ExtractMilliseconds();
            else if (ExpectOptionalKeyword(ScriptSymbolEnum.NO_TIMEOUT))
                step.TimeoutMilliseconds = 0;

            ExpectEnd();

            return step;
        }
    }
}
