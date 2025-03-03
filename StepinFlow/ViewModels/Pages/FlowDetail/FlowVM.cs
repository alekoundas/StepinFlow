﻿using Model.Models;
using Business.BaseViewModels;
using Business.Services.Interfaces;

namespace StepinFlow.ViewModels.Pages
{
    public partial class FlowVM : BaseFlowDetailVM
    {
        private readonly IDataService _dataService;
        private readonly FlowsVM _flowsViewModel;


        public FlowVM(FlowsVM flowsViewModel, IDataService dataService) : base(dataService)
        {
            _dataService = dataService;
            _flowsViewModel = flowsViewModel;

        }

        public override async Task OnSave()
        {
            // Edit mode
            if (Flow.Id > 0)
            {
                Flow updateFlow = await _dataService.Flows.FirstAsync(x => x.Id == Flow.Id);
                updateFlow.Name = Flow.Name;
                await _dataService.UpdateAsync(updateFlow);
            }

            /// Add mode
            else
            {
                if (Flow.Name.Length == 0)
                    Flow.Name = "Flow";

                await _dataService.Flows.AddAsync(Flow);
            }


            _flowsViewModel.RefreshData();
        }
    }
}
