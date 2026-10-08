using Business.FlowScript.Catalogs;
using Core.Models.Database;

namespace Business.FlowScript.Text.Header
{
    internal sealed class FlowSizesWriter : BaseWriter
    {
        private readonly IReadOnlyList<FlowViewport> _viewports;

        public FlowSizesWriter(IReadOnlyList<FlowViewport> viewports)
        {
            _viewports = viewports;
        }

        protected override void Compose()
        {
            // Sizes:   1920x1080 390x844
            WriteKeyword(ScriptSymbolEnum.FLOWFIELD_SIZES);
            WriteGapUntil(FIELD_GAP_UNTIL);

            foreach (FlowViewport viewport in _viewports.OrderBy(x => x.OrderNumber))
                WriteSize(viewport.Width, viewport.Height);
        }
    }
}
