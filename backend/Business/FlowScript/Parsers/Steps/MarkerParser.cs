using Business.FlowScript.Models.Binding;
using Business.FlowScript.Models.Text;
using Core.Enums;
using Core.Models.Database;

namespace Business.FlowScript.Parsers.Steps
{
    internal sealed class MarkerParser : StepParser
    {
        public MarkerParser(IReadOnlyList<ScriptToken> tokens) : base(tokens)
        {
        }

        public override FlowStepSchemaBindng Parse()
        {
            // ## Sign in
            ExpectKeyword(FlowStepTypeEnum.MARKER);
            string name = ExtractText();
            ExpectEnd();

            return new FlowStepSchemaBindng()
            {
                Step = new FlowStep()
                {
                    FlowStepType = FlowStepTypeEnum.MARKER,
                    Name = name
                },
            };
        }
    }
}
