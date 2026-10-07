using Business.FlowScript.Catalogs;
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

        public override FlowStep Parse()
        {
            // Go Back  to <[ step ]>
            ExpectKeyword(FlowStepTypeEnum.GO_BACK);
            ExpectKeyword(ScriptSymbolEnum.TO);

            ExpectKeyword(ScriptSymbolEnum.QUOTE_OPEN);
            FlowStep reference = new FlowStep() { Name = ExtractText() };
            ExpectKeyword(ScriptSymbolEnum.QUOTE_CLOSE);

            ExpectEnd();

            return new FlowStep() { FlowStepType = FlowStepTypeEnum.GO_BACK, FlowStepReference = reference };
        }
    }
}
