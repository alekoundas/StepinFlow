using Business.FlowScript.Catalogs;
using Business.FlowScript.Models.Text;

namespace Business.FlowScript.Parsers
{
    internal sealed class SectionHeaderParser : TokenParser<ScriptSymbolEnum>
    {
        public SectionHeaderParser(IReadOnlyList<ScriptToken> tokens) : base(tokens)
        {
        }

        public override ScriptSymbolEnum Parse()
        {
            // Areas: | Points: | Inputs: | Templates: | Steps:
            ScriptSymbolEnum section = ExtractKeyword(ScriptSymbolEnum.AREAS, ScriptSymbolEnum.POINTS, ScriptSymbolEnum.CSV_COLUMNS, ScriptSymbolEnum.TEMPLATES, ScriptSymbolEnum.STEPS);
            ExpectEnd();

            return section;
        }
    }
}
