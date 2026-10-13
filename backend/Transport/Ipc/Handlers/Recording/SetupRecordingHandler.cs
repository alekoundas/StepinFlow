using Business.Recording.Setup;
using Core.Models.Dtos;

namespace Transport.Ipc.Handlers
{
    public class SetupRecordingHandler
    {
        private readonly IRecordingSetupService _recordingSetupService;

        public SetupRecordingHandler(IRecordingSetupService recordingSetupService)
        {
            _recordingSetupService = recordingSetupService;
        }

        public Task<ResultDto<int>> HandleAsync(RecordingSetupDto dto, CancellationToken ct)
        {
            return _recordingSetupService.CreateFlowAsync(dto, ct);
        }
    }
}
