using Business.Factories.FormValidationFactory.Workers;
using Microsoft.Extensions.DependencyInjection;

namespace Business.Factories.FormValidationFactory
{
    public class FormValidationFactory : IFormValidationFactory
    {
        private readonly IServiceProvider _serviceProvider;

        public FormValidationFactory(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        public IFormValidationWorker CreateValidator(string propertyName)
        {
            IFormValidationWorker worker;
            switch (propertyName)
            {
                case "FlowStep.Name":
                    worker = _serviceProvider.GetRequiredService<NameFormValidationWorker>();
                    worker.SetPropertyName(propertyName);
                    break;

                case "FlowStep.Accuracy":
                    worker = _serviceProvider.GetRequiredService<AccuracyFormValidationWorker>();
                    worker.SetPropertyName(propertyName);
                    break;

                case "FlowStep.TemplateImage":
                    worker = _serviceProvider.GetRequiredService<ImageFormValidationWorker>();
                    worker.SetPropertyName(propertyName);
                    break;

                case "FlowStep.FlowParameter":
                    worker = _serviceProvider.GetRequiredService<FlowParameterFormValidationWorker>();
                    worker.SetPropertyName(propertyName);
                    break;

                case "FlowStep.TemplateMatchMode":
                    worker = _serviceProvider.GetRequiredService<TemplateMatchModeFormValidationWorker>();
                    worker.SetPropertyName(propertyName);
                    break;

                case "FlowStep.CursorAction":
                    worker = _serviceProvider.GetRequiredService<CursorActionFormValidationWorker>();
                    worker.SetPropertyName(propertyName);
                    break;

                case "FlowStep.CursorButton":
                    worker = _serviceProvider.GetRequiredService<CursorButtonFormValidationWorker>();
                    worker.SetPropertyName(propertyName);
                    break;

                case "FlowStep.CursorRelocationType":
                    worker = _serviceProvider.GetRequiredService<CursorRelocationTypeFormValidationWorker>();
                    worker.SetPropertyName(propertyName);
                    break;

                case "FlowStep.ParentTemplateSearchFlowStep":
                    worker = _serviceProvider.GetRequiredService<ParentTemplateSearchFlowStepFormValidationWorker>();
                    worker.SetPropertyName(propertyName);
                    break;

                case "FlowStep.LocationX":
                    worker = _serviceProvider.GetRequiredService<LocationFormValidationWorker>();
                    worker.SetPropertyName(propertyName);
                    break;

                case "FlowStep.LocationY":
                    worker = _serviceProvider.GetRequiredService<LocationFormValidationWorker>();
                    worker.SetPropertyName(propertyName);
                    break;

                case "FlowStep.CursorScrollDirection":
                    worker = _serviceProvider.GetRequiredService<CursorScrollDirectionFormValidationWorker>();
                    worker.SetPropertyName(propertyName);
                    break;

                case "FlowStep.LoopCount":
                    worker = _serviceProvider.GetRequiredService<LoopCountFormValidationWorker>();
                    worker.SetPropertyName(propertyName);
                    break;

                case "FlowStep.LoopMaxCount":
                    worker = _serviceProvider.GetRequiredService<LoopCountFormValidationWorker>();
                    worker.SetPropertyName(propertyName);
                    break;

                default:
                    throw new ArgumentException($"No validator defined for {propertyName}.");
            }

            return worker;
        }
    }
}
