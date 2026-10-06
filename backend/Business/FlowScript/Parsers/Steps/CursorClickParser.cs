using Business.FlowScript.Catalogs;
using Business.FlowScript.Models.Binding;
using Business.FlowScript.Models.Text;
using Core.Enums;
using Core.Models.Database;

namespace Business.FlowScript.Parsers.Steps
{
    internal sealed class CursorClickParser : BaseStepParser
    {
        public CursorClickParser(IReadOnlyList<ScriptToken> tokens) : base(tokens)
        {
        }

        public override FlowStepSchemaBindng Parse()
        {
            // Click  at <[ step ]> | at point <[ X ]> | at match   [right | middle] [double | hold | release]
            FlowStepSchemaBindng result = new FlowStepSchemaBindng();

            ExpectKeyword(FlowStepTypeEnum.CURSOR_CLICK);
            result.Step = new FlowStep() { FlowStepType = FlowStepTypeEnum.CURSOR_CLICK };

            ExpectKeyword(ScriptSymbolEnum.AT);
            (string? pointName, string? referenceName) = ExtractTarget();
            result.PointName = pointName;
            result.ReferenceName = referenceName;

            // Left and single when left out, which is nearly every click.
            result.Step.CursorButtonType = ExtractOptionalKeyword<CursorButtonTypeEnum>() ?? CursorButtonTypeEnum.LEFT_BUTTON;
            result.Step.CursorButtonActionType = ExtractOptionalKeyword<CursorButtonActionTypeEnum>() ?? CursorButtonActionTypeEnum.SINGLE_CLICK;
            ExpectEnd();

            return result;
        }
    }
}
