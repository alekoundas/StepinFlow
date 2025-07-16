using Business.Interfaces;
using CommunityToolkit.Mvvm.ComponentModel;
using Model.Models;
using StepinFlow.Views.UserControls;
using System.Windows.Threading;

namespace StepinFlow.ViewModels.Pages.Executions
{
    public partial class WaitExecutionVM : ObservableObject, IExecutionViewModel
    {
        public TimeSpanInputUserControl TimeSpanInputUserControl;


        [ObservableProperty]
        private Execution _execution;

        [ObservableProperty]
        private string _timeLeft = "";


        private readonly DispatcherTimer _timer;
        private TimeSpan _timeElapsed = new TimeSpan();

        public WaitExecutionVM()
        {
            _execution = new Execution();

            // Update every second
            _timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
        }

        public Task SetExecution(Execution execution)
        {
            Execution = execution;

            if (Execution.FlowStep != null)
            {
                TimeSpanInputUserControl.ViewModel.SetFromTotalMilliseconds(Execution.FlowStep.Milliseconds);

                // Update every second
                _timeElapsed = TimeSpan.FromMilliseconds(Execution.FlowStep.Milliseconds);

                void UpdateTimer(object sender, EventArgs e)
                {
                    _timeElapsed = _timeElapsed.Subtract(TimeSpan.FromSeconds(1));
                    TimeLeft = _timeElapsed.ToString(@"hh\:mm\:ss");
                }

                _timer.Tick += UpdateTimer;
                _timer.Start();
            }
            return Task.CompletedTask;
        }
    }
}
