namespace Business.Factories.FormValidationFactory
{
    public interface IFormValidationWorker
    {
        void Validate(object? rawInputValue);
        void SetPropertyName(string propertyName);
    }
}
