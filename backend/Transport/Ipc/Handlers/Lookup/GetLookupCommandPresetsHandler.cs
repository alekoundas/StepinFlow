using Business.Services.CommandService;
using Core.Models.Dtos;
using Transport.Messages;
using MediatR;

namespace Transport.Ipc.Handlers
{
    public class GetLookupCommandPresetsHandler
        : IRequestHandler<GetLookupCommandPresetsQuery, ResultDto<IReadOnlyList<CommandPresetDto>>>
    {
        public async Task<ResultDto<IReadOnlyList<CommandPresetDto>>> Handle(
            GetLookupCommandPresetsQuery request, CancellationToken ct) =>
            ResultDto<IReadOnlyList<CommandPresetDto>>.Success(CommandPresetCatalog.All);
    }
}
