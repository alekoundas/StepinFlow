using Business.Helpers;

namespace Business.Factories.FormValidationFactory.Workers
{
    public class HeightFormValidationWorker : IFormValidationWorker
    {
        private string _propertyName = "";
        public void SetPropertyName(string propertyName)
        {
            _propertyName = propertyName;
        }

        public void Validate(object? rawInputValue)
        {
            if (!int.TryParse(rawInputValue?.ToString(), out int input))
                ValidationHelper.AddError(_propertyName, "Height count is required!");
        }
    }
}
