using Business.Recording.Generation.Models;

namespace Business.Recording.Generation
{
    public interface IRecordingWordsService
    {
        /// <summary>The message a model is sent for the words of a draft: its outline, then how a finished flow like it reads.</summary>
        string RequestOf(RecordingOutline outline, string example);

        /// <summary>
        /// The words for the outline's searches, in JSON of a shape Ollama holds the model to, with
        /// ids only the outline has. Asked again when an answer does not read, ATTEMPT_COUNT times
        /// at most; not when the model cannot be reached. onAttempt is told each attempt's number.
        /// </summary>
        Task<RecordingWordsResult> NameAsync(RecordingOutline outline, string request, bool isReadTextSent, Action<int>? onAttempt, CancellationToken ct = default);

        /// <summary>A QA test's stages, and its checks when what was read off the screen may be sent, for an outline whose searches have their names.</summary>
        Task<RecordingWordsResult> ProposeTestAsync(RecordingOutline outline, bool isReadTextSent, Action<int>? onAttempt, CancellationToken ct = default);
    }
}
