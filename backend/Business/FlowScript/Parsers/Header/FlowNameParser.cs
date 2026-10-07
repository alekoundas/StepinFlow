using Business.FlowScript.Catalogs;
using Business.FlowScript.Diagnostics;
using Business.FlowScript.Models.Text;
using Core.Helpers;

namespace Business.FlowScript.Parsers.Header
{
    internal sealed class FlowNameParser : BaseParser<string>
    {
        public FlowNameParser(IReadOnlyList<ScriptToken> tokens) : base(tokens)
        {
        }

        public override string Parse()
        {
            // Flow:    Login and add to cart
            ExpectKeyword(ScriptSymbolEnum.FLOWFIELD_NAME);
            string name = ExtractText();
            string? error = FileNameHelper.Validate(name);
            if (error != null)
                throw new ScriptSyntaxException(Diagnostic.Error(DiagnosticCodeEnum.FLOW_NAME_INVALID, CurrentToken.Line, CurrentToken.Column, error));

            ExpectEnd();


            return name;
        }
    }
}
