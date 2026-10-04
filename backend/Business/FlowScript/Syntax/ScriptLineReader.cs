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
                    return SyntaxFacts.Quote(token.Text);

                return $"\"{token.Text}\"";
            }
        }

        /// <summary>The bare word that many tokens ahead, or empty when it is text or past the end.</summary>
        public string Peek(int ahead)
        {
            int index = _index + ahead;
            if (HasFailed || index >= _line.Tokens.Count || _line.Tokens[index].IsQuoted)
                return string.Empty;

            return _line.Tokens[index].Text;
        }

        public string Peek()
        {
            return Peek(0);
        }

        public bool Is(string word)
        {
            return Peek() == word;
        }

        public bool Take(string word)
        {
            if (!Is(word))
                return false;

            _index++;
            return true;
        }

        /// <summary>Moves past the next token, once Peek has said what it is.</summary>
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

        public string? Text(string what)
        {
            return Text(DiagnosticCodeEnum.ARGUMENT_EXPECTED, $"Expected {what} in {Delimiters()}.");
        }

        public string? Text(DiagnosticCodeEnum code, string message)
        {
            if (HasFailed)
                return null;

            if (IsAtEnd || !_line.Tokens[_index].IsQuoted)
            {
                Fail(code, message);
                return null;
            }

            ScriptToken token = _line.Tokens[_index];
            if (!IsSound(token))
                return null;

            _index++;
            return token.Text;
        }

        public string? Word(string what)
        {
            string word = Peek();
            if (word.Length == 0)
            {
                Fail(DiagnosticCodeEnum.ARGUMENT_EXPECTED, $"Expected {what}.");
                return null;
            }

            _index++;
            return word;
        }

        public int? Integer(string what)
        {
            int column = Column;
            string? word = Word(what);
            if (word == null)
                return null;

            int? value = SyntaxFacts.Integer(word);
            if (value == null)
                Fail(DiagnosticCodeEnum.NUMBER_MALFORMED, $"\"{word}\" is not a whole number. Expected {what}.", column);

            return value;
        }

        public float? Float(string what)
        {
            int column = Column;
            string? word = Word(what);
            if (word == null)
                return null;

            float? value = SyntaxFacts.Float(word);
            if (value == null)
                Fail(DiagnosticCodeEnum.NUMBER_MALFORMED, $"\"{word}\" is not a number. Expected {what}.", column);

            return value;
        }

        public int? Milliseconds(string what)
        {
            int column = Column;
            string? word = Word($"{what}, such as 800ms");
            if (word == null)
                return null;

            int? value = SyntaxFacts.Milliseconds(word);
            if (value == null)
                Fail(DiagnosticCodeEnum.DURATION_MALFORMED, $"\"{word}\" is not a duration. Expected {what}, such as 800ms.", column);

            return value;
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
                if (!IsSound(_line.Tokens[i]))
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

            string textEnd = SyntaxFacts.Symbol(ScriptSymbolEnum.TEXT_END);
            string message = $"{Current} was not expected here.";

            // A stray "]>" further on means text ended at the first one, earlier than it was meant to.
            if (_line.Tokens.Skip(_index).Any(x => !x.IsQuoted && x.Text.Contains(textEnd, StringComparison.Ordinal)))
                message += $" Text can't contain \"{textEnd}\", so the text before it ended there.";

            Fail(DiagnosticCodeEnum.TOKEN_UNEXPECTED, message);
        }


        // ================================================================
        // Private methods
        // ================================================================

        // A text token that closed, and holds no delimiter of its own.
        private bool IsSound(ScriptToken token)
        {
            if (!token.IsQuoted)
                return true;

            string textStart = SyntaxFacts.Symbol(ScriptSymbolEnum.TEXT_START);
            string textEnd = SyntaxFacts.Symbol(ScriptSymbolEnum.TEXT_END);

            if (token.IsUnclosed)
            {
                Fail(DiagnosticCodeEnum.TEXT_UNCLOSED, $"This text never closes: expected \"{textEnd}\" before the end of the line.", token.Column);
                return false;
            }

            if (SyntaxFacts.HasTextDelimiter(token.Text))
            {
                Fail(DiagnosticCodeEnum.TEXT_DELIMITER, $"Text can't contain \"{textStart}\" or \"{textEnd}\".", token.Column);
                return false;
            }

            return true;
        }

        private static string Delimiters()
        {
            return $"{SyntaxFacts.Symbol(ScriptSymbolEnum.TEXT_START)} {SyntaxFacts.Symbol(ScriptSymbolEnum.TEXT_END)}";
        }
    }
}
