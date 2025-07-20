using Model.Models;
using StepinFlow.ViewModels.UserControls;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace StepinFlow.Views.UserControls
{
    public partial class TimeSpanInputUserControl : UserControl
    {

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



        //public TreeViewUserControlVM ViewModel { get; set; }

        //public static readonly DependencyProperty TotalMillisecondsProperty =
        //           DependencyProperty.Register(nameof(TotalMilliseconds), typeof(double), typeof(TimeSpanInputUserControl),
        //               new FrameworkPropertyMetadata(0.0, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnTotalMillisecondsChanged));

        //public static readonly DependencyProperty HoursProperty =
        //    DependencyProperty.Register(nameof(Hours), typeof(int), typeof(TimeSpanInputUserControl),
        //        new FrameworkPropertyMetadata(0, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnHoursChanged));

        //public static readonly DependencyProperty MinutesProperty =
        //    DependencyProperty.Register(nameof(Minutes), typeof(int), typeof(TimeSpanInputUserControl),
        //        new FrameworkPropertyMetadata(0, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnMinutesChanged));

        //public static readonly DependencyProperty SecondsProperty =
        //    DependencyProperty.Register(nameof(Seconds), typeof(int), typeof(TimeSpanInputUserControl),
        //        new FrameworkPropertyMetadata(0, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnSecondsChanged));

        //public static readonly DependencyProperty MillisecondsProperty =
        //    DependencyProperty.Register(nameof(Milliseconds), typeof(int), typeof(TimeSpanInputUserControl),
        //        new FrameworkPropertyMetadata(0, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnMillisecondsChanged));

        //public double TotalMilliseconds
        //{
        //    get => (double)GetValue(TotalMillisecondsProperty);
        //    set => SetValue(TotalMillisecondsProperty, value);
        //}

        //public int Hours
        //{
        //    get => (int)GetValue(HoursProperty);
        //    set => SetValue(HoursProperty, value);
        //}

        //public int Minutes
        //{
        //    get => (int)GetValue(MinutesProperty);
        //    set => SetValue(MinutesProperty, value);
        //}

        //public int Seconds
        //{
        //    get => (int)GetValue(SecondsProperty);
        //    set => SetValue(SecondsProperty, value);
        //}

        //public int Milliseconds
        //{
        //    get => (int)GetValue(MillisecondsProperty);
        //    set => SetValue(MillisecondsProperty, value);
        //}

        public TimeSpanInputUserControlVM ViewModel { get; set; }
        //private TimeSpanInputUserControlVM ViewModel => DataContext as TimeSpanInputUserControlVM;
        private bool _isUpdatingFromViewModel = false;

        //public TimeSpanInputUserControl()
        //{
        //    // Resolve the ViewModel from the DI container.
        //    TimeSpanInputUserControlVM? viewModel = App.GetService<TimeSpanInputUserControlVM>();

        //    if (viewModel == null)
        //        throw new InvalidOperationException("Failed to resolve TimeSpanInputUserControlVM from DI container.");

        //    DataContext = this;
        //    viewModel.OnSelectedFlowStepIdChangedEvent += OnSelectedFlowStepIdChangedEvent;
        //    viewModel.OnSelectedFlowIdChangedEvent += OnSelectedFlowIdChangedEvent;
        //    viewModel.OnSelectedFlowParameterIdChangedEvent += OnSelectedFlowParameterIdChangedEvent;
        //    viewModel.OnFlowStepCloneEvent += OnFlowStepCloneEvent;
        //    viewModel.OnAddFlowStepClickEvent += OnAddFlowStepClickEvent;
        //    viewModel.OnAddFlowParameterClickEvent += OnAddFlowParameterClickEvent;





        //    //InitializeComponent();
        //    //DataContext = new TimeSpanInputViewModel();

        //    // Subscribe to ViewModel property changes
        //    if (ViewModel != null)
        //    {
        //        ViewModel.PropertyChanged += ViewModel_PropertyChanged;
        //    }

        //    // Add input validation event handlers
        //    HoursTextBox.PreviewTextInput += NumericValidation;
        //    MinutesTextBox.PreviewTextInput += NumericValidation;
        //    SecondsTextBox.PreviewTextInput += NumericValidation;
        //    MillisecondsTextBox.PreviewTextInput += NumericValidation;

        //    // Handle paste events
        //    HoursTextBox.PreviewKeyDown += HandlePaste;
        //    MinutesTextBox.PreviewKeyDown += HandlePaste;
        //    SecondsTextBox.PreviewKeyDown += HandlePaste;
        //    MillisecondsTextBox.PreviewKeyDown += HandlePaste;




        //    ViewModel = viewModel;
        //    InitializeComponent();
        //}

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

            // Subscribe to ViewModel property changes
            //if (ViewModel != null)
            //{
            //    ViewModel.PropertyChanged += ViewModel_PropertyChanged;
            //}

            // Add input validation event handlers
            //HoursTextBox.PreviewTextInput += NumericValidation;
            //MinutesTextBox.PreviewTextInput += NumericValidation;
            //SecondsTextBox.PreviewTextInput += NumericValidation;
            //MillisecondsTextBox.PreviewTextInput += NumericValidation;

            //// Handle paste events
            //HoursTextBox.PreviewKeyDown += HandlePaste;
            //MinutesTextBox.PreviewKeyDown += HandlePaste;
            //SecondsTextBox.PreviewKeyDown += HandlePaste;
            //MillisecondsTextBox.PreviewKeyDown += HandlePaste;
        }




        //private void NumericValidation(object sender, TextCompositionEventArgs e)
        //{
        //    // Only allow numeric input
        //    Regex regex = new Regex("[^0-9]+");
        //    e.Handled = regex.IsMatch(e.Text);
        //}

        //private void HandlePaste(object sender, KeyEventArgs e)
        //{
        //    if (e.Key == Key.V && (Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control)
        //    {
        //        // Handle paste - ensure only numeric content
        //        string clipboardText = Clipboard.GetText();
        //        if (!string.IsNullOrEmpty(clipboardText))
        //        {
        //            Regex regex = new Regex("[^0-9]+");
        //            if (regex.IsMatch(clipboardText))
        //            {
        //                e.Handled = true;
        //            }
        //        }
        //    }
        //}

    }

}