using Business.Services.CommandService;
using Core.Models.Dtos;
using Core.Models.Ipc;
using MediatR;

namespace Business.Ipc.Handlers
{
    /// <summary>
    /// Runs the step's command for real so the author can see what it returns. There is no way to
    /// preview a command without running it, which is why destructive presets ask first.
    /// </summary>
    public class TestRunCommandHandler : IRequestHandler<TestRunCommandQuery, ResultDto<RunCommandTestResultDto>>
    {
        private readonly ICommandRunner _commandRunner;

        public TestRunCommandHandler(ICommandRunner commandRunner)
        {
            _commandRunner = commandRunner;
        }

        public async Task<ResultDto<RunCommandTestResultDto>> Handle(TestRunCommandQuery request, CancellationToken ct)
        {
            RunCommandTestResultDto result = await _commandRunner.RunAsync(request.dto, ct);
            return ResultDto<RunCommandTestResultDto>.Success(result);
        }
    }
}
