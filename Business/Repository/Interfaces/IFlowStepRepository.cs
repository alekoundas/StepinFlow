using Business.Repository.Entities;
using Model.Enums;
using Model.Models;
using System.Linq.Expressions;

namespace Business.Repository.Interfaces
{
    public interface IFlowStepRepository : IBaseRepository<FlowStep>
    {
        Task<FlowStep> GetIsNewSibling(int flowStepId);
        Task<List<FlowStep>> GetSiblings(int flowStepId);
        Task<FlowStep?> GetNextSibling(int flowStepId);
        Task<FlowStep?> GetNextChild(int flowStepId, ExecutionResultEnum? resultEnum);
        Task<FlowStep?> LoadAllClone(int id);
        Task<FlowStep> LoadAllExpandedChildren(FlowStep flowStep);
    }
}
