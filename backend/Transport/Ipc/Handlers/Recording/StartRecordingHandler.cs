using Business.Recording.Session;
using Core.Models.Dtos;

namespace Transport.Ipc.Handlers
{
    public class StartRecordingHandler
    {
        private readonly IRecordingSessionService _recordingSessionService;

        public StartRecordingHandler(IRecordingSessionService recordingSessionService)
        {
            _recordingSessionService = recordingSessionService;
        }

        public async Task<ResultDto<bool>> HandleAsync(RecordingStartDto dto, CancellationToken ct)
        {
            return await _recordingSessionService.StartAsync(dto.FlowId, dto.ElectronProcessId, ct);
        }
    }
}
