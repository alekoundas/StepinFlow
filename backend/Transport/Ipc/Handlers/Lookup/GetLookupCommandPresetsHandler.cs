using Business.Command;
using Core.Models.Dtos;

namespace Transport.Ipc.Handlers
{
    /// <summary>
    /// Static because it reads a catalogue and nothing else. Nothing is resolved for it and no
    /// instance is built per request; CA1822 is what pointed it out.
    /// </summary>
    public static class GetLookupCommandPresetsHandler
    {
        public static Task<ResultDto<IReadOnlyList<CommandPresetDto>>> HandleAsync(CancellationToken ct)
        {
            return Task.FromResult(ResultDto<IReadOnlyList<CommandPresetDto>>.Success(CommandPresetCatalog.All));
        }
    }
}
