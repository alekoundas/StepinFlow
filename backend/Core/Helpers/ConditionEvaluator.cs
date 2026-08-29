using System.Globalization;
using System.Text.RegularExpressions;

using Core.Enums;
using Core.Models.Database;

namespace Core.Helpers
{
    /// <summary>
    /// Tests a value against a step's condition. Shared so Check Value and Read Text agree on what
    /// "contains" means, and so a dry run answers the same as a real one.
    /// </summary>
    public static class ConditionEvaluator
    {
        /// <summary>The condition as a sentence, so a failure can say what it was holding out for.</summary>
        public static string Describe(FlowStep step)
        {
            string end = string.IsNullOrWhiteSpace(step.ConditionTextEnd)
                ? string.Empty
                : $" .. {step.ConditionTextEnd}";

            return $"{step.ConditionType} {step.ConditionText}{end}".Trim();
        }

        public static bool IsSatisfied(string? value, ConditionTypeEnum? condition, string expected, string expectedEnd)
        {
            string actual = value ?? string.Empty;

            switch (condition)
            {
                case ConditionTypeEnum.EQUALS:
                    return string.Equals(actual, expected, StringComparison.OrdinalIgnoreCase);

                case ConditionTypeEnum.NOT_EQUALS:
                    return !string.Equals(actual, expected, StringComparison.OrdinalIgnoreCase);

                case ConditionTypeEnum.CONTAINS:
                    return actual.Contains(expected, StringComparison.OrdinalIgnoreCase);

                case ConditionTypeEnum.NOT_CONTAINS:
                    return !actual.Contains(expected, StringComparison.OrdinalIgnoreCase);

                case ConditionTypeEnum.MATCHES_REGEX:
                    return IsRegexMatch(actual, expected);

                case ConditionTypeEnum.IS_EMPTY:
                    return string.IsNullOrWhiteSpace(actual);

                case ConditionTypeEnum.IS_NOT_EMPTY:
                    return !string.IsNullOrWhiteSpace(actual);

                case ConditionTypeEnum.GREATER_THAN:
                    return Compare(actual, expected, out int greater) && greater > 0;

                case ConditionTypeEnum.LESS_THAN:
                    return Compare(actual, expected, out int less) && less < 0;

                case ConditionTypeEnum.BETWEEN:
                    return IsBetween(actual, expected, expectedEnd);

                default:
                    return false;
            }
        }


        // ================================================================
        // Private methods
        // ================================================================

        private static bool IsRegexMatch(string actual, string pattern)
        {
            try
            {
                return Regex.IsMatch(actual, pattern, RegexOptions.IgnoreCase, TimeSpan.FromMilliseconds(200));
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

        /// <summary>Text that will not parse as a number is a failure, not a false result.</summary>
        private static bool Compare(string actual, string expected, out int comparison)
        {
            comparison = 0;

            if (!TryParse(actual, out double left) || !TryParse(expected, out double right))
                return false;

            comparison = left.CompareTo(right);
            return true;
        }

        private static bool IsBetween(string actual, string from, string to)
        {
            if (!TryParse(actual, out double value) || !TryParse(from, out double low) || !TryParse(to, out double high))
                return false;

            return value >= low && value <= high;
        }

        private static bool TryParse(string text, out double value)
        {
            return double.TryParse(text, NumberStyles.Any, CultureInfo.InvariantCulture, out value);
        }
    }
}
