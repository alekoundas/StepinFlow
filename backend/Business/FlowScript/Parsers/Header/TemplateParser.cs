using System.Drawing;

using Business.FlowScript.Catalogs;
using Business.FlowScript.Models.Text;
using Core.Models.Database;

namespace Business.FlowScript.Parsers.Header
{
    /// <summary>
    /// The facts about one picture, named by its file. Every template has a click, so the line
    /// always gives one.
    /// </summary>
    internal sealed class TemplateParser : BaseParser<FlowStepTemplate>
    {
        public TemplateParser(IReadOnlyList<ScriptToken> tokens) : base(tokens)
        {
        }

        public override FlowStepTemplate Parse()
        {
            // <[ a.png ]>   click 120 40   [captured 800x600] [at 120dpi]
            ExpectKeyword(ScriptSymbolEnum.QUOTE_OPEN);
            FlowStepTemplate template = new FlowStepTemplate() { Name = ExtractText() };
            ExpectKeyword(ScriptSymbolEnum.QUOTE_CLOSE);

            ExpectKeyword(ScriptSymbolEnum.CLICK);
            template.ClickOffsetX = ExtractInteger();
            template.ClickOffsetY = ExtractInteger();

            if (ExpectOptionalKeyword(ScriptSymbolEnum.CAPTURED))
            {
                Size size = ExtractSize();
                template.AuthoredFlowAreaWidth = size.Width;
                template.AuthoredFlowAreaHeight = size.Height;
            }

            if (ExpectOptionalKeyword(ScriptSymbolEnum.AT))
                template.AuthoredDpi = ExtractDpi();

            ExpectEnd();

            return template;
        }
    }
}
