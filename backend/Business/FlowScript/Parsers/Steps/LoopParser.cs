using Business.FlowScript.Catalogs;
using Business.FlowScript.Models.Binding;
using Business.FlowScript.Models.Text;
using Core.Enums;
using Core.Models.Database;

namespace Business.FlowScript.Parsers.Steps
{
    internal sealed class LoopParser : BaseStepParser
    {
        public LoopParser(IReadOnlyList<ScriptToken> tokens) : base(tokens)
        {
        }

        public override FlowStepSchemaBindng Parse()
        {
            // Loop  5 times | forever | each match in <[ search ]>
            FlowStepSchemaBindng result = new FlowStepSchemaBindng();

            ExpectKeyword(FlowStepTypeEnum.LOOP);
            result.Step = new FlowStep() { FlowStepType = FlowStepTypeEnum.LOOP };


            if (ExpectOptionalKeyword(ScriptSymbolEnum.EACH))
            {
                ExpectKeyword(ScriptSymbolEnum.MATCH);
                ExpectKeyword(ScriptSymbolEnum.IN);

                ExpectKeyword(ScriptSymbolEnum.QUOTE_OPEN);
                result.ReferenceName = ExtractText();
                ExpectKeyword(ScriptSymbolEnum.QUOTE_CLOSE);
            }
            else if (ExpectOptionalKeyword(ScriptSymbolEnum.FOREVER))
            {
                result.Step.IsLoopInfinite = true;
            }
            else
            {
                result.Step.LoopCount = ExtractInteger();
                ExpectKeyword(ScriptSymbolEnum.TIMES);
            }

            ExpectEnd();

            return result;
        }
    }
}
