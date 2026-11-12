using Business.Recording.Models.Session;
using Core.Models.Dtos;

namespace Business.Recording.Screens
{
    public interface IRecordedScreenReader
    {
        /// <summary>
        /// Reads every press's screenshot whole, and the screen the recording stopped on, and says
        /// what changed between each and the next, in action order, which page each press was on,
        /// and the line under each click, with every line read. The elements are what each click
        /// hit, for a click that landed beside its words.
        /// </summary>
        Task<RecordedScreens> ReadAsync(
            IReadOnlyList<RecordedActionDto> actions,
            Func<int, RecordedPress?> pressOf,
            IReadOnlyList<ClickedElementDto> elements,
            RecordingScreenshot? lastScreen,
            Action<int, int> onProgress,
            CancellationToken ct = default);
    }
}
