using Business.FlowScript.Catalogs;
using Business.FlowScript.Models.Text;
using Core.Models.Database;

namespace Business.FlowScript.Parsers.Header
{
    /// <summary>
    /// An automation or a QA test, and whether it is a sub-flow too. Read into a flow that carries
    /// only those two facts.
    /// </summary>
    internal sealed class FlowKindParser : BaseParser<Flow>
    {
        public FlowKindParser(IReadOnlyList<ScriptToken> tokens) : base(tokens)
        {
        }

        public override Flow Parse()
        {
            // Kind:    qa test   sub-flow
            ExpectKeyword(ScriptSymbolEnum.FLOWFIELD_KIND);

            FlowKindEnum kind = ExtractKeyword(FlowKindEnum.AUTOMATION, FlowKindEnum.QA_TEST);
            bool isSubFlow = ExpectOptionalKeyword(FlowKindEnum.SUB_FLOW);

            ExpectEnd();

            return new Flow() { IsQaTest = kind == FlowKindEnum.QA_TEST, IsSubFlow = isSubFlow };
        }
    }
}
