using Model.Models;

namespace Business.Interfaces
{
    public interface IFlowDetailVM
    {
        public Task LoadFlowId(int flowId);
        public void LoadNewFlow(Flow newFlow);

        int GetCurrentEntityId();

        void OnPageExit();
        Task OnSave();
        Task OnCancel();
    }
}
