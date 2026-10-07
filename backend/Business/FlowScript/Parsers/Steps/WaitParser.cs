using Business.FlowScript.Catalogs;
using Business.FlowScript.Models.Text;
using Core.Enums;
using Core.Models.Database;

namespace Business.FlowScript.Parsers.Steps
{
    internal sealed class WaitParser : BaseStepParser
    {
        public WaitParser(IReadOnlyList<ScriptToken> tokens) : base(tokens)
        {
        }

        public override FlowStep Parse()
        {
            // Wait  800ms   [to 1200ms]
            ExpectKeyword(FlowStepTypeEnum.WAIT);
            FlowStep step = new FlowStep() { FlowStepType = FlowStepTypeEnum.WAIT };

            step.WaitForMilliseconds = ExtractMilliseconds();

            if (ExpectOptionalKeyword(ScriptSymbolEnum.TO))
                step.WaitForMillisecondsMax = ExtractMilliseconds();

            ExpectEnd();

            return step;
        }
    }
}
