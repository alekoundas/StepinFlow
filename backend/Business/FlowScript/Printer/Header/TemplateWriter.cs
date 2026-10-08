using Business.FlowScript.Catalogs;
using Core.Models.Database;

namespace Business.FlowScript.Text.Header
{
    internal sealed class TemplateWriter : BaseWriter
    {
        private readonly FlowStepTemplate _template;

        public TemplateWriter(FlowStepTemplate template)
        {
            _template = template;
        }

        protected override void Compose()
        {
            // <[ a.png ]>   click 120 40   [captured 800x600] [at 120dpi]
            WriteQuote(_template.Name);
            WriteGapUntil(TEMPLATE_GAP_UNTIL);

            WriteKeyword(ScriptSymbolEnum.CLICK);
            WriteNumber(_template.ClickOffsetX);
            WriteNumber(_template.ClickOffsetY);

            // "captured 800x600 at 120dpi" reads as one phrase, so it is one clause apart from the click.
            bool isCaptured = _template.AuthoredFlowAreaWidth > 0 && _template.AuthoredFlowAreaHeight > 0;
            bool hasDpi = _template.AuthoredDpi > 0;

            if (isCaptured || hasDpi)
                WriteGap();

            if (isCaptured)
            {
                WriteKeyword(ScriptSymbolEnum.CAPTURED);
                WriteSize(_template.AuthoredFlowAreaWidth, _template.AuthoredFlowAreaHeight);
            }

            if (hasDpi)
            {
                WriteKeyword(ScriptSymbolEnum.AT);
                WriteDpi(_template.AuthoredDpi);
            }
        }
    }
}
