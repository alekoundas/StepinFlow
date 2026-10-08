using Business.FlowScript.Catalogs;
using Core.Models.Database;

namespace Business.FlowScript.Text.Header
{
    internal sealed class CsvColumnWriter : BaseWriter
    {
        private readonly FlowCsvColumn _input;

        public CsvColumnWriter(FlowCsvColumn input)
        {
            _input = input;
        }

        protected override void Compose()
        {
            // <[ email ]>   [default <[ ops@example.com ]> | secret]
            // A default travels with the flow, so a clone and an import keep it; a csv row still
            // overrides it. A secret's value never reaches a file.
            WriteQuote(_input.Name);

            if (_input.IsSecret)
            {
                WriteGapUntil(NAME_GAP_UNTIL);
                WriteKeyword(ScriptSymbolEnum.SECRET);
                return;
            }

            if (_input.DefaultValue.Length > 0)
            {
                WriteGapUntil(NAME_GAP_UNTIL);
                WriteKeyword(ScriptSymbolEnum.DEFAULT);
                WriteQuote(_input.DefaultValue);
            }
        }
    }
}
