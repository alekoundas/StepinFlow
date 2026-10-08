using Business.FlowScript.Catalogs;
using Business.FlowScript.Models.Text;
using Core.Models.Database;

namespace Business.FlowScript.Parsers.Header
{
    internal sealed class CsvColumnParser : BaseParser<FlowCsvColumn>
    {
        public CsvColumnParser(IReadOnlyList<ScriptToken> tokens) : base(tokens)
        {
        }

        public override FlowCsvColumn Parse()
        {
            // <[ email ]>   [default <[ ops@example.com ]> | secret]
            ExpectKeyword(ScriptSymbolEnum.QUOTE_OPEN);
            string name = ExtractText();
            ExpectKeyword(ScriptSymbolEnum.QUOTE_CLOSE);

            FlowCsvColumn input = new FlowCsvColumn() { Name = name };

            // A secret has no value in the file, so the two never stand together.
            if (ExpectOptionalKeyword(ScriptSymbolEnum.DEFAULT))
            {
                ExpectKeyword(ScriptSymbolEnum.QUOTE_OPEN);
                input.DefaultValue = ExtractText();
                ExpectKeyword(ScriptSymbolEnum.QUOTE_CLOSE);
            }
            else
            {
                input.IsSecret = ExpectOptionalKeyword(ScriptSymbolEnum.SECRET);
            }

            ExpectEnd();

            return input;
        }
    }
}
