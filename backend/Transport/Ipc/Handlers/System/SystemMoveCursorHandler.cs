using Core.Ports;
using Core.Models.Dtos;

namespace Transport.Ipc.Handlers
{
    public class SystemMoveCursorHandler
    {
        private readonly IInputService _inputService;

        public SystemMoveCursorHandler(IInputService inputService)
        {
            _inputService = inputService;
        }

        public async Task<ResultDto<bool>> HandleAsync(ScreenPointDto dto, CancellationToken ct)
        {
            bool moved = _inputService.MoveCursor(dto.X, dto.Y);

            if (!moved)
                return ResultDto<bool>.Failure("Could not move the cursor to the requested point.");

            return ResultDto<bool>.Success(true);
        }
    }
}
