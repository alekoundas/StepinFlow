using Business.FlowScript.Catalogs;
using Business.FlowScript.Models.Binding;
using Business.FlowScript.Models.Text;
using Core.Enums;
using Core.Models.Database;

namespace Business.FlowScript.Parsers.Steps
{
    internal sealed class CursorDragParser : BaseStepParser
    {
        public CursorDragParser(IReadOnlyList<ScriptToken> tokens) : base(tokens)
        {
        }

        public override FlowStepSchemaBindng Parse()
        {
            // Drag  at point <[ A ]> to point <[ B ]>
            ExpectKeyword(FlowStepTypeEnum.CURSOR_DRAG);

            ExpectKeyword(ScriptSymbolEnum.AT);
            (string? startPoint, string? startReference) = ExtractTarget();

            ExpectKeyword(ScriptSymbolEnum.TO);
            (string? endPoint, string? endReference) = ExtractTarget();

            ExpectEnd();

            return new FlowStepSchemaBindng()
            {
                Step = new FlowStep() { FlowStepType = FlowStepTypeEnum.CURSOR_DRAG },
                PointName = startPoint,
                ReferenceName = startReference,
                PointEndName = endPoint,
                ReferenceEndName = endReference,
            };
        }
    }
}
