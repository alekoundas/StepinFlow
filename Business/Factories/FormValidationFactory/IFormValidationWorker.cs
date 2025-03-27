namespace Business.Factories.FormValidationFactory
{
    public interface IFormValidationWorker
    {
        List<string> Validate(object? rawInputValue);
    }
}
