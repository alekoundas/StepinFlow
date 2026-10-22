using Business.FlowScript.Catalogs;
using Business.FlowScript.Models.Text;
using Core.Enums;
using Core.Models.Database;

namespace Business.FlowScript.Parsers.Steps
{
    internal sealed class BreakParser : BaseStepParser
    {
        public BreakParser(IReadOnlyList<ScriptToken> tokens, ScriptScope scope) : base(tokens, scope)
        {
        }

        public override FlowStep Parse()
        {
            // Break  out of <[ step ]>
            ExpectKeyword(FlowStepTypeEnum.BREAK);
            ExpectKeyword(ScriptSymbolEnum.OUT_OF);

            FlowStep? reference = ExtractStepReference();

            ExpectEnd();

            return new FlowStep() { FlowStepType = FlowStepTypeEnum.BREAK, FlowStepReference = reference };
        }
    }
}
