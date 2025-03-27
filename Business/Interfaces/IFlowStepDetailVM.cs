using Model.Models;

namespace Business.Interfaces
{
    public interface IFlowStepDetailVM
    {
        //event Action<int> OnSave;
        FlowStep GetFlowStep();
        bool IsFormValid();

        Task LoadFlowStepId(int flowStepId);
        Task LoadNewFlowStep(FlowStep newFlowStep);
        int GetCurrentEntityId();
        void OnPageExit();
        Task<int> OnSave();
        Task OnCancel();

    }
}
