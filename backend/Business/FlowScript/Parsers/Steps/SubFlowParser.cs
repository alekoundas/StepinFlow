using Business.FlowScript.Catalogs;
using Business.FlowScript.Models.Binding;
using Business.FlowScript.Models.Text;
using Core.Enums;
using Core.Models.Database;

namespace Business.FlowScript.Parsers.Steps
{
    internal sealed class SubFlowParser : BaseStepParser
    {
        public SubFlowParser(IReadOnlyList<ScriptToken> tokens) : base(tokens)
        {
        }

        public override FlowStepSchemaBindng Parse()
        {
            // Sub Flow  <[ path ]>
            ExpectKeyword(FlowStepTypeEnum.SUB_FLOW);

            ExpectKeyword(ScriptSymbolEnum.QUOTE_OPEN);
            string path = ExtractText();
            ExpectKeyword(ScriptSymbolEnum.QUOTE_CLOSE);
            
            ExpectEnd();

            return new FlowStepSchemaBindng()
            {
                Step = new FlowStep() { FlowStepType = FlowStepTypeEnum.SUB_FLOW },
                SubFlowPath = path,
            };
        }
    }
}
