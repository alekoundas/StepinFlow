using Business.FlowScript.Catalogs;
using Business.FlowScript.Models.Text;

namespace Business.FlowScript.Parsers.Structure
{
    internal sealed class CommentParser : TokenParser<string>
    {
        public CommentParser(IReadOnlyList<ScriptToken> tokens) : base(tokens)
        {
        }

        public override string Parse()
        {
            // # A fresh profile every time.
            ExpectKeyword(ScriptSymbolEnum.COMMENT);
            string comment = ExtractText();
            ExpectEnd();

            return comment;
        }
    }
}
