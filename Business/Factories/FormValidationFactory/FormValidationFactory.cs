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

                default:
                    throw new ArgumentException($"No validator defined for {propertyName}.");
            }

            return worker;
        }
    }
}
