using Business.Services.InputService;
using Core.Models.Dtos;
using Core.Models.Ipc;
using MediatR;

namespace Business.Ipc.Handlers
{
    public class SystemMoveCursorHandler : IRequestHandler<SystemMoveCursorCommand, ResultDto<bool>>
    {
        private readonly IInputService _inputService;

        public SystemMoveCursorHandler(IInputService inputService)
        {
            _inputService = inputService;
        }

        public async Task<ResultDto<bool>> Handle(SystemMoveCursorCommand request, CancellationToken ct)
        {
            bool moved = _inputService.MoveCursor(request.dto.X, request.dto.Y);

            if (!moved)
                return ResultDto<bool>.Failure("Could not move the cursor to the requested point.");

            return ResultDto<bool>.Success(true);
        }
    }
}
