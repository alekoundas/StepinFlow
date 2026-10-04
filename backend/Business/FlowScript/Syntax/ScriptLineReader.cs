using Business.FlowScript.Catalogs;
using Business.FlowScript.Diagnostics;
using Business.FlowScript.Models.Binding;
using Business.FlowScript.Models.Text;

namespace Business.FlowScript.Syntax
{
    /// <summary>
    /// Walks one line's tokens. Every read takes a token or reports what belonged there, and End
    /// reports whatever is left, so a line either reads exactly or names the first place it stops
    /// making sense. Nothing after that first problem is reported: the rest of the line is not
    /// worth guessing at.
    /// </summary>
    internal sealed class ScriptLineReader
    {
        private readonly FlowScriptSchema _document;
        private readonly ScriptLine _line;
        private int _index;

        public ScriptLineReader(FlowScriptSchema document, ScriptLine line, int start)
        {
            _document = document;
            _line = line;
            _index = start;
        }

        public bool HasFailed { get; private set; }

        public bool IsAtEnd
        {
            get { return _index >= _line.Tokens.Count; }
        }

        /// <summary>Tokens are left and nothing has failed, so a loop over clauses goes on.</summary>
        public bool HasMore
        {
            get { return !HasFailed && !IsAtEnd; }
        }

        /// <summary>Where the next token starts, or just past the end of the line.</summary>
        public int Column
        {
            get { return _line.ColumnOf(_index); }
        }

        /// <summary>The next token as it was written, for a message.</summary>
        public string Current
        {
            get
            {
                if (IsAtEnd)
                    return "the end of the line";

                ScriptToken token = _line.Tokens[_index];
                if (token.IsQuoted)
                    return SyntaxFacts.Quote(token.Value);

                return $"\"{token.Value}\"";
            }
        }

        /// <summary>The unquoted value that many tokens ahead, or empty when that token is quoted or past the end.</summary>
        public string PeekUnquoted(int ahead)
        {
            int index = _index + ahead;
            if (HasFailed || index >= _line.Tokens.Count || _line.Tokens[index].IsQuoted)
                return string.Empty;

            return _line.Tokens[index].Value;
        }

        public string PeekUnquoted()
        {
            return PeekUnquoted(0);
        }

        public bool Is(string word)
        {
            return PeekUnquoted() == word;
        }

        public bool Take(string word)
        {
            if (!Is(word))
                return false;

            _index++;
            return true;
        }

        /// <summary>Moves past the next token, once PeekUnquoted has said what it is.</summary>
        public void Skip()
        {
            _index++;
        }

        public bool Expect(string word, DiagnosticCodeEnum code, string message)
        {
            if (Take(word))
                return true;

            Fail(code, message);
            return false;
        }

        public string? Quoted(string what)
        {
            return Quoted(DiagnosticCodeEnum.ARGUMENT_EXPECTED, $"Expected {what} in {Quotes()}.");
        }

        public string? Quoted(DiagnosticCodeEnum code, string message)
        {
            if (HasFailed)
                return null;

            if (IsAtEnd || !_line.Tokens[_index].IsQuoted)
            {
                Fail(code, message);
                return null;
            }

            ScriptToken token = _line.Tokens[_index];
            if (!IsWellQuoted(token))
                return null;

            _index++;
            return token.Value;
        }

        public string? Unquoted(string what)
        {
            string value = PeekUnquoted();
            if (value.Length == 0)
            {
                Fail(DiagnosticCodeEnum.ARGUMENT_EXPECTED, $"Expected {what}.");
                return null;
            }

            _index++;
            return value;
        }

        public int? Integer(string what)
        {
            int column = Column;
            string? value = Unquoted(what);
            if (value == null)
                return null;

            int? number = SyntaxFacts.Integer(value);
            if (number == null)
                Fail(DiagnosticCodeEnum.NUMBER_MALFORMED, $"\"{value}\" is not a whole number. Expected {what}.", column);

            return number;
        }

        public float? Float(string what)
        {
            int column = Column;
            string? value = Unquoted(what);
            if (value == null)
                return null;

            float? number = SyntaxFacts.Float(value);
            if (number == null)
                Fail(DiagnosticCodeEnum.NUMBER_MALFORMED, $"\"{value}\" is not a number. Expected {what}.", column);

            return number;
        }

        public int? Milliseconds(string what)
        {
            int column = Column;
            string? value = Unquoted($"{what}, such as 800ms");
            if (value == null)
                return null;

            int? milliseconds = SyntaxFacts.Milliseconds(value);
            if (milliseconds == null)
                Fail(DiagnosticCodeEnum.DURATION_MALFORMED, $"\"{value}\" is not a duration. Expected {what}, such as 800ms.", column);

            return milliseconds;
        }

        /// <summary>The longest keyword of that vocabulary starting here, or null without reporting - the caller knows what belonged.</summary>
        public ScriptKeyword? Keyword<TEnum>() where TEnum : struct, Enum
        {
            if (HasFailed)
                return null;

            ScriptKeyword? keyword = SyntaxFacts.ReadKeyword<TEnum>(_line.Tokens, _index);
            if (keyword != null)
                _index += keyword.TokenCount;

            return keyword;
        }

        /// <summary>A condition starting here, or null without reporting.</summary>
        public ConditionSyntax? Condition()
        {
            if (HasFailed)
                return null;

            ConditionSyntax? condition = SyntaxFacts.ReadCondition(_line.Tokens, _index);
            if (condition == null)
                return null;

            for (int i = _index; i < _index + condition.Words; i++)
            {
                if (!IsWellQuoted(_line.Tokens[i]))
                    return null;
            }

            _index += condition.Words;
            return condition;
        }

        public void Fail(DiagnosticCodeEnum code, string message)
        {
            Fail(code, message, Column);
        }

        public void Fail(DiagnosticCodeEnum code, string message, int column)
        {
            if (HasFailed)
                return;

            HasFailed = true;
            _document.Diagnostics.Add(Diagnostic.Error(code, _line.Number, column, message));
        }

        /// <summary>Reports the first token nothing read: a line ends where its grammar does.</summary>
        public void End()
        {
            if (HasFailed || IsAtEnd)
                return;

            string quoteClose = SyntaxFacts.Symbol(ScriptSymbolEnum.QUOTE_CLOSE);
            string message = $"{Current} was not expected here.";

            // A stray "]>" further on means a quote closed at the first one, earlier than it was meant to.
            if (_line.Tokens.Skip(_index).Any(x => !x.IsQuoted && x.Value.Contains(quoteClose, StringComparison.Ordinal)))
                message += $" Quoted text can't contain \"{quoteClose}\", so the quote before it closed there.";

            Fail(DiagnosticCodeEnum.TOKEN_UNEXPECTED, message);
        }


        // ================================================================
        // Private methods
        // ================================================================

        // An unquoted token, or a quoted one that closed and holds no quote of its own.
        private bool IsWellQuoted(ScriptToken token)
        {
            if (!token.IsQuoted)
                return true;

            string quoteOpen = SyntaxFacts.Symbol(ScriptSymbolEnum.QUOTE_OPEN);
            string quoteClose = SyntaxFacts.Symbol(ScriptSymbolEnum.QUOTE_CLOSE);

            if (token.IsQuoteUnclosed)
            {
                Fail(DiagnosticCodeEnum.QUOTE_UNCLOSED, $"This quote never closes: expected \"{quoteClose}\" before the end of the line.", token.Column);
                return false;
            }

            if (SyntaxFacts.HasQuote(token.Value))
            {
                Fail(DiagnosticCodeEnum.QUOTE_INSIDE, $"Quoted text can't contain \"{quoteOpen}\" or \"{quoteClose}\".", token.Column);
                return false;
            }

            return true;
        }

        private static string Quotes()
        {
            return $"{SyntaxFacts.Symbol(ScriptSymbolEnum.QUOTE_OPEN)} {SyntaxFacts.Symbol(ScriptSymbolEnum.QUOTE_CLOSE)}";
        }
    }
}
