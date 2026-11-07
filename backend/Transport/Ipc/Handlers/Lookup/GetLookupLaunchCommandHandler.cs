using Business.Shell;
using Core.Models.Dtos;
using Core.Ports;

namespace Transport.Ipc.Handlers
{
    /// <summary>
    /// What starts the app an open window belongs to, for an area's Started by. Empty when its
    /// program cannot be read, which an app running as administrator does to one that is not.
    /// </summary>
    public class GetLookupLaunchCommandHandler
    {
        private readonly IProcessService _processService;

        public GetLookupLaunchCommandHandler(IProcessService processService)
        {
            _processService = processService;
        }

        public Task<ResultDto<string>> HandleAsync(int processId, CancellationToken ct)
        {
            string? program = _processService.GetExecutablePath(processId);
            if (string.IsNullOrEmpty(program))
                return Task.FromResult(ResultDto<string>.Success(string.Empty));

            return Task.FromResult(ResultDto<string>.Success(LaunchCommandHelper.Of(program)));
        }
    }
}
