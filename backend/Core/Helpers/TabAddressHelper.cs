namespace Core.Helpers
{
    /// <summary>
    /// Whether a browser tab is the one a browser tab area means, by its address.
    ///
    /// A rule rather than part of the adapter that reads the address bar, so it can be checked
    /// without a browser open. The address bar leaves out https:// and www., and a browser leaves
    /// the slash off a bare site, so both are left out on either side.
    /// </summary>
    public static class TabAddressHelper
    {
        private const string SCHEME_END = "://";
        private const string WWW = "www.";
        private const string DEFAULT_SCHEME = "https://";

        // What can follow the area's address in a tab's and still be the same page or one under it.
        private const string BOUNDARIES = "/?#:";

        /// <summary>
        /// The tab's address starts with the area's, at a boundary: example.com matches
        /// example.com/cart, not example.community.
        /// </summary>
        public static bool Matches(string tabAddress, string areaAddress)
        {
            string area = Bare(areaAddress).TrimEnd('/');
            if (area.Length == 0)
                return false;

            string tab = Bare(tabAddress);
            if (!tab.StartsWith(area, StringComparison.OrdinalIgnoreCase))
                return false;

            return tab.Length == area.Length || BOUNDARIES.Contains(tab[area.Length], StringComparison.Ordinal);
        }

        /// <summary>The address to open a browser at: as written, https:// in front when it has no scheme.</summary>
        public static string ToOpen(string areaAddress)
        {
            string address = areaAddress.Trim();
            if (address.Contains(SCHEME_END, StringComparison.Ordinal))
                return address;

            return DEFAULT_SCHEME + address;
        }


        // ================================================================
        // Private methods
        // ================================================================

        private static string Bare(string address)
        {
            string text = address.Trim();

            int scheme = text.IndexOf(SCHEME_END, StringComparison.Ordinal);
            if (scheme >= 0)
                text = text[(scheme + SCHEME_END.Length)..];

            if (text.StartsWith(WWW, StringComparison.OrdinalIgnoreCase))
                text = text[WWW.Length..];

            return text;
        }
    }
}
