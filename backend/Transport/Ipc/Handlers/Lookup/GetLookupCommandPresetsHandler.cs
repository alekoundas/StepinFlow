using Business.Services.CommandService;
using Core.Models.Dtos;
using Core.Models.Ipc;
using MediatR;

namespace Business.Ipc.Handlers
{
    public class GetLookupCommandPresetsHandler
        : IRequestHandler<GetLookupCommandPresetsQuery, ResultDto<IReadOnlyList<CommandPresetDto>>>
    {
        public async Task<ResultDto<IReadOnlyList<CommandPresetDto>>> Handle(
            GetLookupCommandPresetsQuery request, CancellationToken ct) =>
            ResultDto<IReadOnlyList<CommandPresetDto>>.Success(CommandPresetCatalog.All);
    }
}
