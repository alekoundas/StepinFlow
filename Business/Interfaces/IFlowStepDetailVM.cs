using Model.Models;

namespace Business.Interfaces
{
    public interface IFlowStepDetailVM
    {
        //event Action<int> OnSave;
        FlowStep GetFlowStep();

        Task LoadFlowStepId(int flowStepId);
        Task LoadNewFlowStep(FlowStep newFlowStep);
        int GetCurrentEntityId();
        void OnPageExit();
        Task OnSave();
        Task OnCancel();

    }
}
