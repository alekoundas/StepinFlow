using Business.Helpers;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Text.RegularExpressions;
using System.Windows.Input;

namespace StepinFlow.ViewModels.Pages
{
    public partial class DashboardVM : ObservableObject
    {
        [ObservableProperty]
        private int _counter = 0;

        [ObservableProperty]
        private List<string> _processList = SystemProcessHelper.GetProcessWindowTitles();

        [ObservableProperty]
        private List<string> _flowsList = SystemProcessHelper.GetProcessWindowTitles();

        [RelayCommand]
        private void OnCounterIncrement()
        {
            Counter++;
        }

       

    }
}
