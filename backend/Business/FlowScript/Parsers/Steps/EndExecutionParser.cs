using Business.FlowScript.Catalogs;
using Business.FlowScript.Models.Text;
using Core.Enums;
using Core.Models.Database;

namespace Business.FlowScript.Parsers.Steps
{
    internal sealed class EndExecutionParser : BaseStepParser
    {
        public EndExecutionParser(IReadOnlyList<ScriptToken> tokens) : base(tokens)
        {
        }

        public override FlowStep Parse()
        {
            // End Execution  passed | failed <[ reason ]>
            ExpectKeyword(FlowStepTypeEnum.END_EXECUTION);
            FlowStep step = new FlowStep() { FlowStepType = FlowStepTypeEnum.END_EXECUTION };

            if (ExpectOptionalKeyword(ScriptSymbolEnum.PASSED))
            {
                step.EndExecutionAsSuccess = true;
            }
            else
            {
                ExpectKeyword(ScriptSymbolEnum.FAILED);
                step.EndExecutionAsSuccess = false;

                ExpectKeyword(ScriptSymbolEnum.QUOTE_OPEN);
                step.Message = ExtractText();
                ExpectKeyword(ScriptSymbolEnum.QUOTE_CLOSE);
            }

            ExpectEnd();

            return step;
        }
    }
}
