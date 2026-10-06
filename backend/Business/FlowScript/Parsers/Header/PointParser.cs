using Business.FlowScript.Catalogs;
using Business.FlowScript.Models.Binding;
using Business.FlowScript.Models.Text;
using Core.Enums;
using Core.Models.Database;

namespace Business.FlowScript.Parsers.Header
{
    internal sealed class PointParser : TokenParser<FlowPointSchemaBindng>
    {
        public PointParser(IReadOnlyList<ScriptToken> tokens) : base(tokens)
        {
        }

        public override FlowPointSchemaBindng Parse()
        {
            // <[ name ]>   inside <[ area ]> | on screen   ratio x y | offset x y   [at 120dpi]
            FlowPointSchemaBindng result = new FlowPointSchemaBindng();

            ExpectKeyword(ScriptSymbolEnum.QUOTE_OPEN);
            string name = ExtractText();
            ExpectKeyword(ScriptSymbolEnum.QUOTE_CLOSE);

            result.Point = new FlowPoint() { Name = name };

            // Measured from an area, or from the screen's corner - right here and nowhere else.
            if (ExpectOptionalKeyword(ScriptSymbolEnum.INSIDE))
            {
                ExpectKeyword(ScriptSymbolEnum.QUOTE_OPEN);
                result.AreaName = ExtractText();
                ExpectKeyword(ScriptSymbolEnum.QUOTE_CLOSE);
            }
            else
            {
                ExpectKeyword(ScriptSymbolEnum.ON_SCREEN);
            }

            // A fraction of the area, or pixels from its corner.
            if (ExpectOptionalKeyword(ScriptSymbolEnum.RATIO))
            {
                result.Point.OffsetMode = AreaSizingModeEnum.RATIO;
                result.Point.RatioX = ExtractFloat();
                result.Point.RatioY = ExtractFloat();
            }
            else
            {
                ExpectKeyword(ScriptSymbolEnum.OFFSET);
                result.Point.OffsetMode = AreaSizingModeEnum.ABSOLUTE_PX;
                result.Point.LocationX = ExtractInteger();
                result.Point.LocationY = ExtractInteger();
            }

            // The DPI the offset was captured at. Without it the pixels stay as written.
            if (ExpectOptionalKeyword(ScriptSymbolEnum.AT))
                result.Point.AuthoredDpi = ExtractDpi();

            ExpectEnd();

            return result;
        }
    }
}
