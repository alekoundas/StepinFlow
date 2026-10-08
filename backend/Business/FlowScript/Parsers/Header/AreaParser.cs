using Business.FlowScript.Catalogs;
using Business.FlowScript.Models.Text;
using Core.Enums;
using Core.Models.Database;

namespace Business.FlowScript.Parsers.Header
{
    internal sealed class AreaParser : BaseParser<FlowArea>
    {
        private readonly ScriptScope _scope;

        public AreaParser(IReadOnlyList<ScriptToken> tokens, ScriptScope scope) : base(tokens)
        {
            _scope = scope;
        }

        public override FlowArea Parse()
        {
            // <[ name ]>   window process <[ x ]> [title is <[ y ]>] | monitor primary | monitor <[ device ]> | on screen placement | inside <[ area ]> placement   [scales with dpi]   [at 120dpi]
            ExpectKeyword(ScriptSymbolEnum.QUOTE_OPEN);
            string name = ExtractText();
            ExpectKeyword(ScriptSymbolEnum.QUOTE_CLOSE);

            FlowArea area = new FlowArea() { Name = name };

            if (ExpectOptionalKeyword(ScriptSymbolEnum.WINDOW))
            {
                area.Type = FlowAreaTypeEnum.APPLICATION;

                ExpectKeyword(ScriptSymbolEnum.PROCESS);
                ExpectKeyword(ScriptSymbolEnum.QUOTE_OPEN);
                area.ProcessName = ExtractText();
                ExpectKeyword(ScriptSymbolEnum.QUOTE_CLOSE);

                if (ExpectOptionalKeyword(ScriptSymbolEnum.TITLE))
                {
                    area.TitleMatchMode = ExtractKeyword<TitleMatchModeEnum>();
                    ExpectKeyword(ScriptSymbolEnum.QUOTE_OPEN);
                    area.TitlePattern = ExtractText();
                    ExpectKeyword(ScriptSymbolEnum.QUOTE_CLOSE);
                }
            }
            // Empty is the primary monitor. A device is quoted, so one called primary is not mistaken for it.
            else if (ExpectOptionalKeyword(ScriptSymbolEnum.MONITOR))
            {
                area.Type = FlowAreaTypeEnum.MONITOR;

                if (!ExpectOptionalKeyword(ScriptSymbolEnum.PRIMARY))
                {
                    ExpectKeyword(ScriptSymbolEnum.QUOTE_OPEN);
                    area.MonitorDeviceName = ExtractText();
                    ExpectKeyword(ScriptSymbolEnum.QUOTE_CLOSE);
                }
            }
            // Screen coordinates: right on the machine it was made on and nowhere else.
            else if (ExpectOptionalKeyword(ScriptSymbolEnum.ON_SCREEN))
            {
                area.Type = FlowAreaTypeEnum.CUSTOM;
                ExpectPlacement(area);
            }
            else
            {
                ExpectKeyword(ScriptSymbolEnum.INSIDE);
                area.Type = FlowAreaTypeEnum.CUSTOM;

                ExpectKeyword(ScriptSymbolEnum.QUOTE_OPEN);
                ScriptToken at = CurrentToken;
                area.ParentFlowArea = _scope.ParentArea(ExtractText(), at);
                ExpectKeyword(ScriptSymbolEnum.QUOTE_CLOSE);

                ExpectPlacement(area);
            }

            // Both optional: no setting inherits the parent's, and no DPI leaves pixels as they are.
            if (ExpectOptionalKeyword(ScriptSymbolEnum.SCALES_WITH))
                area.ScalesWith = ExtractKeyword<ScalesWithEnum>();

            if (ExpectOptionalKeyword(ScriptSymbolEnum.AT))
                area.AuthoredDpi = ExtractDpi();

            ExpectEnd();

            return area;
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
