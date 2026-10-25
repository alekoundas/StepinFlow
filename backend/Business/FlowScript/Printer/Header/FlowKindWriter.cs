using Business.FlowScript.Catalogs;
using Core.Models.Database;

namespace Business.FlowScript.Text.Header
{
    internal sealed class FlowKindWriter : BaseWriter
    {
        private readonly Flow _flow;

        public FlowKindWriter(Flow flow)
        {
            _flow = flow;
        }

        protected override void Compose()
        {
            // Kind:    qa test   sub-flow
            WriteKeyword(ScriptSymbolEnum.FLOWFIELD_KIND);
            WriteGapUntil(FIELD_GAP_UNTIL);

            FlowKindEnum kind = FlowKindEnum.AUTOMATION;
            if (_flow.IsQaTest)
                kind = FlowKindEnum.QA_TEST;

            WriteKeyword(kind);

            if (_flow.IsSubFlow)
            {
                WriteGap();
                WriteKeyword(FlowKindEnum.SUB_FLOW);
            }
        }
    }
}
