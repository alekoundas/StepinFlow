using Business.Helpers;

namespace Business.Factories.FormValidationFactory.Workers
{
    public class NameFormValidationWorker : IFormValidationWorker
    {
        private string _propertyName = "";
        public void SetPropertyName(string propertyName)
        {
            _propertyName = propertyName;
        }

        public void Validate(object? rawInputValue)
        {
            string? input = rawInputValue as string;
            if (input?.Length == 0)
                ValidationHelper.AddError(_propertyName, "Name cannot be empty!");
        }
    }
}
