using Business.Recording.Models.Session;
using Core.Models.Business;
using Core.Models.Dtos;

namespace Business.Recording.Screens
{
    public interface IRecordedScreenReader
    {
        /// <summary>
        /// Reads every press's screenshot whole, and the screen the recording stopped on, and says
        /// what changed between each and the next, in action order, with every line read.
        /// </summary>
        Task<RecordedScreens> ReadAsync(
            IReadOnlyList<RecordedActionDto> actions,
            Func<int, RecordedPress?> pressOf,
            RawImage? lastScreen,
            Action<int, int> onProgress,
            CancellationToken ct = default);
    }
}
