using Core.Models.Dtos;

namespace Business.Recording.Setup
{
    public interface IRecordingSetupService
    {
        /// <summary>
        /// Creates the flow a recording goes into, with its main area and, when asked, a first step
        /// that launches the application. Returns the flow's id.
        /// </summary>
        Task<ResultDto<int>> CreateFlowAsync(RecordingSetupDto dto, CancellationToken ct);
    }
}
