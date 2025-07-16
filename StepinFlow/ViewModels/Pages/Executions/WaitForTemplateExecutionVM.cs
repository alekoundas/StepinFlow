using Business.Interfaces;
using CommunityToolkit.Mvvm.ComponentModel;
using Model.Models;
using StepinFlow.Views.UserControls;
using System.IO;

namespace StepinFlow.ViewModels.Pages.Executions
{
    public partial class WaitForTemplateExecutionVM : ObservableObject, IExecutionViewModel
    {
        public TimeSpanInputUserControl TimeSpanInputUserControl;


        [ObservableProperty]
        private Execution _execution;

        public event ShowResultImageEvent? ShowResultImage;
        public delegate void ShowResultImageEvent(string filePath);

        public WaitForTemplateExecutionVM()
        {
            _execution = new Execution() { FlowStep = new FlowStep() };
        }

        public Task SetExecution(Execution execution)
        {
            Execution = execution;
            TimeSpanInputUserControl.ViewModel.SetFromTotalMilliseconds(Execution.FlowStep.Milliseconds);

            if (execution.ResultImagePath != null)
                if (File.Exists(execution.ResultImagePath))
                    ShowResultImage?.Invoke(execution.ResultImagePath);

            return Task.CompletedTask;
        }
    }
}

