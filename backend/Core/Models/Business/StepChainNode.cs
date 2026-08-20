using Core.Enums;

namespace Core.Models.Business
{
    /// <summary>
    /// The little of a step that walking the parent chain needs. Exists so the lookup and the
    /// validator can share one rule without the lookup having to load whole entities.
    /// </summary>
    public readonly record struct StepChainNode(
        int Id,
        int? ParentFlowStepId,
        FlowStepTypeEnum FlowStepType,
        string Name);
}
