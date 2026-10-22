using Business.AreaPoint;
using Core.Enums;
using Core.Models.Business.AreaPointResolution;
using Core.Models.Dtos;
using Core.Ports;

namespace Transport.Ipc.Handlers
{
    /// <summary>
    /// Moves or drags the real cursor between the step's saved locations, worked out the way an
    /// execution does, so the test shows what the flow will do. A step result exists only while the
    /// flow executes, so a step that reads one is not tested.
    /// </summary>
    public class TestCursorStepHandler
    {
        private const string CURSOR_DID_NOT_MOVE = "The cursor did not move. The window in front is probably running as administrator.";

        private readonly IAreaPointResolver _areaPointResolver;
        private readonly IInputSimulatorService _inputSimulatorService;

        public TestCursorStepHandler(IAreaPointResolver areaPointResolver, IInputSimulatorService inputSimulatorService)
        {
            _areaPointResolver = areaPointResolver;
            _inputSimulatorService = inputSimulatorService;
        }

        public async Task<ResultDto<bool>> HandleAsync(FlowStepDto dto, CancellationToken ct)
        {
            bool isDrag = dto.FlowStepType == FlowStepTypeEnum.CURSOR_DRAG;
            if (dto.FlowStepType != FlowStepTypeEnum.CURSOR_RELOCATE && !isDrag)
                return ResultDto<bool>.Failure("Only a Move or a Drag can be tested.");

            if (dto.FlowPointId == null || (isDrag && dto.FlowPointEndId == null))
                return ResultDto<bool>.Failure("A step result exists only while the flow executes, so only saved locations can be tested.");

            if (isDrag)
                return await DragAsync(dto, ct);

            return await MoveAsync(dto, ct);
        }


        // ================================================================
        // Private methods
        // ================================================================

        private async Task<ResultDto<bool>> MoveAsync(FlowStepDto dto, CancellationToken ct)
        {
            PointResolution target = await _areaPointResolver.ResolvePointAsync(dto.FlowPointId!.Value, ct);
            if (!target.IsResolved)
                return ResultDto<bool>.Failure(target.Error!);

            if (!_inputSimulatorService.MoveCursor(target.Point.X, target.Point.Y))
                return ResultDto<bool>.Failure(CURSOR_DID_NOT_MOVE);

            return ResultDto<bool>.Success(true);
        }

        private async Task<ResultDto<bool>> DragAsync(FlowStepDto dto, CancellationToken ct)
        {
            PointResolution from = await _areaPointResolver.ResolvePointAsync(dto.FlowPointId!.Value, ct);
            if (!from.IsResolved)
                return ResultDto<bool>.Failure(from.Error!);

            PointResolution to = await _areaPointResolver.ResolvePointAsync(dto.FlowPointEndId!.Value, ct);
            if (!to.IsResolved)
                return ResultDto<bool>.Failure(to.Error!);

            if (!_inputSimulatorService.MoveCursor(from.Point.X, from.Point.Y))
                return ResultDto<bool>.Failure(CURSOR_DID_NOT_MOVE);

            CursorButtonTypeEnum button = dto.CursorButtonType ?? CursorButtonTypeEnum.LEFT_BUTTON;
            _inputSimulatorService.SimulateMouseDrag(from.Point.X, from.Point.Y, to.Point.X, to.Point.Y, button);

            return ResultDto<bool>.Success(true);
        }
    }
}
