using Business.FlowScript.Catalogs;
using Business.FlowScript.Models.Binding;
using Business.FlowScript.Models.Text;
using Core.Enums;
using Core.Models.Database;

namespace Business.FlowScript.Parsers.Steps
{
    internal sealed class CursorRelocateParser : BaseStepParser
    {
        public CursorRelocateParser(IReadOnlyList<ScriptToken> tokens) : base(tokens)
        {
        }

        public override FlowStepSchemaBindng Parse()
        {
            // Move  to point <[ X ]> | to <[ step ]> | to match
            ExpectKeyword(FlowStepTypeEnum.CURSOR_RELOCATE);

            ExpectKeyword(ScriptSymbolEnum.TO);
            (string? pointName, string? referenceName) = ExtractTarget();
            
            ExpectEnd();

            return new FlowStepSchemaBindng()
            {
                Step = new FlowStep() { FlowStepType = FlowStepTypeEnum.CURSOR_RELOCATE },
                PointName = pointName,
                ReferenceName = referenceName,
            };
        }
    }
}
