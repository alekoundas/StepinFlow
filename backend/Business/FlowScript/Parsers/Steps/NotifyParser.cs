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
            // Notify  <[ message ]>  <[ its next line ]>   [with result of <[ a step ]>]
            ExpectKeyword(FlowStepTypeEnum.NOTIFY);

            ExpectKeyword(ScriptSymbolEnum.QUOTE_OPEN);
            List<string> lines = [ExtractText()];
            ExpectKeyword(ScriptSymbolEnum.QUOTE_CLOSE);

            while (ExpectOptionalKeyword(ScriptSymbolEnum.QUOTE_OPEN))
            {
                lines.Add(ExtractText());
                ExpectKeyword(ScriptSymbolEnum.QUOTE_CLOSE);
            }

            FlowStep step = new FlowStep() { FlowStepType = FlowStepTypeEnum.NOTIFY, Message = string.Join("\n", lines) };

            if (ExpectOptionalKeyword(ScriptSymbolEnum.WITH_RESULT_OF))
                step.FlowStepReference = ExtractStepReference();

            ExpectEnd();

            return step;
        }
    }
}
