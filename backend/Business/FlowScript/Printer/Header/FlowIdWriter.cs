using Business.FlowScript.Catalogs;
using Core.Models.Database;

namespace Business.FlowScript.Text.Header
{
    internal sealed class FlowIdWriter : BaseWriter
    {
        private readonly Flow _flow;

        public FlowIdWriter(Flow flow)
        {
            _flow = flow;
        }

        protected override void Compose()
        {
            // Id:      8f14e45f-ea2b-4c3f-9f1a-77f0d2a3b111
            WriteKeyword(ScriptSymbolEnum.FLOWFIELD_ID);
            WriteGapUntil(FIELD_GAP_UNTIL);
            WriteText(_flow.PublicId.ToString());
        }
    }
}
