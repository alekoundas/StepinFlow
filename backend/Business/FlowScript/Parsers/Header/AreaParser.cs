using Business.FlowScript.Catalogs;
using Business.FlowScript.Models.Binding;
using Business.FlowScript.Models.Text;
using Core.Enums;
using Core.Models.Database;

namespace Business.FlowScript.Parsers.Header
{
    internal sealed class AreaParser : TokenParser<FlowAreaSchemaBindng>
    {
        public AreaParser(IReadOnlyList<ScriptToken> tokens) : base(tokens)
        {
        }

        public override FlowAreaSchemaBindng Parse()
        {
            // <[ name ]>   window process <[ x ]> [title is <[ y ]>] | monitor primary | monitor <[ device ]> | on screen placement | inside <[ area ]> placement   [scales with dpi]   [at 120dpi]
            FlowAreaSchemaBindng result = new FlowAreaSchemaBindng();

            ExpectKeyword(ScriptSymbolEnum.QUOTE_OPEN);
            string name = ExtractText();
            ExpectKeyword(ScriptSymbolEnum.QUOTE_CLOSE);

            result.Area = new FlowArea() { Name = name };

            if (ExpectOptionalKeyword(ScriptSymbolEnum.WINDOW))
            {
                result.Area.Type = FlowAreaTypeEnum.APPLICATION;

                ExpectKeyword(ScriptSymbolEnum.PROCESS);
                ExpectKeyword(ScriptSymbolEnum.QUOTE_OPEN);
                result.Area.ProcessName = ExtractText();
                ExpectKeyword(ScriptSymbolEnum.QUOTE_CLOSE);

                if (ExpectOptionalKeyword(ScriptSymbolEnum.TITLE))
                {
                    result.Area.TitleMatchMode = ExtractKeyword<TitleMatchModeEnum>();
                    ExpectKeyword(ScriptSymbolEnum.QUOTE_OPEN);
                    result.Area.TitlePattern = ExtractText();
                    ExpectKeyword(ScriptSymbolEnum.QUOTE_CLOSE);
                }
            }
            // Empty is the primary monitor. A device is quoted, so one called primary is not mistaken for it.
            else if (ExpectOptionalKeyword(ScriptSymbolEnum.MONITOR))
            {
                result.Area.Type = FlowAreaTypeEnum.MONITOR;

                if (!ExpectOptionalKeyword(ScriptSymbolEnum.PRIMARY))
                {
                    ExpectKeyword(ScriptSymbolEnum.QUOTE_OPEN);
                    result.Area.MonitorDeviceName = ExtractText();
                    ExpectKeyword(ScriptSymbolEnum.QUOTE_CLOSE);
                }
            }
            // Screen coordinates: right on the machine it was made on and nowhere else.
            else if (ExpectOptionalKeyword(ScriptSymbolEnum.ON_SCREEN))
            {
                result.Area.Type = FlowAreaTypeEnum.CUSTOM;
                ExpectPlacement(result.Area);
            }
            else
            {
                ExpectKeyword(ScriptSymbolEnum.INSIDE);
                result.Area.Type = FlowAreaTypeEnum.CUSTOM;

                ExpectKeyword(ScriptSymbolEnum.QUOTE_OPEN);
                result.ParentName = ExtractText();
                ExpectKeyword(ScriptSymbolEnum.QUOTE_CLOSE);

                ExpectPlacement(result.Area);
            }

            // Both optional: no setting inherits the parent's, and no DPI leaves pixels as they are.
            if (ExpectOptionalKeyword(ScriptSymbolEnum.SCALES_WITH))
                result.Area.ScalesWith = ExtractKeyword<ScalesWithEnum>();

            if (ExpectOptionalKeyword(ScriptSymbolEnum.AT))
                result.Area.AuthoredDpi = ExtractDpi();

            ExpectEnd();

            return result;
        }


        // ================================================================
        // Private methods
        // ================================================================

        // ratio x y size w h | offset x y size w h
        private void ExpectPlacement(FlowArea area)
        {
            if (ExpectOptionalKeyword(ScriptSymbolEnum.RATIO))
            {
                area.SizingMode = AreaSizingModeEnum.RATIO;
                area.RatioX = ExtractFloat();
                area.RatioY = ExtractFloat();

                ExpectKeyword(ScriptSymbolEnum.SIZE);
                area.RatioWidth = ExtractFloat();
                area.RatioHeight = ExtractFloat();
                return;
            }

            ExpectKeyword(ScriptSymbolEnum.OFFSET);
            area.SizingMode = AreaSizingModeEnum.ABSOLUTE_PX;
            area.LocationX = ExtractInteger();
            area.LocationY = ExtractInteger();

            ExpectKeyword(ScriptSymbolEnum.SIZE);
            area.Width = ExtractInteger();
            area.Height = ExtractInteger();
        }
    }
}
