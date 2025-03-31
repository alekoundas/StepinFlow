using Business.Helpers;

namespace Business.Factories.FormValidationFactory.Workers
{
    public class CursorScrollDirectionFormValidationWorker : IFormValidationWorker
    {
        private string _propertyName = "";
        public void SetPropertyName(string propertyName)
        {
            _propertyName = propertyName;
        }

        public void Validate(object? rawInputValue)
        {
            if (rawInputValue == null || rawInputValue == "")
                    ValidationHelper.AddError(_propertyName, "Cursor scroll direction is required!");
        }
    }
}
