using Business.Services.Ai.Providers;
using Core.Models.Dtos;


namespace Transport.Ipc.Handlers.Ai
{
    /// <summary>Whether a provider is set up, so the page can offer the button or explain why not.</summary>
    public class GetAiStatusHandler
    {
        private readonly IAiProviderService _providerService;

        public GetAiStatusHandler(IAiProviderService providerService)
        {
            _providerService = providerService;
        }

        public async Task<ResultDto<bool>> HandleAsync(CancellationToken ct)
        {
            bool isConfigured = await _providerService.IsConfiguredAsync(ct);
            return ResultDto<bool>.Success(isConfigured);
        }
    }
}
