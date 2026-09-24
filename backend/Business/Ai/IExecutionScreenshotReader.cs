using Core.Models.Business;

namespace Business.Services.Ai
{
    public interface IExecutionScreenshotReader
    {
        /// <summary>
        /// The pictures worth showing a model for one execution, each with the sentence that says
        /// what it is: the templates the failing step hunted for, then the screen before it, then
        /// the screen at it.
        ///
        /// Empty when the model cannot read images, when the rule does not allow it, or when the
        /// execution kept none.
        /// </summary>
        Task<IReadOnlyList<AiImage>> GetForExecutionAsync(int executionId, CancellationToken ct = default);

        /// <summary>Whether images may be sent at all, which the ui asks to explain itself.</summary>
        Task<bool> IsAllowedAsync(CancellationToken ct = default);
    }
}
