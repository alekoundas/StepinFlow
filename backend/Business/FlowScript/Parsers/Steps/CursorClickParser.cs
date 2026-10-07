using Business.FlowScript.Catalogs;
using Business.FlowScript.Models.Text;
using Core.Enums;
using Core.Models.Database;

namespace Business.FlowScript.Parsers.Steps
{
    internal sealed class CursorClickParser : BaseStepParser
    {
        public CursorClickParser(IReadOnlyList<ScriptToken> tokens, ScriptScope scope) : base(tokens, scope)
        {
        }

        public override FlowStep Parse()
        {
            // Click  at <[ step ]> | at point <[ X ]> | at match   [right | middle] [double | hold | release]
            ExpectKeyword(FlowStepTypeEnum.CURSOR_CLICK);
            FlowStep step = new FlowStep() { FlowStepType = FlowStepTypeEnum.CURSOR_CLICK };

            ExpectKeyword(ScriptSymbolEnum.AT);
            (step.FlowPoint, step.FlowStepReference) = ExtractTarget();

            // Left and single when left out, which is nearly every click.
            step.CursorButtonType = ExtractOptionalKeyword<CursorButtonTypeEnum>() ?? CursorButtonTypeEnum.LEFT_BUTTON;
            step.CursorButtonActionType = ExtractOptionalKeyword<CursorButtonActionTypeEnum>() ?? CursorButtonActionTypeEnum.SINGLE_CLICK;
            ExpectEnd();

            return step;
        }
    }
}
