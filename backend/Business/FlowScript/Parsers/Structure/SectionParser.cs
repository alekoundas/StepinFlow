using Business.FlowScript.Catalogs;
using Business.FlowScript.Models.Text;

namespace Business.FlowScript.Parsers.Structure
{
    internal sealed class SectionParser : TokenParser<ScriptSymbolEnum>
    {
        public SectionParser(IReadOnlyList<ScriptToken> tokens) : base(tokens)
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
