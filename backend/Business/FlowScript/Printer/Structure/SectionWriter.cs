using Business.FlowScript.Catalogs;

namespace Business.FlowScript.Text.Structure
{
    internal sealed class SectionWriter : BaseWriter
    {
        private readonly ScriptSymbolEnum _section;

        public SectionWriter(ScriptSymbolEnum section)
        {
            _section = section;
        }

        protected override void Compose()
        {
            // Areas: | Points: | Inputs: | Templates: | Steps:
            WriteKeyword(_section);
        }
    }
}
