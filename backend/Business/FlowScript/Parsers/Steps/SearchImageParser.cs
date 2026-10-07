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
            // Find Image  <[ name ]>   template <[ a.png ]> accuracy 0.9 required ...   match shape   in <[ area ]>   timeout 10000ms
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
            // A template with no accuracy takes its mode's default, and the mode comes after the templates.
            List<FlowStepTemplate> templates = new List<FlowStepTemplate>();
            List<FlowStepTemplate> withoutAccuracy = new List<FlowStepTemplate>();
            while (ExpectOptionalKeyword(ScriptSymbolEnum.TEMPLATE))
            {
                ExpectKeyword(ScriptSymbolEnum.QUOTE_OPEN);
                ScriptToken at = CurrentToken;
                string fileName = ExtractText();
                ExpectKeyword(ScriptSymbolEnum.QUOTE_CLOSE);

                // The step's own row: accuracy and required are the step's, so the picture's facts
                // are copied onto it rather than the Templates line's row being shared.
                FlowStepTemplate template = new FlowStepTemplate() { Name = fileName, OrderNumber = templates.Count };

                FlowStepTemplate? described = Scope.Template(fileName, at);
                if (described != null)
                {
                    template.ClickOffsetX = described.ClickOffsetX;
                    template.ClickOffsetY = described.ClickOffsetY;
                    template.AuthoredFlowAreaWidth = described.AuthoredFlowAreaWidth;
                    template.AuthoredFlowAreaHeight = described.AuthoredFlowAreaHeight;
                    template.AuthoredDpi = described.AuthoredDpi;
                }

                if (ExpectOptionalKeyword(ScriptSymbolEnum.ACCURACY))
                    template.Accuracy = ExtractFloat();
                else
                    withoutAccuracy.Add(template);

                template.IsRequired = ExpectOptionalKeyword(ScriptSymbolEnum.REQUIRED);
                templates.Add(template);
            }

            step.FlowStepTemplates = templates;

            if (ExpectOptionalKeyword(ScriptSymbolEnum.MATCH))
                step.TemplateMatchMode = ExtractKeyword<TemplateMatchModeEnum>();

            foreach (FlowStepTemplate template in withoutAccuracy)
                template.Accuracy = DefaultAccuracy(step.TemplateMatchMode);

            step.FlowArea = ExtractOptionalArea();

            // Optional timeout.
            if (ExpectOptionalKeyword(ScriptSymbolEnum.TIMEOUT))
                step.TimeoutMilliseconds = ExtractMilliseconds();
            else if (ExpectOptionalKeyword(ScriptSymbolEnum.NO_TIMEOUT))
                step.TimeoutMilliseconds = 0;

            ExpectEnd();

            return step;
        }

        /// <summary>
        /// What a template starts on when the file gives it no accuracy. Per mode, because the two
        /// are different instruments and one number cannot mean the same in both.
        /// </summary>
        public static float DefaultAccuracy(TemplateMatchModeEnum mode)
        {
            if (mode == TemplateMatchModeEnum.SHAPE_AND_BRIGHTNESS)
                return 0.95f;

            return 0.8f;
        }
    }
}
