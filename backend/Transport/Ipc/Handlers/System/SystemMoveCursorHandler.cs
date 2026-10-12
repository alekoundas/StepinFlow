using Core.Ports;
using Core.Models.Dtos;

namespace Transport.Ipc.Handlers
{
    public class SystemMoveCursorHandler
    {
        private readonly IInputSimulatorService _inputSimulatorService;

        public SystemMoveCursorHandler(IInputSimulatorService inputSimulatorService)
        {
            _inputSimulatorService = inputSimulatorService;
        }

        public async Task<ResultDto<bool>> HandleAsync(ScreenPointDto dto, CancellationToken ct)
        {
            bool moved = _inputSimulatorService.MoveCursor(dto.X, dto.Y);

            if (!moved)
                return ResultDto<bool>.Failure("Could not move the cursor to the requested point.");

            return ResultDto<bool>.Success(true);
        }
    }
}
