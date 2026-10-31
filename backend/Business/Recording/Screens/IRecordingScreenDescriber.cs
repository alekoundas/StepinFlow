using Core.Models.Dtos;

namespace Business.Recording.Screens
{
    public interface IRecordingScreenDescriber
    {
        /// <summary>
        /// What the local model sees at the kept presses worth a look: a sentence for the first
        /// screen and every one where a new page began, a few words for every click nothing else
        /// named. Empty when the local model cannot take a picture. screenAt is the recording
        /// session's.
        /// </summary>
        Task<IReadOnlyList<ScreenDescriptionDto>> DescribeAsync(
            RecordingResultDto recording,
            ReviewedRecordingDto reviewed,
            Func<int, RecordingScreenDto?> screenAt,
            Action<int, int> onProgress,
            CancellationToken ct = default);
    }
}
