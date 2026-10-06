using Business.FlowScript.Catalogs;
using Business.FlowScript.Models.Binding;
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

        public override FlowStepSchemaBindng Parse()
        {
            // Wait  800ms   [to 1200ms]
            FlowStepSchemaBindng result = new FlowStepSchemaBindng();
            
            ExpectKeyword(FlowStepTypeEnum.WAIT);
            result.Step = new FlowStep() { FlowStepType = FlowStepTypeEnum.WAIT };

            result.Step.WaitForMilliseconds = ExtractMilliseconds();

            if (ExpectOptionalKeyword(ScriptSymbolEnum.TO))
                result.Step.WaitForMillisecondsMax = ExtractMilliseconds();

            ExpectEnd();

            return result;
        }
    }
}
