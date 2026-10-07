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
            // Sub Flow  <[ path ]>
            ExpectKeyword(FlowStepTypeEnum.SUB_FLOW);

            ExpectKeyword(ScriptSymbolEnum.QUOTE_OPEN);
            Flow subFlow = new Flow() { Name = ExtractText() };
            ExpectKeyword(ScriptSymbolEnum.QUOTE_CLOSE);

            ExpectEnd();

            return new FlowStep() { FlowStepType = FlowStepTypeEnum.SUB_FLOW, SubFlow = subFlow };
        }
    }
}
