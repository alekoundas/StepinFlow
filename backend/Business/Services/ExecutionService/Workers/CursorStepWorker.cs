using System.Drawing;
using Business.Services.AreaPointService;
using Business.Services.InputService;
using Core.Enums;
using Core.Models.Business;
using Core.Models.Database;

using SharpHook.Data;

namespace Business.Services.ExecutionService.Workers
{
    /// <summary>
    /// Move, click, drag and scroll. 
    /// Click and scroll carry no location of their own. 
    /// </summary>
    public class CursorStepWorker : IStepWorker
    {
        private readonly IInputService _inputService;
        private readonly IAreaPointResolver _areaPointResolver;

        public CursorStepWorker(IInputService inputService, IAreaPointResolver areaPointResolver)
        {
            _inputService = inputService;
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
                    return Scroll(step);

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

            if (!_inputService.MoveCursor(target.Value.X, target.Value.Y))
                return ExecutionStep.Failure("The cursor did not move. The window in front is probably running as administrator.");

            return ExecutionStep.Success(target.Value);
        }

        private ExecutionStep Click(FlowStep step)
        {
            Point at = _inputService.CursorPosition();
            _inputService.SimulateMouseClick(at.X, at.Y, ButtonOf(step.CursorButtonType));

            return ExecutionStep.Success(at);
        }

        private ExecutionStep Scroll(FlowStep step)
        {
            Point at = _inputService.CursorPosition();

            int notches = Math.Max(step.LoopCount, 1);
            int delta = step.CursorScrollDirectionType == CursorScrollDirectionTypeEnum.DOWN ? -notches : notches;

            _inputService.SimulateMouseScroll(at.X, at.Y, delta);

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

            if (!_inputService.MoveCursor(from.Value.X, from.Value.Y))
                return ExecutionStep.Failure("The cursor did not move. The window in front is probably running as administrator.");

            _inputService.SimulateMouseDrag(from.Value.X, from.Value.Y, to.Value.X, to.Value.Y, ButtonOf(step.CursorButtonType));

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

        private static MouseButton ButtonOf(CursorButtonTypeEnum? type)
        {
            switch (type)
            {
                case CursorButtonTypeEnum.RIGHT_BUTTON:
                    return MouseButton.Button2;

                case CursorButtonTypeEnum.MIDDLE_BUTTON:
                    return MouseButton.Button3;

                default:
                    return MouseButton.Button1;
            }
        }
    }
}
