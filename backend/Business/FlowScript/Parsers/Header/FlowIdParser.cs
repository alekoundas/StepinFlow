using Business.FlowScript.Catalogs;
using Business.FlowScript.Models.Text;

namespace Business.FlowScript.Parsers.Header
{
    internal sealed class FlowIdParser : TokenParser<Guid>
    {
        public FlowIdParser(IReadOnlyList<ScriptToken> tokens) : base(tokens)
        {
        }

        public override Guid Parse()
        {
            // Id:      8f14e45f-ea2b-4c3f-9f1a-77f0d2a3b111
            ExpectKeyword(ScriptSymbolEnum.FLOWFIELD_ID);
            Guid id = ExtractGuid();
            ExpectEnd();

            return id;
        }
    }
}
