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
            switch (propertyName)
            {
                case "FlowStep.Accuracy":
                    return _serviceProvider.GetRequiredService<AccuracyFormValidationWorker>();
                case "FlowStep.TemplateImage":
                    return _serviceProvider.GetRequiredService<ImageFormValidationWorker>();
                case "FlowStep.FlowParameter":
                    return _serviceProvider.GetRequiredService<FlowParameterFormValidationWorker>();
                default:
                    throw new ArgumentException($"No validator defined for {propertyName}.");
            }
        }
    }
}
