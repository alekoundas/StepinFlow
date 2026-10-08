using Business.FlowScript.Catalogs;
using System.Globalization;
using System.Text;

namespace Business.FlowScript.Text
{
    /// <summary>
    /// Writes one line of a script.
    /// Mirror of a parser reading one: where a parser expects and extracts a piece at a time, a
    /// writer writes one, in the order the line reads.
    /// </summary>
    internal abstract class BaseWriter
    {
        // The default amount of whitespace per case.
        // Used to line up text in the same column.
        protected const int FIELD_GAP_UNTIL = 9;        // Flow:    Login
        protected const int NAME_GAP_UNTIL = 20;        // <[ Browser ]>       window process ...
        protected const int TEMPLATE_GAP_UNTIL = 28;    // <[ template-k3x9q.png ]>    click ...
        protected const int KEYWORD_GAP_UNTIL = 16;     // Find Image      <[ Find login ]> ...

        private readonly StringBuilder _line = new StringBuilder();

        /// <summary>
        /// The line, without its indentation.
        /// </summary>
        public string Write()
        {
            _line.Clear();
            Compose();

            return _line.ToString();
        }

        // Write the line's pieces, in the order it reads.
        protected abstract void Compose();


        // ================================================================
        // Protected methods - words
        // ================================================================

        /// <summary>
        /// Find the Keyword Text from the Keyword Type and Write it.
        /// </summary>
        protected void WriteKeyword(Enum value)
        {
            WriteText(ScriptKeywordCatalog.GetTextOfKeyword(value));
        }


        /// <summary>
        /// Find the Keyword Text from the Keyword Type + Modifier and write it.
        /// </summary>
        protected void WriteKeyword(Enum value, Enum modifier)
        {
            WriteText(ScriptKeywordCatalog.GetTextOfKeyword(value, modifier));
        }

        /// <summary>
        /// Write given value between the quotes symbol from catalog.
        /// </summary>
        protected void WriteQuote(string? text)
        {
            // Between <[ and ]>
            string quoteOpen = ScriptKeywordCatalog.GetTextOfKeyword(ScriptSymbolEnum.QUOTE_OPEN);
            string quoteClose = ScriptKeywordCatalog.GetTextOfKeyword(ScriptSymbolEnum.QUOTE_CLOSE);

            WriteText($"{quoteOpen} {text} {quoteClose}");
        }

        /// <summary>
        /// Write text as is.
        /// Writes a whitespace at the begining if needed.
        /// </summary>
        protected void WriteText(string text)
        {
            // Check if the last char was a whitespace.
            if (_line.Length > 0 && _line[^1] != ' ')
                _line.Append(' ');

            _line.Append(text);
        }


        // ================================================================
        // Protected methods - numbers
        // ================================================================

        /// <summary>
        /// Write an integer.
        /// </summary>
        protected void WriteNumber(int value)
        {
            WriteText(value.ToString(CultureInfo.InvariantCulture));
        }

        /// <summary>
        /// Write a Float. Hide trailing 0s
        /// </summary>
        protected void WriteNumber(float value)
        {
            WriteText(value.ToString("0.####", CultureInfo.InvariantCulture));
        }

        /// <summary>
        /// Write a Float with precision of 2.
        /// </summary>
        protected void WriteRatio(float value)
        {
            WriteText(value.ToString("0.00", CultureInfo.InvariantCulture));
        }

        /// <summary>
        /// Write milliseconds. ex: 800ms
        /// </summary>
        protected void WriteMilliseconds(int milliseconds)
        {
            string keyword = ScriptKeywordCatalog.GetTextOfKeyword(ScriptSymbolEnum.MILLISECONDS);
            WriteText(milliseconds.ToString(CultureInfo.InvariantCulture) + keyword);
        }

        /// <summary>
        /// Write DPI. ex: 120dpi
        /// </summary>
        protected void WriteDpi(int dpi)
        {
            string keyword = ScriptKeywordCatalog.GetTextOfKeyword(ScriptSymbolEnum.DPI);
            string dpiText = dpi.ToString(CultureInfo.InvariantCulture);
            WriteText(dpiText + keyword);
        }

        /// <summary>
        /// Write size ex: 1920x1080
        /// </summary>
        protected void WriteSize(int width, int height)
        {
            string keyword = ScriptKeywordCatalog.GetTextOfKeyword(ScriptSymbolEnum.SIZE_SEPARATOR);
            string heightText = height.ToString(CultureInfo.InvariantCulture);
            string widthText = width.ToString(CultureInfo.InvariantCulture);

            WriteText(widthText + keyword + heightText);
        }


        // ================================================================
        // Protected methods - layout
        // ================================================================

        
        /// <summary>
        /// Write whitespace.
        /// </summary>
        protected void WriteGap(int spaces = 3)
        {
            _line.Append(' ', spaces);
        }

        /// <summary>
        /// Write whitespace until a specific column.
        /// Used to line up text to a column.
        /// </summary>
        protected void WriteGapUntil(int column)
        {
            if (_line.Length >= column)
                _line.Append("  ");
            else
                _line.Append(' ', column - _line.Length);
        }
    }
}
