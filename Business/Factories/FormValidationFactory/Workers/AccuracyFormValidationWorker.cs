namespace Business.Factories.FormValidationFactory.Workers
{
    public class AccuracyFormValidationWorker : IFormValidationWorker
    {
        public List<string> Validate(object? rawInputValue)
        {
            List<string> errors = new List<string>();
            if (decimal.TryParse(rawInputValue?.ToString(), out decimal value))
            {
                if (value < 0m || value > 100m)
                    errors.Add("Accuracy must be between 0 and 100");
            }
            else
                errors.Add("Input is not of the expected type Decimal");

            return errors;
        }
    }
}
