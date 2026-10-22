using Business.FlowScript.Catalogs;
using Business.FlowScript.Models.Text;
using Core.Enums;
using Core.Models.Database;

namespace Business.FlowScript.Parsers.Steps
{
    internal sealed class LoopParser : BaseStepParser
    {
        public LoopParser(IReadOnlyList<ScriptToken> tokens, ScriptScope scope) : base(tokens, scope)
        {
        }

        public override FlowStep Parse()
        {
            // Loop  <[ name ]>   5 times | forever
            ExpectKeyword(FlowStepTypeEnum.LOOP);

            ExpectKeyword(ScriptSymbolEnum.QUOTE_OPEN);
            string name = ExtractText();
            ExpectKeyword(ScriptSymbolEnum.QUOTE_CLOSE);

            FlowStep step = new FlowStep() { FlowStepType = FlowStepTypeEnum.LOOP, Name = name };

            if (ExpectOptionalKeyword(ScriptSymbolEnum.FOREVER))
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
