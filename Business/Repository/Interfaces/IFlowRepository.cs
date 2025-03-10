﻿using Model.Models;

namespace Business.Repository.Interfaces
{
    public interface IFlowRepository : IBaseRepository<Flow>
    {
        Task<FlowStep> GetIsNewSibling(int id);
        Task<List<Flow>> LoadAllExpanded();
        Task<List<Flow>> LoadAllCollapsed();
        Task<List<Flow>> LoadAllExport(int? flowId = null);
        Task FixOneToOneRelationIds(int flowId);

    }
}
