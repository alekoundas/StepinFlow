using Core.Enums;
using Core.Models.Business;

namespace Core.Helpers
{
    /// <summary>
    /// Whether a window is the one a <see cref="WindowQuery"/> is looking for.
    ///
    /// Separate from the adapter that enumerates windows: enumerating is the machine, deciding is a
    /// rule. Kept here it can be checked against a hand built list instead of needing the real
    /// application open.
    /// </summary>
    public static class WindowNameMatchHelper
    {
        /// <summary>
        /// A blank process name or title pattern matches anything, so a query that fills in neither matches every window rather than none.
        /// </summary>
        public static bool Matches(string title, string processName, WindowQuery query)
        {
            if (string.IsNullOrWhiteSpace(title))
                return false;

            if (!string.IsNullOrWhiteSpace(query.ProcessName) && !string.Equals(processName, query.ProcessName, StringComparison.OrdinalIgnoreCase))
                return false;

            if (!string.IsNullOrWhiteSpace(query.TitlePattern) && !IsTitleMatch(title, query.TitlePattern, query.TitleMatchMode))
                return false;

            return true;
        }

        public static bool IsTitleMatch(string title, string pattern, TitleMatchModeEnum mode)
        {
            switch (mode)
            {
                case TitleMatchModeEnum.EQUALS:
                    return string.Equals(title, pattern, StringComparison.OrdinalIgnoreCase);

                case TitleMatchModeEnum.STARTS_WITH:
                    return title.StartsWith(pattern, StringComparison.OrdinalIgnoreCase);

                case TitleMatchModeEnum.REGEX:
                    return RegexHelper.IsMatch(title, pattern);

                case TitleMatchModeEnum.CONTAINS:
                default:
                    return title.Contains(pattern, StringComparison.OrdinalIgnoreCase);
            }
        }
    }
}
