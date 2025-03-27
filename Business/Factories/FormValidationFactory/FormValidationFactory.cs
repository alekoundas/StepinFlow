using Business.Factories.FormValidationFactory.Workers;
using Model.Models;
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
            if (propertyName == "FlowStep.Accuracy")
                return (IFormValidationWorker)_serviceProvider.GetRequiredService<AccuracyFormValidationWorker>();


            throw new ArgumentException($"No validator defined for {propertyName}.");
        }
    }
}
