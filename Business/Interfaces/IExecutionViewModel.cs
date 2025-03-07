using Model.Models;

namespace Business.Interfaces
{
    public interface IExecutionViewModel
    {
        public Task SetExecution(Execution execution);
        //void OnPageExit();
        //Task OnSave();
        //Task OnCancel();
    }
}
