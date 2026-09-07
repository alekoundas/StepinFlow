using System.Text.RegularExpressions;

namespace Core.Helpers
{
    /// <summary>
    /// Narrows what OCR read down to the part that matters.
    ///
    /// Shared because the engine and the form's test both extract before they check, and a preview
    /// that narrowed differently from the execution would be worse than no preview.
    /// </summary>
    public static class TextExtractHelper
    {
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
                // A pattern the user is still typing is not a reason to fail the step.
                return text;
            }
            catch (RegexMatchTimeoutException)
            {
                return text;
            }
        }
    }
}
