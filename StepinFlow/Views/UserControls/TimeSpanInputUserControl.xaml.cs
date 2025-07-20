using StepinFlow.ViewModels.UserControls;
using System.Windows;
using System.Windows.Controls;

namespace StepinFlow.Views.UserControls
{
    public partial class TimeSpanInputUserControl : UserControl
    {

        // DependencyProperty for IsEnabled
        public static readonly DependencyProperty IsEnabledProperty =
            DependencyProperty.Register(
                nameof(IsEnabled),              // Property name
                typeof(bool),                    // Property type
                typeof(TreeViewUserControl),     // Owner type
                new PropertyMetadata(false, OnIsEnabledChanged)); // Default value and callback

        public bool IsEnabled
        {
            get { return (bool)GetValue(IsEnabledProperty); }
            set { SetValue(IsEnabledProperty, value); }
        }

        private static void OnIsEnabledChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var control = (TreeViewUserControl)d;
            control.IsEnabled = !(bool)e.NewValue;
        }



  
        public TimeSpanInputUserControlVM ViewModel { get; set; }
        private bool _isUpdatingFromViewModel = false;

        public TimeSpanInputUserControl()
        {
            // Resolve the ViewModel from the DI container.
            TimeSpanInputUserControlVM? viewModel = App.GetService<TimeSpanInputUserControlVM>();

            InitializeComponent();
            ViewModel = viewModel;
            DataContext = new TimeSpanInputUserControlVM();


            if (viewModel == null)
                throw new InvalidOperationException("Failed to resolve TimeSpanInputUserControlVM from DI container.");

            DataContext = this;
        }
    }
}