namespace Business.ScreenText.Models
{
    /// <summary>What changed between two screenshots, by their text.</summary>
    public sealed class ScreenTextChange
    {
        // Most of the text changing is a new page or a dialog, rather than what a click did to one.
        private const double NEW_SCREEN_SHARE = 0.5;

        /// <summary>Lines on the second screenshot and not the first, in the order read.</summary>
        public IReadOnlyList<ScreenLine> Appeared { get; init; } = [];

        /// <summary>Lines on the first screenshot and not the second, in the order read.</summary>
        public IReadOnlyList<ScreenLine> Gone { get; init; } = [];

        /// <summary>How much of the two screenshots' words changed, from 0 to 1, counted in letters.</summary>
        public double ChangedShare { get; init; }

        public bool IsNewScreen
        {
            get
            {
                return ChangedShare >= NEW_SCREEN_SHARE;
            }
        }
    }
}
