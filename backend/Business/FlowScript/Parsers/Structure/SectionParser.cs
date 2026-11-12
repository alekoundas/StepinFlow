using Business.FlowScript.Catalogs;
using Business.FlowScript.Models.Text;

namespace Business.FlowScript.Parsers.Structure
{
    internal sealed class SectionParser : BaseParser<ScriptSymbolEnum>
    {
        public SectionParser(IReadOnlyList<ScriptToken> tokens) : base(tokens)
        {
        }

        public override ScriptSymbolEnum Parse()
        {
            // Description: | Areas: | Points: | Inputs: | Discord Bots: | Templates: | Steps:
            ScriptSymbolEnum section = ExtractKeyword(ScriptSymbolEnum.FLOWFIELD_DESCRIPTION, ScriptSymbolEnum.AREAS, ScriptSymbolEnum.POINTS, ScriptSymbolEnum.INPUTS, ScriptSymbolEnum.DISCORD_BOTS, ScriptSymbolEnum.TEMPLATES, ScriptSymbolEnum.STEPS);
            ExpectEnd();

            return section;
        }
    }
}
