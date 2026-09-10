using System.Text.RegularExpressions;

using Core.Models.Business;
using Core.Models.Database;

namespace Core.Helpers
{
    /// <summary>
    /// Swaps {{name}} for what it stands for.
    ///
    /// One syntax for three things - a csv column, the viewport being executed, and what an earlier
    /// step produced - because at the point of use they are the same thing: a value the flow did not
    /// know when it was recorded.
    /// </summary>
    public static class VariableTranslator
    {
        /// <summary>Reserved names, resolved from the viewport rather than from the flow.</summary>
        public static readonly string[] ViewportNames = ["width", "height"];

        private static readonly TimeSpan _patternTimeout = TimeSpan.FromMilliseconds(200);

        // Four braces are an escaped pair, the way string.Format escapes one. Anything else between
        // a pair of braces is a name: names hold spaces and punctuation, so the only thing ruled
        // out is another brace.
        private static readonly Regex _variable = new Regex(
            @"(\{\{\{\{)|\{\{\s*([^{}]+?)\s*\}\}",
            RegexOptions.Compiled,
            _patternTimeout);

        /// <summary>
        /// The fields a variable can be written into - the ones the workers resolve.
        ///
        /// The regex fields are deliberately absent. A pattern is full of braces of its own, and
        /// "keep only" would be a strange place to want a value the flow did not know.
        /// </summary>
        public static IEnumerable<string> VariableBearingText(FlowStep step)
        {
            yield return step.KeyboardInputText;
            yield return step.RunCommandValue;
            yield return step.Message;
            yield return step.ConditionText;
            yield return step.ConditionTextEnd;
        }

        /// <summary>
        /// Why a step stopped, said the same way wherever it happens. Names the variables rather
        /// than the fields, because the variable is what the author wrote and can go and fix.
        /// </summary>
        public static string DescribeUnresolved(IReadOnlyCollection<string> names)
        {
            IEnumerable<string> written = names.Select(x => "{{" + x + "}}");

            return $"Nothing has a value for {string.Join(", ", written)}.";
        }

        /// <summary>Every name the text asks for, in the order they appear, without duplicates.</summary>
        public static IReadOnlyList<string> Names(string? text)
        {
            if (string.IsNullOrEmpty(text))
                return [];

            List<string> names = new List<string>();

            foreach (Match match in _variable.Matches(text))
            {
                if (!match.Groups[2].Success)
                    continue;

                string name = match.Groups[2].Value;
                if (!names.Contains(name, StringComparer.OrdinalIgnoreCase))
                    names.Add(name);
            }

            return names;
        }

        /// <summary>
        /// The text with every name replaced by its value.
        ///
        /// A name with no value is left exactly as it was written and reported instead. Substituting
        /// an empty string would type nothing into a password box and call it a pass; leaving the
        /// braces is at least visible.
        /// </summary>
        public static VariableTranslationResult Resolve(string? text, IReadOnlyDictionary<string, string> values)
        {
            if (string.IsNullOrEmpty(text))
                return new VariableTranslationResult { Text = text ?? string.Empty };

            List<string> unresolved = new List<string>();

            string resolved = _variable.Replace(text, match =>
            {
                if (match.Groups[1].Success)
                    return "{{";

                string name = match.Groups[2].Value;

                if (values.TryGetValue(name, out string? value))
                    return value;

                if (!unresolved.Contains(name, StringComparer.OrdinalIgnoreCase))
                    unresolved.Add(name);

                return match.Value;
            });

            return new VariableTranslationResult { Text = resolved, Unresolved = unresolved };
        }
    }
}
