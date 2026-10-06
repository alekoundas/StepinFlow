using Business.FlowScript.Catalogs;
using Business.FlowScript.Models.Binding;
using Business.FlowScript.Models.Text;
using Core.Enums;
using Core.Models.Database;

namespace Business.FlowScript.Parsers.Steps
{
    internal sealed class SearchImageParser : BaseStepParser
    {
        public SearchImageParser(IReadOnlyList<ScriptToken> tokens) : base(tokens)
        {
        }

        public override FlowStepSchemaBindng Parse()
        {
            // Find Image  <[ name ]>   template <[ a.png ]> accuracy 0.9 required ...   match shape   in <[ area ]>   timeout 10000ms
            FlowStepSchemaBindng result = new FlowStepSchemaBindng();
            ScriptKeyword keyword = ExtractStepKeyword(FlowStepTypeEnum.SEARCH_IMAGE);

            ExpectKeyword(ScriptSymbolEnum.QUOTE_OPEN);
            string name = ExtractText();
            ExpectKeyword(ScriptSymbolEnum.QUOTE_CLOSE);


            result.Step = new FlowStep()
            {
                FlowStepType = FlowStepTypeEnum.SEARCH_IMAGE,
                SearchMode = keyword.As<SearchModeEnum>()!.Value,
                Name = name
            };

            // A template with no accuracy takes its mode's default, and the mode comes after the templates.
            List<ScriptTemplateImage> withoutAccuracy = new List<ScriptTemplateImage>();
            while (ExpectOptionalKeyword(ScriptSymbolEnum.TEMPLATE))
            {
                ExpectKeyword(ScriptSymbolEnum.QUOTE_OPEN);
                ScriptTemplateImage template = new ScriptTemplateImage() { FileName = ExtractText() };
                ExpectKeyword(ScriptSymbolEnum.QUOTE_CLOSE);

                if (ExpectOptionalKeyword(ScriptSymbolEnum.ACCURACY))
                    template.Accuracy = ExtractFloat();
                else
                    withoutAccuracy.Add(template);

                template.IsRequired = ExpectOptionalKeyword(ScriptSymbolEnum.REQUIRED);
                result.Templates.Add(template);
            }

            if (ExpectOptionalKeyword(ScriptSymbolEnum.MATCH))
                result.Step.TemplateMatchMode = ExtractKeyword<TemplateMatchModeEnum>();

            foreach (ScriptTemplateImage template in withoutAccuracy)
                template.Accuracy = ScriptTemplateImage.DefaultAccuracy(result.Step.TemplateMatchMode);

            result.AreaName = ExtractOptionalArea();

            // Optional timeout.
            if (ExpectOptionalKeyword(ScriptSymbolEnum.TIMEOUT))
                result.Step.TimeoutMilliseconds = ExtractMilliseconds();
            else if (ExpectOptionalKeyword(ScriptSymbolEnum.NO_TIMEOUT))
                result.Step.TimeoutMilliseconds = 0;

            ExpectEnd();

            return result;
        }
    }
}
