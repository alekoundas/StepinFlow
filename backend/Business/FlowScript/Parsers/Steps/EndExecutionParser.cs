using Business.FlowScript.Catalogs;
using Business.FlowScript.Models.Binding;
using Business.FlowScript.Models.Text;
using Core.Enums;
using Core.Models.Database;

namespace Business.FlowScript.Parsers.Steps
{
    internal sealed class EndExecutionParser : StepParser
    {
        public EndExecutionParser(IReadOnlyList<ScriptToken> tokens) : base(tokens)
        {
        }

        public override FlowStepSchemaBindng Parse()
        {
            // End Execution  passed | failed <[ reason ]>
            FlowStepSchemaBindng result = new FlowStepSchemaBindng();

            ExpectKeyword(FlowStepTypeEnum.END_EXECUTION);
            result.Step = new FlowStep() { FlowStepType = FlowStepTypeEnum.END_EXECUTION };


            if (ExpectOptionalKeyword(ScriptSymbolEnum.PASSED))
            {
                result.Step.EndExecutionAsSuccess = true;
            }
            else
            {
                ExpectKeyword(ScriptSymbolEnum.FAILED);
                result.Step.EndExecutionAsSuccess = false;

                ExpectKeyword(ScriptSymbolEnum.QUOTE_OPEN);
                result.Step.Message = ExtractText();
                ExpectKeyword(ScriptSymbolEnum.QUOTE_CLOSE);
            }

            ExpectEnd();

            return result;
        }
    }
}
