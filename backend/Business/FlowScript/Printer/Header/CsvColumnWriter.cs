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
            // <[ password ]>   [secret]
            // The value is never written, secret or not: data belongs in the csv beside the file,
            // and a default in the script would be the one nobody remembers to change.
            WriteQuote(_input.Name);

            if (_input.IsSecret)
            {
                WriteGapUntil(NAME_GAP_UNTIL);
                WriteKeyword(ScriptSymbolEnum.SECRET);
            }
        }
    }
}
