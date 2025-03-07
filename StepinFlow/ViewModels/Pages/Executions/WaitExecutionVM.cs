using CommunityToolkit.Mvvm.ComponentModel;
using Model.Models;
using Business.Interfaces;
using System.Windows.Threading;

namespace StepinFlow.ViewModels.Pages.Executions
{
    public partial class WaitExecutionVM : ObservableObject, IExecutionViewModel
    {
        [ObservableProperty]
        private Execution _execution;

        [ObservableProperty]
        private string _timeLeft = "";

        [ObservableProperty]
        private string _timeTotal = "";

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

                int miliseconds = 0;
                miliseconds += Execution.FlowStep.WaitForMilliseconds;
                miliseconds += Execution.FlowStep.WaitForSeconds * 1000;
                miliseconds += Execution.FlowStep.WaitForMinutes * 60 * 1000;
                miliseconds += Execution.FlowStep.WaitForHours * 60 * 60 * 1000;

                TimeTotal = TimeSpan.FromMilliseconds(miliseconds).ToString(@"hh\:mm\:ss");



                // Update every second
                _timeElapsed = TimeSpan.FromMilliseconds(miliseconds);

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
