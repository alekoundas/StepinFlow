using System.Drawing;

using Business.FlowScript.Catalogs;
using Business.FlowScript.Models.Text;
using Core.Models.Database;

namespace Business.FlowScript.Parsers.Header
{
    internal sealed class FlowSizesParser : TokenParser<List<FlowViewport>>
    {
        public FlowSizesParser(IReadOnlyList<ScriptToken> tokens) : base(tokens)
        {
        }

        public override List<FlowViewport> Parse()
        {
            // Sizes:   1920x1080 390x844
            ExpectKeyword(ScriptSymbolEnum.FLOWFIELD_SIZES);

            List<FlowViewport> viewports = new List<FlowViewport>();
            do
            {
                Size size = ExtractSize();
                viewports.Add(new FlowViewport() { Width = size.Width, Height = size.Height, OrderNumber = viewports.Count });
            }
            while (!ExpectOptionalEnd());

            return viewports;
        }
    }
}
