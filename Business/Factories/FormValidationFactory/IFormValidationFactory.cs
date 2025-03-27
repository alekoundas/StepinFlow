namespace Business.Factories.FormValidationFactory
{
    public interface IFormValidationFactory
    {
        IFormValidationWorker CreateValidator(string propertyName);
    }
}
