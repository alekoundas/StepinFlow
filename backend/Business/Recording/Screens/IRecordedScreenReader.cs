using Business.Recording.Models.Session;
using Core.Models.Dtos;

namespace Business.Recording.Screens
{
    public interface IRecordedScreenReader
    {
        /// <summary>
        /// Reads every press's screenshot whole and says what changed between each and the next,
        /// in action order. Empty with fewer than two presses.
        /// </summary>
        Task<IReadOnlyList<ScreenChangeDto>> ReadAsync(
            IReadOnlyList<RecordedActionDto> actions,
            Func<int, RecordedPress?> pressOf,
            Action<int, int> onProgress,
            CancellationToken ct = default);
    }
}
