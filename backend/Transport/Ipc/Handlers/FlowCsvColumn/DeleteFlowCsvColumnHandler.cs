using Business.Flows.DataService;
using Core.Models.Dtos;

namespace Transport.Ipc.Handlers
{
    public class DeleteFlowCsvColumnHandler
    {
        private readonly DataService _dataService;

        public DeleteFlowCsvColumnHandler(DataService dataService)
        {
            _dataService = dataService;
        }

        public Task<ResultDto<bool>> HandleAsync(int id, CancellationToken ct)
        {
            return _dataService.FlowCsvColumn.DeleteAsync(id, ct);
        }
    }
}
