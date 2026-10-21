using Core.Models.Database;
using Core.Models.Dtos.FlowScript;

namespace Business.FlowScript
{
    public interface IFlowStepScriptLineService
    {
        /// <summary>
        /// What a step's script line would be, for a step still being edited: the area, points,
        /// steps and sub-flow it names are read by id. Nothing is saved.
        /// </summary>
        Task<FlowStepScriptLineDto> WriteAsync(FlowStep step, CancellationToken ct = default);
    }
}
