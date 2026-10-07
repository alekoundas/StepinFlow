using System.Drawing;

using Business.FlowScript.Catalogs;
using Business.FlowScript.Models.Text;
using Core.Models.Database;

namespace Business.FlowScript.Parsers.Header
{
    /// <summary>
    /// The facts about one picture, named by its file. Whether the line gave a click is returned
    /// beside it, because a template's click is a number either way and the importer centres the
    /// ones left out.
    /// </summary>
    internal sealed class TemplateParser : BaseParser<(FlowStepTemplate Facts, bool HasClick)>
    {
        public TemplateParser(IReadOnlyList<ScriptToken> tokens) : base(tokens)
        {
        }

        public override (FlowStepTemplate Facts, bool HasClick) Parse()
        {
            // <[ a.png ]>   [click 120 40]   [captured 800x600] [at 120dpi]
            ExpectKeyword(ScriptSymbolEnum.QUOTE_OPEN);
            FlowStepTemplate facts = new FlowStepTemplate() { Name = ExtractText() };
            ExpectKeyword(ScriptSymbolEnum.QUOTE_CLOSE);

            bool hasClick = ExpectOptionalKeyword(ScriptSymbolEnum.CLICK);
            if (hasClick)
            {
                facts.ClickOffsetX = ExtractInteger();
                facts.ClickOffsetY = ExtractInteger();
            }

            if (ExpectOptionalKeyword(ScriptSymbolEnum.CAPTURED))
            {
                Size size = ExtractSize();
                facts.AuthoredFlowAreaWidth = size.Width;
                facts.AuthoredFlowAreaHeight = size.Height;
            }

            if (ExpectOptionalKeyword(ScriptSymbolEnum.AT))
                facts.AuthoredDpi = ExtractDpi();

            ExpectEnd();

            return (facts, hasClick);
        }
    }
}
