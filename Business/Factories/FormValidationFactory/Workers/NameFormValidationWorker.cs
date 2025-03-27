namespace Business.Factories.FormValidationFactory.Workers
{
    public class NameFormValidationWorker : IFormValidationWorker
    {
        public List<string> Validate(object? rawInputValue)
        {
            string? input = rawInputValue as string;
            List<string> errors = new List<string>();
            if (input?.Length == 0)
                errors.Add("Name cannot be empty!");

            return errors;
        }
    }
}
