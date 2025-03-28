namespace Business.Factories.FormValidationFactory.Workers
{
    public class TemplateMatchModeFormValidationWorker : IFormValidationWorker
    {
        public List<string> Validate(object? rawInputValue)
        {
            List<string> errors = new List<string>();

            if (rawInputValue == null || rawInputValue == "")
                errors.Add("Template match mode is required!");

            return errors;
        }
    }
}
