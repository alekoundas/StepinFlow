using Business.Ai;
using Core.Models.Business;
using Core.Models.Dtos;


namespace Transport.Ipc.Handlers.Ai
{
    /// <summary>What a question about this execution could show the model, by name.</summary>
    public class GetAiChatImagesHandler
    {
        private readonly IExecutionScreenshotReader _executionScreenshotReader;

        public GetAiChatImagesHandler(IExecutionScreenshotReader executionScreenshotReader)
        {
            _executionScreenshotReader = executionScreenshotReader;
        }

        public async Task<ResultDto<List<string>>> HandleAsync(int executionId, CancellationToken ct)
        {
            IReadOnlyList<AiImage> images = await _executionScreenshotReader.GetForExecutionAsync(executionId, ct);

            return ResultDto<List<string>>.Success(images.Select(x => x.Name).ToList());
        }
    }
}
