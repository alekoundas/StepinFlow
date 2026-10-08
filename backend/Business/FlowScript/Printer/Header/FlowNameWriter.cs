using Business.FlowScript.Catalogs;
using Core.Models.Database;

namespace Business.FlowScript.Text.Header
{
    internal sealed class FlowNameWriter : BaseWriter
    {
        private readonly Flow _flow;

        public FlowNameWriter(Flow flow)
        {
            _flow = flow;
        }

        protected override void Compose()
        {
            // Flow:    Login and add to cart
            WriteKeyword(ScriptSymbolEnum.FLOWFIELD_NAME);
            WriteGapUntil(FIELD_GAP_UNTIL);
            WriteText(_flow.Name);
        }
    }
}
