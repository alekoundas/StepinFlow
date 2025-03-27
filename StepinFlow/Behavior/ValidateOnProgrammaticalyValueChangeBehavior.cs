using Business.Interfaces;
using Microsoft.Xaml.Behaviors;
using StepinFlow.Views.Pages.FlowStepDetail;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;

namespace StepinFlow.Behavior
{
    public class ValidateOnProgrammaticalyValueChangeBehavior : Behavior<TextBox>
    {
        private INotifyPropertyChanged _viewModel;
        private INotifyPropertyChanged _flowStep;

        protected override void OnAttached()
        {
            base.OnAttached();
            UpdateBindings(AssociatedObject.DataContext);
            AssociatedObject.DataContextChanged += OnDataContextChanged;
        }

        protected override void OnDetaching()
        {
            Unsubscribe();
            AssociatedObject.DataContextChanged -= OnDataContextChanged;
            base.OnDetaching();
        }

        private void OnDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            UpdateBindings(e.NewValue);
        }

        private void UpdateBindings(object dataContext)
        {
            Unsubscribe();

            if (dataContext is TemplateSearchFlowStepPage page && page.ViewModel is INotifyPropertyChanged vm)
            {
                _viewModel = vm;
                _viewModel.PropertyChanged += OnViewModelPropertyChanged;

                if (vm is IFlowStepDetailVM flowStepVm && flowStepVm.GetFlowStep() is INotifyPropertyChanged flowStep)
                {
                    _flowStep = flowStep;
                    _flowStep.PropertyChanged += OnFlowStepPropertyChanged;
                }
            }
        }

        private void Unsubscribe()
        {
            if (_viewModel != null)
            {
                _viewModel.PropertyChanged -= OnViewModelPropertyChanged;
                _viewModel = null;
            }
            if (_flowStep != null)
            {
                _flowStep.PropertyChanged -= OnFlowStepPropertyChanged;
                _flowStep = null;
            }
        }

        private void OnViewModelPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "FlowStep")
            {
                // FlowStep object changed, update subscription
                if (_viewModel is IFlowStepDetailVM flowStepVm && flowStepVm.GetFlowStep() is INotifyPropertyChanged newFlowStep)
                {
                    if (_flowStep != null)
                    {
                        _flowStep.PropertyChanged -= OnFlowStepPropertyChanged;
                    }

                    _flowStep = newFlowStep;
                    _flowStep.PropertyChanged += OnFlowStepPropertyChanged;
                }
            }
        }

        private void OnFlowStepPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "TemplateImage")
            {
                var bindingExpression = AssociatedObject.GetBindingExpression(TextBox.TextProperty);
                if (bindingExpression != null)
                {
                    AssociatedObject.Text = bindingExpression.DataItem switch
                    {
                        TemplateSearchFlowStepPage page => Convert.ToBase64String(page.ViewModel?.GetFlowStep()?.TemplateImage ?? Array.Empty<byte>()),
                        _ => AssociatedObject.Text
                    };
                    bindingExpression.UpdateSource();
                }
            }
        }
    }
}