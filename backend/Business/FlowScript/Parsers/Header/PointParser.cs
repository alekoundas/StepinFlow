using Business.FlowScript.Catalogs;
using Business.FlowScript.Models.Text;
using Core.Enums;
using Core.Models.Database;

namespace Business.FlowScript.Parsers.Header
{
    internal sealed class PointParser : BaseParser<FlowPoint>
    {
        public PointParser(IReadOnlyList<ScriptToken> tokens) : base(tokens)
        {
        }

        public override FlowPoint Parse()
        {
            // <[ name ]>   inside <[ area ]> | on screen   ratio x y | offset x y   [at 120dpi]
            ExpectKeyword(ScriptSymbolEnum.QUOTE_OPEN);
            string name = ExtractText();
            ExpectKeyword(ScriptSymbolEnum.QUOTE_CLOSE);

            FlowPoint point = new FlowPoint() { Name = name };

            // Measured from an area, or from the screen's corner - right here and nowhere else. The
            // area is named, and linked by ScriptLineParser to the one declared above.
            if (ExpectOptionalKeyword(ScriptSymbolEnum.INSIDE))
            {
                ExpectKeyword(ScriptSymbolEnum.QUOTE_OPEN);
                point.FlowArea = new FlowArea() { Name = ExtractText() };
                ExpectKeyword(ScriptSymbolEnum.QUOTE_CLOSE);
            }
            else
            {
                ExpectKeyword(ScriptSymbolEnum.ON_SCREEN);
            }

            // A fraction of the area, or pixels from its corner.
            if (ExpectOptionalKeyword(ScriptSymbolEnum.RATIO))
            {
                point.OffsetMode = AreaSizingModeEnum.RATIO;
                point.RatioX = ExtractFloat();
                point.RatioY = ExtractFloat();
            }
            else
            {
                ExpectKeyword(ScriptSymbolEnum.OFFSET);
                point.OffsetMode = AreaSizingModeEnum.ABSOLUTE_PX;
                point.LocationX = ExtractInteger();
                point.LocationY = ExtractInteger();
            }

            // The DPI the offset was captured at. Without it the pixels stay as written.
            if (ExpectOptionalKeyword(ScriptSymbolEnum.AT))
                point.AuthoredDpi = ExtractDpi();

            ExpectEnd();

            return point;
        }
    }
}
