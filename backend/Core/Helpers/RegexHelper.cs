using System.Text.RegularExpressions;

namespace Core.Helpers
{
    /// <summary>
    /// Every regular expression in the application runs through here. Nothing else in the solution
    /// may name <c>Regex</c> at all - the banned symbol lists make it a build error - so there is
    /// nowhere for a second set of rules to appear.
    ///
    /// Two rules have to hold wherever a pattern runs, and both are easy to forget once. A pattern
    /// that does not compile must not throw at the caller, because most of them are typed by
    /// whoever is authoring a flow and are half-written for as long as they are being typed. And a
    /// pattern that backtracks for ever has to give up, because it is competing for CPU with the
    /// application being tested.
    ///
    /// <see cref="Captures"/> and <see cref="Replace"/> are the exception: their patterns are
    /// written in this repository rather than typed by anybody, so a broken one is a bug and throws.
    /// </summary>
    public static class RegexHelper
    {
        // Long enough for any pattern somebody means, short enough that a catastrophic one gives up
        // instead of hanging an execution. Window matching runs a pattern once per visible window,
        // so there this is the budget per window rather than the total.
        private static readonly TimeSpan _patternTimeout = TimeSpan.FromMilliseconds(200);

        /// <summary>The first group of the pattern, or the whole match, or everything when there is no pattern.</summary>
        public static string Extract(string text, string pattern)
        {
            if (string.IsNullOrWhiteSpace(pattern))
                return text;

            try
            {
                Match match = Regex.Match(text, pattern, RegexOptions.None, _patternTimeout);
                if (!match.Success)
                    return string.Empty;

                return match.Groups.Count > 1 ? match.Groups[1].Value : match.Value;
            }
            catch (ArgumentException)
            {
                // A pattern the author is still typing is not a reason to fail the step.
                return text;
            }
            catch (RegexMatchTimeoutException)
            {
                return text;
            }
        }

        /// <summary>
        /// Whether the text matches, ignoring case. A pattern that does not compile matches nothing,
        /// and so does one that gives up: the question asked was whether this text matches, and
        /// neither answer is yes.
        /// </summary>
        public static bool IsMatch(string text, string pattern)
        {
            try
            {
                return Regex.IsMatch(text, pattern, RegexOptions.IgnoreCase, _patternTimeout);
            }
            catch (ArgumentException)
            {
                return false;
            }
            catch (RegexMatchTimeoutException)
            {
                return false;
            }
        }

        /// <summary>
        /// Why the pattern does not compile, or empty when it does. For the one place that has to
        /// say a pattern is wrong rather than quietly matching nothing - a form testing it.
        /// </summary>
        public static string PatternError(string pattern)
        {
            try
            {
                _ = Regex.Match(string.Empty, pattern, RegexOptions.None, _patternTimeout);
                return string.Empty;
            }
            catch (ArgumentException ex)
            {
                return ex.Message;
            }
        }

        /// <summary>
        /// One group's value for every match that filled it in, in the order they appear. The
        /// pattern is written in code rather than typed, so a broken one throws.
        /// </summary>
        public static IReadOnlyList<string> Captures(string text, string pattern, int group)
        {
            List<string> values = new List<string>();

            foreach (Match match in Regex.Matches(text, pattern, RegexOptions.None, _patternTimeout))
            {
                if (match.Groups[group].Success)
                    values.Add(match.Groups[group].Value);
            }

            return values;
        }

        /// <summary>
        /// Every match replaced by what <paramref name="replacement"/> makes of its groups - index 0
        /// is the whole match, and a group that did not take part is empty. The pattern is written
        /// in code rather than typed, so a broken one throws.
        /// </summary>
        public static string Replace(string text, string pattern, Func<IReadOnlyList<string>, string> replacement)
        {
            return Regex.Replace(
                text,
                pattern,
                match =>
                {
                    string[] groups = new string[match.Groups.Count];
                    for (int i = 0; i < groups.Length; i++)
                        groups[i] = match.Groups[i].Value;

                    return replacement(groups);
                },
                RegexOptions.None,
                _patternTimeout);
        }
    }
}
