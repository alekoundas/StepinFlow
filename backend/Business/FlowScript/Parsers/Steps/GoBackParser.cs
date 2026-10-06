using Business.FlowScript.Catalogs;
using Business.FlowScript.Models.Binding;
using Business.FlowScript.Models.Text;
using Core.Enums;
using Core.Models.Database;

namespace Business.FlowScript.Parsers.Steps
{
    internal sealed class GoBackParser : BaseStepParser
    {
        public GoBackParser(IReadOnlyList<ScriptToken> tokens) : base(tokens)
        {
        }

        public override FlowStepSchemaBindng Parse()
        {
            // Go Back  to <[ step ]>
            ExpectKeyword(FlowStepTypeEnum.GO_BACK);
            ExpectKeyword(ScriptSymbolEnum.TO);

            ExpectKeyword(ScriptSymbolEnum.QUOTE_OPEN);
            string reference = ExtractText();
            ExpectKeyword(ScriptSymbolEnum.QUOTE_CLOSE);
            
            ExpectEnd();

            return new FlowStepSchemaBindng()
            {
                Step = new FlowStep() { FlowStepType = FlowStepTypeEnum.GO_BACK },
                ReferenceName = reference,
            };
        }
    }
}
