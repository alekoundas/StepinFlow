using Business.FlowScript.Catalogs;
using Core.Enums;
using Core.Models.Database;

namespace Business.FlowScript.Text.Header
{
    internal sealed class AreaWriter : BaseWriter
    {
        private readonly FlowArea _area;

        public AreaWriter(FlowArea area)
        {
            _area = area;
        }

        protected override void Compose()
        {
            // <[ name ]>   window process <[ x ]> [title is <[ y ]>] | monitor primary | monitor <[ device ]> | on screen placement | inside <[ area ]> placement   [scales with dpi]   [at 120dpi]
            WriteQuote(_area.Name);
            WriteGapUntil(NAME_GAP_UNTIL);

            WriteLocation();

            // Both left out when unset: no setting inherits the parent's, and no DPI leaves pixels as they are.
            if (_area.ScalesWith != null)
            {
                WriteGap();
                WriteKeyword(ScriptSymbolEnum.SCALES_WITH);
                WriteKeyword(_area.ScalesWith.Value);
            }

            if (_area.AuthoredDpi > 0)
            {
                WriteGap();
                WriteKeyword(ScriptSymbolEnum.AT);
                WriteDpi(_area.AuthoredDpi);
            }
        }


        // ================================================================
        // Private methods
        // ================================================================

        // A window, a monitor, the screen, or inside another area.
        private void WriteLocation()
        {
            if (_area.ParentFlowArea != null)
            {
                WriteKeyword(ScriptSymbolEnum.INSIDE);
                WriteQuote(_area.ParentFlowArea.Name);
                WriteGap();
                WritePlacement();
                return;
            }

            switch (_area.Type)
            {
                // Empty is the primary monitor, and "primary" is a bare word so a device cannot be mistaken for it.
                case FlowAreaTypeEnum.MONITOR:
                    WriteKeyword(ScriptSymbolEnum.MONITOR);

                    if (_area.MonitorDeviceName.Length == 0)
                        WriteKeyword(ScriptSymbolEnum.PRIMARY);
                    else
                        WriteQuote(_area.MonitorDeviceName);
                    break;

                case FlowAreaTypeEnum.CUSTOM:
                    WriteKeyword(ScriptSymbolEnum.ON_SCREEN);
                    WriteGap();
                    WritePlacement();
                    break;

                default:
                    WriteKeyword(ScriptSymbolEnum.WINDOW);
                    WriteKeyword(ScriptSymbolEnum.PROCESS);
                    WriteQuote(_area.ProcessName);

                    if (!string.IsNullOrWhiteSpace(_area.TitlePattern))
                    {
                        WriteKeyword(ScriptSymbolEnum.TITLE);
                        WriteKeyword(_area.TitleMatchMode);
                        WriteQuote(_area.TitlePattern);
                    }
                    break;
            }
        }

        // ratio x y  size w h | offset x y  size w h
        private void WritePlacement()
        {
            if (_area.SizingMode == AreaSizingModeEnum.RATIO)
            {
                WriteKeyword(ScriptSymbolEnum.RATIO);
                WriteRatio(_area.RatioX);
                WriteRatio(_area.RatioY);
                WriteGap(2);
                WriteKeyword(ScriptSymbolEnum.SIZE);
                WriteRatio(_area.RatioWidth);
                WriteRatio(_area.RatioHeight);
                return;
            }

            WriteKeyword(ScriptSymbolEnum.OFFSET);
            WriteNumber(_area.LocationX);
            WriteNumber(_area.LocationY);
            WriteGap(2);
            WriteKeyword(ScriptSymbolEnum.SIZE);
            WriteNumber(_area.Width);
            WriteNumber(_area.Height);
        }
    }
}
