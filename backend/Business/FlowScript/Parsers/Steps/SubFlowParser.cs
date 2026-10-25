using Business.FlowScript.Catalogs;
using Business.FlowScript.Models.Text;
using Core.Enums;
using Core.Models.Database;

namespace Business.FlowScript.Parsers.Steps
{
    internal sealed class SubFlowParser : BaseStepParser
    {
        public SubFlowParser(IReadOnlyList<ScriptToken> tokens, ScriptScope scope) : base(tokens, scope)
        {
        }

        public override FlowStep Parse()
        {
            // Sub Flow  <[ Checkout ]>   id <[ 3b0c1d52-6f7e-4a8b-9c0d-1e2f3a4b5c6d ]>
            ExpectKeyword(FlowStepTypeEnum.SUB_FLOW);

            ExpectKeyword(ScriptSymbolEnum.QUOTE_OPEN);
            Flow subFlow = new Flow() { Name = ExtractText(), PublicId = Guid.Empty };
            ExpectKeyword(ScriptSymbolEnum.QUOTE_CLOSE);

            if (ExpectOptionalKeyword(ScriptSymbolEnum.ID))
            {
                ExpectKeyword(ScriptSymbolEnum.QUOTE_OPEN);
                subFlow.PublicId = ExtractGuid();
                ExpectKeyword(ScriptSymbolEnum.QUOTE_CLOSE);
            }

            ExpectEnd();

            return new FlowStep() { FlowStepType = FlowStepTypeEnum.SUB_FLOW, SubFlow = subFlow };
        }
    }
}
