using Business.Shell;
using Core.Models.Dtos;

namespace Transport.Ipc.Handlers
{
    /// <summary>
    /// Static because it reads a catalogue and nothing else. Nothing is resolved for it and no
    /// instance is built per request; CA1822 is what pointed it out.
    /// </summary>
    public static class GetLookupShellCommandModesHandler
    {
        public static Task<ResultDto<IReadOnlyList<ShellCommandModeDto>>> HandleAsync(CancellationToken ct)
        {
            return Task.FromResult(ResultDto<IReadOnlyList<ShellCommandModeDto>>.Success(ShellCommandModeCatalog.All));
        }
    }
}
