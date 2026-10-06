using Business.FlowScript.Catalogs;
using Business.FlowScript.Models.Binding;
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

        public override FlowStepSchemaBindng Parse()
        {
            // Check Value  <[ name ]>   <[ {{Step name}} ]> > <[ 100 ]>
            FlowStepSchemaBindng result = new FlowStepSchemaBindng();
            ExpectKeyword(FlowStepTypeEnum.CHECK_VALUE);

            ExpectKeyword(ScriptSymbolEnum.QUOTE_OPEN);
            string name = ExtractText();
            ExpectKeyword(ScriptSymbolEnum.QUOTE_CLOSE);

            result.Step = new FlowStep() { FlowStepType = FlowStepTypeEnum.CHECK_VALUE, Name = name };

            // At the point of use a step's result and an input are the same thing, so both are {{name}}.
            ExpectKeyword(ScriptSymbolEnum.QUOTE_OPEN);
            result.ReferenceName = ExtractVariable();
            ExpectKeyword(ScriptSymbolEnum.QUOTE_CLOSE);

            ExpectCondition(result.Step);
            ExpectEnd();

            return result;
        }
    }
}
