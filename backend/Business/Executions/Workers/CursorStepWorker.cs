using System.Drawing;
using Business.AreaPoint;
using Core.Ports;
using Core.Enums;
using Core.Models.Database;
using Core.Models.Business.AreaPointResolution;


namespace Business.Executions.Workers
{
    /// <summary>
    /// Move, click, drag and scroll. 
    /// Click and scroll carry no location of their own. 
    /// </summary>
    public class CursorStepWorker : IStepWorker
    {
        private readonly IInputSimulatorService _inputSimulatorService;
        private readonly IAreaPointResolver _areaPointResolver;

        public CursorStepWorker(IInputSimulatorService inputSimulatorService, IAreaPointResolver areaPointResolver)
        {
            _inputSimulatorService = inputSimulatorService;
            _areaPointResolver = areaPointResolver;
        }

        public async Task<ExecutionStep> ExecuteAsync(FlowStep step, IExecutionCacheService cache, CancellationToken ct)
        {
            switch (step.FlowStepType)
            {
                case FlowStepTypeEnum.CURSOR_RELOCATE:
                    return await MoveAsync(step, cache, ct);

                case FlowStepTypeEnum.CURSOR_CLICK:
                    return Click(step);

                case FlowStepTypeEnum.CURSOR_SCROLL:
                    return Scroll(step, ct);

                case FlowStepTypeEnum.CURSOR_DRAG:
                    return await DragAsync(step, cache, ct);

                default:
                    return ExecutionStep.Failure($"{step.FlowStepType} is not a cursor step.");
            }
        }


        // ================================================================
        // Private methods
        // ================================================================

        private async Task<ExecutionStep> MoveAsync(FlowStep step, IExecutionCacheService cache, CancellationToken ct)
        {
            Point? target = await ResolvePointAsync(step.FlowPointId, step.FlowStepReferenceId, cache, ct);
            if (target == null)
                return ExecutionStep.Failure("There is nowhere to move to.");

            if (!_inputSimulatorService.MoveCursor(target.Value.X, target.Value.Y))
                return ExecutionStep.Failure("The cursor did not move. The window in front is probably running as administrator.");

            return ExecutionStep.Success(target.Value);
        }

        private ExecutionStep Click(FlowStep step)
        {
            Point at = _inputSimulatorService.CursorPosition();
            CursorButtonTypeEnum button = step.CursorButtonType ?? CursorButtonTypeEnum.LEFT_BUTTON;

            switch (step.CursorButtonActionType)
            {
                case CursorButtonActionTypeEnum.DOUBLE_CLICK:
                    _inputSimulatorService.SimulateMouseDoubleClick(at.X, at.Y, button);
                    break;

                // Held and released are two halves of one gesture, so each is a step of its own and
                // whatever runs between them happens with the button down.
                case CursorButtonActionTypeEnum.HOLD_CLICK:
                    _inputSimulatorService.SimulateMouseDown(at.X, at.Y, button);
                    break;

                case CursorButtonActionTypeEnum.RELEASE_CLICK:
                    _inputSimulatorService.SimulateMouseUp(at.X, at.Y, button);
                    break;

                default:
                    _inputSimulatorService.SimulateMouseClick(at.X, at.Y, button);
                    break;
            }

            return ExecutionStep.Success(at);
        }

        private ExecutionStep Scroll(FlowStep step, CancellationToken ct)
        {
            Point at = _inputSimulatorService.CursorPosition();

            int notches = Math.Max(step.LoopCount, 1);
            CursorScrollDirectionTypeEnum direction = step.CursorScrollDirectionType ?? CursorScrollDirectionTypeEnum.DOWN;

            _inputSimulatorService.SimulateMouseScroll(at.X, at.Y, direction, notches, ct);

            return ExecutionStep.Success(at);
        }

        private async Task<ExecutionStep> DragAsync(FlowStep step, IExecutionCacheService cache, CancellationToken ct)
        {
            Point? from = await ResolvePointAsync(step.FlowPointId, step.FlowStepReferenceId, cache, ct);
            if (from == null)
                return ExecutionStep.Failure("There is nowhere to drag from.");

            Point? to = await ResolvePointAsync(step.FlowPointEndId, step.FlowStepReferenceEndId, cache, ct);
            if (to == null)
                return ExecutionStep.Failure("There is nowhere to drag to.");

            if (!_inputSimulatorService.MoveCursor(from.Value.X, from.Value.Y))
                return ExecutionStep.Failure("The cursor did not move. The window in front is probably running as administrator.");

            _inputSimulatorService.SimulateMouseDrag(from.Value.X, from.Value.Y, to.Value.X, to.Value.Y, step.CursorButtonType ?? CursorButtonTypeEnum.LEFT_BUTTON);

            return ExecutionStep.Success(to.Value);
        }

        // A saved point, or the result of an earlier search.
        private async Task<Point?> ResolvePointAsync(int? flowPointId, int? flowStepReferenceId, IExecutionCacheService cache, CancellationToken ct)
        {
            if (flowPointId == null)
                return cache.GetStepLocationFrom(flowStepReferenceId);

            PointResolution resolution = await _areaPointResolver.ResolvePointAsync(flowPointId.Value, ct);
            if (!resolution.IsResolved)
                return null;

            return resolution.Point;
        }
    }
}
