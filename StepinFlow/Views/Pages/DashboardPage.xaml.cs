using CommunityToolkit.Mvvm.Input;
using StepinFlow.ViewModels.Pages;
using System.Text.RegularExpressions;
using System.Windows.Input;
using Wpf.Ui.Abstractions.Controls;
using Wpf.Ui.Controls;

namespace StepinFlow.Views.Pages
{
    public partial class DashboardPage : INavigableView<DashboardVM>
    {
        public DashboardVM ViewModel { get; }

        public DashboardPage(DashboardVM viewModel)
        {
            ViewModel = viewModel;
            DataContext = this;

            InitializeComponent();
        }

        private void NumberValidationTextBox(object sender, TextCompositionEventArgs e)
        {
            Regex regex = new Regex("[^0-9:]+");
            e.Handled = regex.IsMatch(e.Text);
        }
    }
}
