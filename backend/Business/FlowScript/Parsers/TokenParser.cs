using System.Drawing;
using System.Globalization;

using Business.FlowScript.Catalogs;
using Business.FlowScript.Diagnostics;
using Business.FlowScript.Models.Text;
using Business.FlowScript.Syntax;

using Core.Enums;

namespace Business.FlowScript.Parsers
{
    /// <summary>
    /// Reads one line's tokens and expect them to be in the correct order and extract values.
    ///
    /// Expect methods takes a token that must be there and returns nothing. 
    /// Extract methods takes a token that must be there and returns its value. 
    /// Either throws at the token when it is not what was wanted (an Optional method never throws).
    /// </summary>
    internal abstract class TokenParser<TResult>
    {
        private int _index; // current Index of the token list.
        private readonly IReadOnlyList<ScriptToken> _tokens; // All Tokens of the script line.
        private readonly List<string> _expected = new List<string>(); // Used by Diagnosticts to generate the error.

        private ScriptToken CurrentToken { get { return _tokens[_index]; } }


        protected TokenParser(IReadOnlyList<ScriptToken> tokens)
        {
            _tokens = tokens;
        }

        /// <summary>
        /// Parse the line tokens.
        /// </summary>
        public abstract TResult Parse();


        // ================================================================
        // Protected methods - keywords
        // ================================================================

        /// <summary>
        /// Check the next token is a specific keyword.
        /// If not found, stop and throw with the expected reason.
        /// </summary>
        protected void ExpectKeyword<TEnum>(TEnum keyword) where TEnum : struct, Enum
        {
            if (CheckKeywordAt([keyword]) == null)
                throw Unexpected(Quoted(SyntaxFacts.Keyword(keyword)));

            IncreaseIndex();
        }

        /// <summary>
        /// Check the next token is a specific keyword.
        /// Doesnt throw.
        /// </summary>
        protected bool ExpectOptionalKeyword<TEnum>(TEnum keyword) where TEnum : struct, Enum
        {
            if (CheckKeywordAt([keyword]) == null)
            {
                _expected.Add(Quoted(SyntaxFacts.Keyword(keyword)));
                return false;
            }

            IncreaseIndex();
            return true;
        }

        /// <summary>
        /// Check the next token is a specific keyword from the array and extracts it.
        /// Empty array checks token against all TEnum values.
        /// If not found, stop and throw with the expected reason.
        /// </summary>
        protected TEnum ExtractKeyword<TEnum>(params TEnum[] allowed) where TEnum : struct, Enum
        {
            TEnum? keyword = CheckKeywordAt(allowed);
            if (keyword == null)
            {
                // The words named, or every word of TEnum when none are.
                if (allowed.Length > 0)
                    throw Unexpected(allowed.Select(x => Quoted(SyntaxFacts.Keyword(x))));

                throw Unexpected(ScriptKeywordCatalog.All.Where(x => x.Type is TEnum).Select(x => Quoted(x.Text)));
            }

            IncreaseIndex();
            return keyword.Value;
        }

        /// <summary>
        /// Check the next token is a specific keyword.
        /// Empty array checks token against all TEnum values.
        /// Doesnt throw.
        /// </summary>
        protected TEnum? ExtractOptionalKeyword<TEnum>() where TEnum : struct, Enum
        {
            TEnum? keyword = CheckKeywordAt<TEnum>([]);
            if (keyword == null)
            {
                _expected.AddRange(ScriptKeywordCatalog.All.Where(x => x.Type is TEnum).Select(x => Quoted(x.Text)));
                return null;
            }

            IncreaseIndex();
            return keyword;
        }

        /// <summary>
        /// Check the next token is a specific keyword.
        /// If not found, stop and throw with the expected reason.
        /// </summary>
        protected ScriptKeyword ExtractStepKeyword(FlowStepTypeEnum type)
        {
            ScriptKeyword? keyword = null;
            if (CurrentToken.Kind == ScriptTokenKindEnum.KEYWORD)
                keyword = ScriptKeywordCatalog.Get<FlowStepTypeEnum>(CurrentToken.Value);

            if (keyword == null || !type.Equals(keyword.Type))
                throw Unexpected(ScriptKeywordCatalog.All.Where(x => type.Equals(x.Type)).Select(x => Quoted(x.Text)));

            IncreaseIndex();
            return keyword;
        }


        // ================================================================
        // Protected methods - values
        // ================================================================

        /// <summary>
        /// Extract Quoted text body from current token (value of: #, ##, Flow:, Id:).
        /// </summary>
        protected string ExtractText()
        {
            if (CurrentToken.Kind != ScriptTokenKindEnum.QUOTE)
                throw Unexpected();

            string text = CurrentToken.Value;

            IncreaseIndex();
            return text;
        }

        /// <summary>
        /// Extract the text body of a valiable from current token: {{name}}
        /// </summary>
        protected string? ExtractVariable()
        {
            if (CurrentToken.Kind != ScriptTokenKindEnum.QUOTE)
                throw Unexpected();

            string text = CurrentToken.Value;
            bool isVariable = text.StartsWith("{{", StringComparison.Ordinal) && text.EndsWith("}}", StringComparison.Ordinal);
            if (text.Length > 0 && !isVariable)
                throw Unexpected("a name in {{ }}");

            IncreaseIndex();

            if (text.Length == 0)
                return null;

            return text[2..^2]; // Read from 2 till length - 2.
        }

        /// <summary>
        /// Extract an integer value from current token.
        /// </summary>
        protected int ExtractInteger()
        {
            if (CurrentToken.Kind != ScriptTokenKindEnum.NUMBER || Integer(CurrentToken.Value) is not int value)
                throw Unexpected("a whole number");

            IncreaseIndex();
            return value;
        }

        /// <summary>
        /// Extract a float value from current token.
        /// </summary>
        protected float ExtractFloat()
        {
            if (CurrentToken.Kind != ScriptTokenKindEnum.NUMBER)
                throw Unexpected("a number");

            if (!float.TryParse(CurrentToken.Value, NumberStyles.Float, CultureInfo.InvariantCulture, out float value))
                throw Unexpected("a number");

            IncreaseIndex();
            return value;
        }


        /// <summary>
        /// Extract milliseconds number from current token (ex. 800ms -> 800).
        /// </summary>
        protected int ExtractMilliseconds()
        {
            string unit = SyntaxFacts.Keyword(ScriptSymbolEnum.MILLISECONDS);

            int? value = Integer(WithoutUnit(unit));
            if (value == null)
                throw Unexpected($"a number ending in {Quoted(unit)}");

            IncreaseIndex();
            return value.Value;
        }

        /// <summary>
        /// Extract the dpi number from current token (ex 120dpi -> 120).
        /// </summary>
        protected int ExtractDpi()
        {
            string unit = SyntaxFacts.Keyword(ScriptSymbolEnum.DPI);

            int? dpi = Integer(WithoutUnit(unit));
            if (dpi == null || dpi <= 0)
                throw Unexpected($"a number ending in {Quoted(unit)}");

            IncreaseIndex();
            return dpi.Value;
        }

        /// <summary>
        /// Extract the dpi number from current token (ex. 1920x1080 -> Size(1920, 1080)).
        /// </summary>
        protected Size ExtractSize()
        {
            string separator = SyntaxFacts.Keyword(ScriptSymbolEnum.SIZE_SEPARATOR);

            if (CurrentToken.Kind != ScriptTokenKindEnum.NUMBER)
                throw Unexpected($"a width and a height joined by {Quoted(separator)}");

            string[] parts = CurrentToken.Value.Split(separator);
            if (parts.Length != 2)
                throw Unexpected($"a width and a height joined by {Quoted(separator)}");

            int? width = Integer(parts[0]);
            int? height = Integer(parts[1]);
            if (width == null || height == null)
                throw Unexpected($"a width and a height joined by {Quoted(separator)}");

            IncreaseIndex();
            return new Size(width.Value, height.Value);
        }

        /// <summary>
        /// Extract a Guid value from current token.
        /// </summary>
        protected Guid ExtractGuid()
        {
            if (CurrentToken.Kind != ScriptTokenKindEnum.QUOTE)
                throw Unexpected("an id");

            if (!Guid.TryParse(CurrentToken.Value, out Guid id))
                throw Unexpected("an id");

            IncreaseIndex();
            return id;
        }

        /// <summary>
        /// Expect END_OF_LINE from current token.
        /// </summary>
        protected void ExpectEnd()
        {
            if (CurrentToken.Kind != ScriptTokenKindEnum.END_OF_LINE)
                throw Unexpected("the end of the line");
        }

        /// <summary>
        /// Expect END_OF_LINE from current token.
        /// Doesnt throw.
        /// </summary>
        protected bool ExpectOptionalEnd()
        {
            if (CurrentToken.Kind != ScriptTokenKindEnum.END_OF_LINE)
            {
                _expected.Add("the end of the line");
                return false;
            }

            return true;
        }


        // ================================================================
        // Private methods 
        // ================================================================

        // Increase index and clear expectations. Nothing failed, no need to keep.
        private void IncreaseIndex()
        {
            _index++;
            _expected.Clear();
        }


        // Find the enum of the current token value against catalog. Empty array param means search all enum values of TEnum.
        private TEnum? CheckKeywordAt<TEnum>(TEnum[] allowed) where TEnum : struct, Enum
        {
            if (CurrentToken.Kind != ScriptTokenKindEnum.KEYWORD)
                return null;

            // Retrieve enum from token text.
            TEnum? found = ScriptKeywordCatalog.Get<TEnum>(CurrentToken.Value)?.As<TEnum>();
            if (found == null || (allowed.Length > 0 && !allowed.Contains(found.Value)))
                return null;

            return found;
        }

        // The number in front of a unit - "800" of "800ms".
        private string? WithoutUnit(string unit)
        {
            if (CurrentToken.Kind != ScriptTokenKindEnum.NUMBER)
                return null;

            if (!CurrentToken.Value.EndsWith(unit, StringComparison.Ordinal))
                return null;

            return CurrentToken.Value[..^unit.Length]; // From 0 till max - unit.Length
        }


        // ================================================================
        // Private methods - the message
        // ================================================================

        // What was wanted here, added to what could already have stood here, as the exception to throw.
        private ScriptSyntaxException Unexpected(params IEnumerable<string> expected)
        {
            _expected.AddRange(expected);

            return ScriptSyntaxException.Unexpected(CurrentToken, _expected);
        }


        private static int? Integer(string? text)
        {
            if (!int.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out int value))
                return null;

            return value;
        }

        private static string Quoted(string keyword)
        {
            return $"\"{keyword}\"";
        }
    }
}
