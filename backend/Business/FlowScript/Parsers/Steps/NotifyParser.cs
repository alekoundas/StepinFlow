using Business.FlowScript.Catalogs;
using Business.FlowScript.Models.Text;
using Core.Enums;
using Core.Models.Database;

namespace Business.FlowScript.Parsers.Steps
{
    internal sealed class NotifyParser : BaseStepParser
    {
        public NotifyParser(IReadOnlyList<ScriptToken> tokens) : base(tokens)
        {
        }

        public override FlowStep Parse()
        {
            // Notify  <[ message ]>
            ExpectKeyword(FlowStepTypeEnum.NOTIFY);

            ExpectKeyword(ScriptSymbolEnum.QUOTE_OPEN);
            string message = ExtractText();
            ExpectKeyword(ScriptSymbolEnum.QUOTE_CLOSE);

            ExpectEnd();

            return new FlowStep() { FlowStepType = FlowStepTypeEnum.NOTIFY, Message = message };
        }
    }
}
