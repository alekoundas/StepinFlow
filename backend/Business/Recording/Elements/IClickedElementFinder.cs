using Business.Recording.Models.Session;
using Core.Models.Dtos;

namespace Business.Recording.Elements
{
    public interface IClickedElementFinder
    {
        /// <summary>
        /// The element each click, drag and gesture of a stopped recording hit. Seconds per click on
        /// the cpu, so it is never done while recording. A press found already, while the recording
        /// was paused to ask about it, is taken as it was found. onProgress is told the clicks done
        /// and the clicks there are. The detector is let go after.
        /// </summary>
        Task<IReadOnlyList<ClickedElementDto>> FindAsync(
            IReadOnlyList<RecordedActionDto> actions,
            Func<int, RecordedPress?> pressOf,
            IReadOnlyDictionary<int, ClickedElementDto> foundByPress,
            Action<int, int> onProgress,
            CancellationToken ct = default);

        /// <summary>
        /// What one press hit, at a point on the screen, while the recording is paused at it. The
        /// detector stays loaded for the next, and the stop's finding lets it go. Null when the point
        /// is outside the press's screenshot.
        /// </summary>
        Task<ClickedElementDto?> FindAtAsync(RecordedPress press, int screenX, int screenY, CancellationToken ct = default);

        /// <summary>Lets the detector go, for a recording discarded with it loaded.</summary>
        void Unload();
    }
}
