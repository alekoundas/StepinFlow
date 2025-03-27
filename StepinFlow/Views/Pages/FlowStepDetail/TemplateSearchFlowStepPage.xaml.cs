using Business.Interfaces;
using StepinFlow.ViewModels.Pages;
using System.Windows.Controls;
using System.Windows.Data;

namespace StepinFlow.Views.Pages.FlowStepDetail
{
    public partial class TemplateSearchFlowStepPage : Page, IFlowStepDetailPage, IDetailPage
    {
        public IFlowDetailVM? FlowViewModel { get; set; }
        public IFlowStepDetailVM? FlowStepViewModel { get; set; }
        public IFlowParameterDetailVM? FlowParameterViewModel { get; set; }
        public IExecutionViewModel? FlowExecutionViewModel { get; set; }


        public IFlowStepDetailVM ViewModel { get; set; }

        public TemplateSearchFlowStepPage(TemplateSearchFlowStepVM viewModel)
        {
            ViewModel = viewModel;
            FlowStepViewModel = ViewModel;
            DataContext = this;
            InitializeComponent();

            this.DataContextChanged += (s, e) =>
            {
                if (e.NewValue is TemplateSearchFlowStepVM viewModel)
                {
                    this.BindingGroup = new BindingGroup(); // Assign BindingGroup
                }
            };
        }

        private void TextBox_Error(object sender, ValidationErrorEventArgs e)
        {
            if (sender is TextBox textBox)
            {
                string propertyName = textBox.GetBindingExpression(TextBox.TextProperty)?.ResolvedSourcePropertyName ?? "";

                if (!string.IsNullOrEmpty(propertyName))
                {
                    if (e.Action == ValidationErrorEventAction.Added)
                    {
                        //// Add error message to ViewModel's ValidationErrors dictionary
                        //if (!viewModel.ValidationErrors.ContainsKey(propertyName))
                        //{
                        //    viewModel.ValidationErrors[propertyName] = new List<string>();
                        //}
                        //viewModel.ValidationErrors[propertyName].Add(e.Error.ErrorContent.ToString() ?? "Invalid input");
                    }
                    else if (e.Action == ValidationErrorEventAction.Removed)
                    {
                        // Remove error message from ViewModel's ValidationErrors dictionary
                        //if (viewModel.ValidationErrors.ContainsKey(propertyName))
                        //{
                        //    viewModel.ValidationErrors[propertyName].Remove(e.Error.ErrorContent.ToString() ?? "");
                        //    if (viewModel.ValidationErrors[propertyName].Count == 0)
                        //    {
                        //        viewModel.ValidationErrors.Remove(propertyName);
                        //    }
                        //}
                    }

                    // Notify UI that errors have changed
                    //viewModel.OnErrorsChanged(propertyName);
                }
            }
        }
    }
}
