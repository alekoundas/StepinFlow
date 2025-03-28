using Business.Interfaces;
using Microsoft.Xaml.Behaviors;
using System.ComponentModel;
using System.Windows.Controls;

namespace StepinFlow.Behavior
{
    public class ValidateOnProgrammaticalyValueChangeBehavior : Behavior<TextBox>
    {
        private INotifyPropertyChanged _viewModel;
        //private INotifyPropertyChanged _flowParameter;

        public string PropertyPath { get; set; } = "";
        protected override void OnAttached()
        {
            base.OnAttached();
            Unsubscribe();

            if (AssociatedObject.DataContext is IFlowStepDetailPage page && page.ViewModel is INotifyPropertyChanged vm)
            {
                _viewModel = vm;
                _viewModel.PropertyChanged += OnViewModelPropertyChanged;

                //if (vm is IFlowStepDetailVM flowStepVm && flowStepVm.GetFlowStep() is INotifyPropertyChanged flowStep)
                //{
                //    _flowStep = flowStep;
                //    _flowStep.PropertyChanged += OnFlowStepPropertyChanged;
                //}


                //if (vm is IFlowStepDetailVM flowStepVm && flowStepVm.GetFlowStep().FlowParameter is INotifyPropertyChanged flowParameter)
                //{
                //    _flowParameter = flowParameter;
                //    _flowParameter.PropertyChanged += OnFlowStepPropertyChanged;
                //}

            }
            //AssociatedObject.DataContextChanged += OnDataContextChanged;
        }

        protected override void OnDetaching()
        {
            Unsubscribe();
            //AssociatedObject.DataContextChanged -= OnDataContextChanged;
            base.OnDetaching();
        }

        //private void OnDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        //{
        //    UpdateBindings(e.NewValue);
        //}

        private void UpdateBindings(object dataContext)
        {

            //Unsubscribe();

            //if (dataContext is IFlowStepDetailPage page && page.ViewModel is INotifyPropertyChanged vm)
            //{
            //    _viewModel = vm;
            //    _viewModel.PropertyChanged += OnViewModelPropertyChanged;

            //    //if (vm is IFlowStepDetailVM flowStepVm && flowStepVm.GetFlowStep() is INotifyPropertyChanged flowStep)
            //    //{
            //    //    _flowStep = flowStep;
            //    //    _flowStep.PropertyChanged += OnFlowStepPropertyChanged;
            //    //}
            //}




        }

        private void Unsubscribe()
        {
            if (_viewModel != null)
            {
                _viewModel.PropertyChanged -= OnViewModelPropertyChanged;
                _viewModel = null;
            }
            //if (_flowParameter != null)
            //{
            //    _flowParameter.PropertyChanged -= OnFlowStepPropertyChanged;
            //    _flowParameter = null;
            //}
        }

        private void OnViewModelPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            //if (e.PropertyName == "FlowStep")
            //{
            //    // FlowStep object changed, update subscription
            //    if (_viewModel is IFlowStepDetailVM flowStepVm && flowStepVm.GetFlowStep() is INotifyPropertyChanged newFlowStep)
            //    {
            //        if (_flowStep != null)
            //        {
            //            _flowStep.PropertyChanged -= OnFlowStepPropertyChanged;
            //        }

            //        _flowStep = newFlowStep;
            //        _flowStep.PropertyChanged += OnFlowStepPropertyChanged;
            //    }
            //}

            var bindingExpression = AssociatedObject.GetBindingExpression(TextBox.TextProperty);


            // Triger on Save validations.
            if (PropertyPath == "FlowStep.TemplateImage")
            {
                if (bindingExpression != null)
                {
                    AssociatedObject.Text = bindingExpression.DataItem switch
                    {
                        IFlowStepDetailPage page => Convert.ToBase64String(page.ViewModel?.GetFlowStep()?.TemplateImage ?? Array.Empty<byte>()),
                        _ => AssociatedObject.Text
                    };
                    bindingExpression.UpdateSource();
                }
            }
            else if (PropertyPath == "FlowStep.FlowParameter")
            {
                if (bindingExpression != null)
                {
                    AssociatedObject.Text = bindingExpression.DataItem switch
                    {
                        IFlowStepDetailPage page => page.ViewModel?.GetFlowStep()?.FlowParameter?.Id.ToString(),
                        _ => AssociatedObject.Text
                    };
                    bindingExpression.UpdateSource();
                }
            }
            else if (bindingExpression != null)
                bindingExpression.UpdateSource();
        }

        private void OnFlowStepPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (PropertyPath == "FlowStep.FlowParameter")
            {
                var bindingExpression = AssociatedObject.GetBindingExpression(TextBox.TextProperty);
                if (bindingExpression != null)
                {
                    //AssociatedObject.Text = bindingExpression.DataItem switch
                    //{
                    //    IFlowStepDetailPage page => Convert.ToBase64String(page.ViewModel?.GetFlowStep()?.TemplateImage ?? Array.Empty<byte>()),
                    //    _ => AssociatedObject.Text
                    //};
                    bindingExpression.UpdateSource();
                }
            }
        }
    }
}