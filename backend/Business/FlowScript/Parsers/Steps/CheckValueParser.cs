using Business.FlowScript.Catalogs;
using Business.FlowScript.Models.Text;
using Core.Enums;
using Core.Models.Database;

namespace Business.FlowScript.Parsers.Steps
{
    internal sealed class CheckValueParser : BaseStepParser
    {
        public CheckValueParser(IReadOnlyList<ScriptToken> tokens) : base(tokens)
        {
        }

        public override FlowStep Parse()
        {
            // Check Value  <[ name ]>   <[ {{Step name}} ]> > <[ 100 ]>
            ExpectKeyword(FlowStepTypeEnum.CHECK_VALUE);

            ExpectKeyword(ScriptSymbolEnum.QUOTE_OPEN);
            string name = ExtractText();
            ExpectKeyword(ScriptSymbolEnum.QUOTE_CLOSE);

            FlowStep step = new FlowStep() { FlowStepType = FlowStepTypeEnum.CHECK_VALUE, Name = name };

            // At the point of use a step's result and an input are the same thing, so both are {{name}}.
            ExpectKeyword(ScriptSymbolEnum.QUOTE_OPEN);
            string? reference = ExtractVariable();
            ExpectKeyword(ScriptSymbolEnum.QUOTE_CLOSE);

            if (reference != null)
                step.FlowStepReference = new FlowStep() { Name = reference };

            ExpectCondition(step);
            ExpectEnd();

            return step;
        }
    }
}
