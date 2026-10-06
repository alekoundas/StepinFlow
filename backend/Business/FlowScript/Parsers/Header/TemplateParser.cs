using System.Drawing;

using Business.FlowScript.Catalogs;
using Business.FlowScript.Models.Binding;
using Business.FlowScript.Models.Text;

namespace Business.FlowScript.Parsers.Header
{
    internal sealed class TemplateParser : BaseParser<FlowStepTemplateSchemaBindng>
    {
        public TemplateParser(IReadOnlyList<ScriptToken> tokens) : base(tokens)
        {
        }

        public override FlowStepTemplateSchemaBindng Parse()
        {
            // <[ a.png ]>   [click 120 40]   [captured 800x600] [at 120dpi]
            FlowStepTemplateSchemaBindng result = new FlowStepTemplateSchemaBindng();

            ExpectKeyword(ScriptSymbolEnum.QUOTE_OPEN);
            result.FileName = ExtractText();
            ExpectKeyword(ScriptSymbolEnum.QUOTE_CLOSE);

            if (ExpectOptionalKeyword(ScriptSymbolEnum.CLICK))
                result.ClickOffset = new Point(ExtractInteger(), ExtractInteger());

            if (ExpectOptionalKeyword(ScriptSymbolEnum.CAPTURED))
            {
                Size size = ExtractSize();
                result.AuthoredFlowAreaWidth = size.Width;
                result.AuthoredFlowAreaHeight = size.Height;
            }

            if (ExpectOptionalKeyword(ScriptSymbolEnum.AT))
                result.AuthoredDpi = ExtractDpi();

            ExpectEnd();

            return result;
        }
    }
}
