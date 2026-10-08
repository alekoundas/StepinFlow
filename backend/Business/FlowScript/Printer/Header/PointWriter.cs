using Business.FlowScript.Catalogs;
using Core.Enums;
using Core.Models.Database;

namespace Business.FlowScript.Text.Header
{
    internal sealed class PointWriter : BaseWriter
    {
        private readonly FlowPoint _point;

        public PointWriter(FlowPoint point)
        {
            _point = point;
        }

        protected override void Compose()
        {
            // <[ name ]>   inside <[ area ]> | on screen   ratio x y | offset x y   [at 120dpi]
            WriteQuote(_point.Name);
            WriteGapUntil(NAME_GAP_UNTIL);

            if (_point.FlowArea != null)
            {
                WriteKeyword(ScriptSymbolEnum.INSIDE);
                WriteQuote(_point.FlowArea.Name);
            }
            else
            {
                WriteKeyword(ScriptSymbolEnum.ON_SCREEN);
            }

            WriteGap();

            if (_point.OffsetMode == AreaSizingModeEnum.RATIO)
            {
                WriteKeyword(ScriptSymbolEnum.RATIO);
                WriteRatio(_point.RatioX);
                WriteRatio(_point.RatioY);
            }
            else
            {
                WriteKeyword(ScriptSymbolEnum.OFFSET);
                WriteNumber(_point.LocationX);
                WriteNumber(_point.LocationY);
            }

            if (_point.AuthoredDpi > 0)
            {
                WriteGap();
                WriteKeyword(ScriptSymbolEnum.AT);
                WriteDpi(_point.AuthoredDpi);
            }
        }
    }
}
