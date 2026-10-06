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
            // <[ password ]>   [secret]
            ExpectKeyword(ScriptSymbolEnum.QUOTE_OPEN);
            string name = ExtractText();
            ExpectKeyword(ScriptSymbolEnum.QUOTE_CLOSE);

            bool isSecret = ExpectOptionalKeyword(ScriptSymbolEnum.SECRET);
            ExpectEnd();

            return new FlowCsvColumn() { Name = name, IsSecret = isSecret };
        }
    }
}
