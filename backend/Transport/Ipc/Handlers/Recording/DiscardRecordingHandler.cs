using Business.Flows.DataService;
using Business.Recording.Session;
using Core.Models.Dtos;

namespace Transport.Ipc.Handlers
{
    public class DiscardRecordingHandler
    {
        private readonly IRecordingSessionService _recordingSessionService;
        private readonly DataService _dataService;

        public DiscardRecordingHandler(IRecordingSessionService recordingSessionService, DataService dataService)
        {
            _recordingSessionService = recordingSessionService;
            _dataService = dataService;
        }

        public async Task<ResultDto<bool>> HandleAsync(RecordingDiscardDto dto, CancellationToken ct)
        {
            await _recordingSessionService.DiscardAsync(ct);

            if (dto.FlowId == null)
                return ResultDto<bool>.Success(true);

            return await _dataService.Flow.DeleteRecordingSetupAsync(dto.FlowId.Value, ct);
        }
    }
}
