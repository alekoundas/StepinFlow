using Business.FlowScript.Catalogs;
using Business.FlowScript.Models;
using Core.Enums;
using Core.Models.Database;

namespace Business.FlowScript.Text.Steps
{
    internal sealed class SubFlowWriter : BaseStepWriter
    {
        private readonly SubFlowReference? _subFlow;

        public SubFlowWriter(FlowStep step, SubFlowReference? subFlow) : base(step)
        {
            _subFlow = subFlow;
        }

        protected override void Compose()
        {
            // Sub Flow  <[ Checkout ]>   id <[ 3b0c1d52-6f7e-4a8b-9c0d-1e2f3a4b5c6d ]>
            WriteKeyword(FlowStepTypeEnum.SUB_FLOW);
            WriteGapUntil(KEYWORD_GAP_UNTIL);
            WriteQuote(_subFlow?.Name ?? string.Empty);

            // The Id is what finds it on another machine or branch; the name is for the reader.
            if (_subFlow?.PublicId != null)
            {
                WriteGap();
                WriteKeyword(ScriptSymbolEnum.ID);
                WriteQuote(_subFlow.PublicId.Value.ToString());
            }
        }
    }
}
