using Business.Interfaces;
using CommunityToolkit.Mvvm.ComponentModel;
using Model.Models;
using System.IO;

namespace StepinFlow.ViewModels.Pages.Executions
{
    public partial class WaitForTemplateExecutionVM : ObservableObject, IExecutionViewModel
    {
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

            if (execution.ResultImagePath != null)
                if (File.Exists(execution.ResultImagePath))
                    ShowResultImage?.Invoke(execution.ResultImagePath);

            return Task.CompletedTask;
        }
    }
}

