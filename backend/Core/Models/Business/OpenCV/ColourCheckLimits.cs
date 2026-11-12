namespace Core.Models.Business.OpenCV
{
    /// <summary>
    /// How far Shape and colour lets a candidate whose shape passed differ from its template. Fixed
    /// rather than per step: on simulated PCs a true match moved less than this, and another state
    /// of the same picture more.
    /// </summary>
    public static class ColourCheckLimits
    {
        /// <summary>The share of the template's own colour its average may move by.</summary>
        public const float COLOUR_SHARE = 0.28f;

        /// <summary>What it may move by whatever its share, for a template with hardly any colour.</summary>
        public const float COLOUR_FLOOR = 8f;

        /// <summary>The contrast allowed against the template's, after a light blur of both.</summary>
        public const float CONTRAST_MIN = 0.85f;
        public const float CONTRAST_MAX = 1f / CONTRAST_MIN;
    }
}
