using Core.Enums;
using Core.Models.Database;
using Core.Models.Dtos;

namespace Business.Services.FlowValidationService.Rules
{
    /// <summary>
    /// What will not survive another screen. Warnings rather than errors: the flow executes here,
    /// and somebody should know it will not execute the same way anywhere else.
    /// </summary>
    public static class FlowPortabilityValidator
    {
        public static void Validate(
            IReadOnlyList<FlowStep> authoredSteps,
            IReadOnlyList<FlowArea> areas,
            IReadOnlyList<FlowPoint> points,
            FlowValidationResultDto result)
        {
            Dictionary<int, FlowArea> areasById = areas.ToDictionary(x => x.Id);
            Dictionary<int, FlowPoint> pointsById = points.ToDictionary(x => x.Id);

            foreach (FlowStep step in authoredSteps)
            {
                if (step.FlowAreaId != null && areasById.TryGetValue(step.FlowAreaId.Value, out FlowArea? area) && IsOnScreen(area, areasById))
                {
                    result.Add(step, ValidationSeverityEnum.WARNING, FlowValidationCodeEnum.SCREEN_COORDINATES,
                        $"\"{area.Name}\" is placed in screen coordinates, so this step only works on a screen laid out like this one. Put it inside a window or a monitor.");
                }

                ValidatePoint(result, step, step.FlowPointId, pointsById, areasById);
                ValidatePoint(result, step, step.FlowPointEndId, pointsById, areasById);
            }
        }


        // ================================================================
        // Private methods
        // ================================================================

        private static void ValidatePoint(
            FlowValidationResultDto result,
            FlowStep step,
            int? pointId,
            Dictionary<int, FlowPoint> pointsById,
            Dictionary<int, FlowArea> areasById)
        {
            if (pointId == null || !pointsById.TryGetValue(pointId.Value, out FlowPoint? point))
                return;

            bool isOnScreen = point.FlowAreaId == null;
            if (!isOnScreen && areasById.TryGetValue(point.FlowAreaId!.Value, out FlowArea? area))
                isOnScreen = IsOnScreen(area, areasById);

            if (!isOnScreen)
                return;

            result.Add(step, ValidationSeverityEnum.WARNING, FlowValidationCodeEnum.SCREEN_COORDINATES,
                $"\"{point.Name}\" is a screen coordinate, so this step only works on a screen laid out like this one. Measure it from a window or a monitor.");
        }

        // A region with no parent is wherever it was drawn on this screen, and so is anything inside it.
        private static bool IsOnScreen(FlowArea area, Dictionary<int, FlowArea> areasById)
        {
            if (area.Type != FlowAreaTypeEnum.CUSTOM)
                return false;

            if (area.ParentFlowAreaId == null)
                return true;

            if (!areasById.TryGetValue(area.ParentFlowAreaId.Value, out FlowArea? parent))
                return false;

            return IsOnScreen(parent, areasById);
        }
    }
}
