using Business.Recording.Models.Session;
using Core.Models.Dtos;

namespace Business.Recording.Elements
{
    public interface IClickedElementFinder
    {
        /// <summary>
        /// The element each click and drag of a stopped recording hit. Seconds per click on the cpu,
        /// so it is never done while recording. onProgress is told the clicks done and the clicks there are.
        /// </summary>
        Task<IReadOnlyList<ClickedElementDto>> FindAsync(
            IReadOnlyList<RecordedActionDto> actions,
            Func<int, RecordedPress?> pressOf,
            Action<int, int> onProgress,
            CancellationToken ct = default);
    }
}
