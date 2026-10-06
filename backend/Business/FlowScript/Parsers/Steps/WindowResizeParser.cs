using Business.FlowScript.Catalogs;
using Business.FlowScript.Models.Binding;
using Business.FlowScript.Models.Text;
using Core.Enums;
using Core.Models.Database;

namespace Business.FlowScript.Parsers.Steps
{
    internal sealed class WindowResizeParser : StepParser
    {
        public WindowResizeParser(IReadOnlyList<ScriptToken> tokens) : base(tokens)
        {
        }

        public override FlowStepSchemaBindng Parse()
        {
            // Resize Window  process <[ chrome.exe ]>   [title starts with <[ Swag ]>]   size 1280 720
            FlowStepSchemaBindng result = new FlowStepSchemaBindng();

            ExpectKeyword(FlowStepTypeEnum.WINDOW_RESIZE);
            result.Step = new FlowStep() { FlowStepType = FlowStepTypeEnum.WINDOW_RESIZE };

            ExpectWindow(result.Step);

            ExpectKeyword(ScriptSymbolEnum.SIZE);
            result.Step.WindowWidth = ExtractInteger();
            result.Step.WindowHeight = ExtractInteger();

            ExpectEnd();

            return result;
        }
    }
}
