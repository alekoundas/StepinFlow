using Business.FlowScript.Catalogs;
using Business.FlowScript.Models.Binding;
using Business.FlowScript.Models.Text;
using Core.Enums;
using Core.Models.Database;

namespace Business.FlowScript.Parsers.Steps
{
    internal sealed class WindowRelocateParser : StepParser
    {
        public WindowRelocateParser(IReadOnlyList<ScriptToken> tokens) : base(tokens)
        {
        }

        public override FlowStepSchemaBindng Parse()
        {
            // Move Window  process <[ chrome.exe ]>   [title starts with <[ Swag ]>]   to point <[ X ]>
            FlowStepSchemaBindng result = new FlowStepSchemaBindng();

            ExpectKeyword(FlowStepTypeEnum.WINDOW_RELOCATE);
            result.Step = new FlowStep() { FlowStepType = FlowStepTypeEnum.WINDOW_RELOCATE };

            ExpectWindow(result.Step);

            ExpectKeyword(ScriptSymbolEnum.TO);
            (string? pointName, string? referenceName) = ExtractTarget();
            result.PointName = pointName;
            result.ReferenceName = referenceName;

            ExpectEnd();

            return result;
        }
    }
}
