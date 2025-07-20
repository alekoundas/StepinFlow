using Business.Helpers;
using System.Globalization;

namespace Business.Factories.FormValidationFactory.Workers
{
    public class AccuracyFormValidationWorker : IFormValidationWorker
    {
        private string _propertyName = "";
        public void SetPropertyName(string propertyName)
        {
            _propertyName = propertyName;
        }

        public void Validate(object? rawInputValue)
        {
            if (decimal.TryParse(rawInputValue?.ToString(), NumberStyles.Any, CultureInfo.CurrentCulture, out decimal value))
            {
                if (value < 0m || value > 100m)
                    ValidationHelper.AddError(_propertyName, "Accuracy must be between 0 and 100!");
            }
            else
                ValidationHelper.AddError(_propertyName, "Input is not of the expected type Decimal!");
        }
    }
}
