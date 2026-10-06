using Business.FlowScript.Models.Binding;
using Business.FlowScript.Models.Text;
using Core.Enums;
using Core.Models.Database;

namespace Business.FlowScript.Parsers.Steps
{
    internal sealed class WindowFocusParser : BaseStepParser
    {
        public WindowFocusParser(IReadOnlyList<ScriptToken> tokens) : base(tokens)
        {
        }

        public override FlowStepSchemaBindng Parse()
        {
            // Focus Window  process <[ chrome.exe ]>   [title starts with <[ Swag ]>]
            FlowStepSchemaBindng result = new FlowStepSchemaBindng();

            ExpectKeyword(FlowStepTypeEnum.WINDOW_FOCUS);
            result.Step = new FlowStep() { FlowStepType = FlowStepTypeEnum.WINDOW_FOCUS };
            
            ExpectWindow(result.Step);
            
            ExpectEnd();

            return result;
        }
    }
}
