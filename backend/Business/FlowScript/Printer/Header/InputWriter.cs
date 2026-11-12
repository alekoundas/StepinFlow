using Business.FlowScript.Catalogs;
using Core.Models.Database;

namespace Business.FlowScript.Text.Header
{
    internal sealed class InputWriter : BaseWriter
    {
        private readonly FlowInput _input;

        public InputWriter(FlowInput input)
        {
            _input = input;
        }

        protected override void Compose()
        {
            // <[ email ]>   [default <[ ops@example.com ]> | secret]
            // A default travels with the flow, so a clone and an import keep it. A secret's value
            // never reaches a file.
            WriteQuote(_input.Name);

            if (_input.IsSecret)
            {
                WriteGapUntil(NAME_GAP_UNTIL);
                WriteKeyword(ScriptSymbolEnum.SECRET);
                return;
            }

            if (_input.DefaultValue.Length > 0)
            {
                WriteGapUntil(NAME_GAP_UNTIL);
                WriteKeyword(ScriptSymbolEnum.DEFAULT);
                WriteQuote(_input.DefaultValue);
            }
        }
    }
}
