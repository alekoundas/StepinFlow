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
            // # A fresh profile every time.
            WriteKeyword(ScriptSymbolEnum.COMMENT);
            WriteText(_comment.Trim());
        }
    }
}
