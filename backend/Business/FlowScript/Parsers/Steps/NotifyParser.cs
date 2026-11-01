using Business.FlowScript.Catalogs;
using Business.FlowScript.Models.Text;
using Core.Enums;
using Core.Models.Database;

namespace Business.FlowScript.Parsers.Steps
{
    internal sealed class NotifyParser : BaseStepParser
    {
        public NotifyParser(IReadOnlyList<ScriptToken> tokens, ScriptScope scope) : base(tokens, scope)
        {
        }

        public override FlowStep Parse()
        {
            // Notify  <[ message ]>   [with result of <[ a step ]>]
            ExpectKeyword(FlowStepTypeEnum.NOTIFY);

            ExpectKeyword(ScriptSymbolEnum.QUOTE_OPEN);
            string message = ExtractText();
            ExpectKeyword(ScriptSymbolEnum.QUOTE_CLOSE);

            FlowStep step = new FlowStep() { FlowStepType = FlowStepTypeEnum.NOTIFY, Message = message };

            if (ExpectOptionalKeyword(ScriptSymbolEnum.WITH_RESULT_OF))
                step.FlowStepReference = ExtractStepReference();

            ExpectEnd();

            return step;
        }
    }
}
