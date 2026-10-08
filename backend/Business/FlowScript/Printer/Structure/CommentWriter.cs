using Business.FlowScript.Catalogs;

namespace Business.FlowScript.Text.Structure
{
    internal sealed class CommentWriter : BaseWriter
    {
        private readonly string _comment;

        public CommentWriter(string comment)
        {
            _comment = comment;
        }

        protected override void Compose()
        {
            // # A fresh profile every time.   A blank line is a bare #.
            WriteKeyword(ScriptSymbolEnum.COMMENT);

            string text = _comment.Trim();
            if (text.Length > 0)
                WriteText(text);
        }
    }
}
