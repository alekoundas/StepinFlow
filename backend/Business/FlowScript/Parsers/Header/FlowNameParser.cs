using Business.FlowScript.Catalogs;
using Business.FlowScript.Models.Text;

namespace Business.FlowScript.Parsers.Header
{
    internal sealed class FlowNameParser : TokenParser<string>
    {
        public FlowNameParser(IReadOnlyList<ScriptToken> tokens) : base(tokens)
        {
        }

        public override string Parse()
        {
            // Flow:    Login and add to cart
            ExpectKeyword(ScriptSymbolEnum.FLOWFIELD_NAME);
            string name = ExtractText();
            ExpectEnd();

            return name;
        }
    }
}
