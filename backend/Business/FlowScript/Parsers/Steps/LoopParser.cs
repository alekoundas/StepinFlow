using Business.FlowScript.Catalogs;
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

        public override FlowStep Parse()
        {
            // Loop  5 times | forever | each match in <[ search ]>
            ExpectKeyword(FlowStepTypeEnum.LOOP);
            FlowStep step = new FlowStep() { FlowStepType = FlowStepTypeEnum.LOOP };

            if (ExpectOptionalKeyword(ScriptSymbolEnum.EACH))
            {
                ExpectKeyword(ScriptSymbolEnum.MATCH);
                ExpectKeyword(ScriptSymbolEnum.IN);

                ExpectKeyword(ScriptSymbolEnum.QUOTE_OPEN);
                step.FlowStepReference = new FlowStep() { Name = ExtractText() };
                ExpectKeyword(ScriptSymbolEnum.QUOTE_CLOSE);
            }
            else if (ExpectOptionalKeyword(ScriptSymbolEnum.FOREVER))
            {
                step.IsLoopInfinite = true;
            }
            else
            {
                step.LoopCount = ExtractInteger();
                ExpectKeyword(ScriptSymbolEnum.TIMES);
            }

            ExpectEnd();

            return step;
        }
    }
}
