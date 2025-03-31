using Business.Helpers;
using System.Globalization;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace Business.Factories.FormValidationFactory.Workers
{
    public class ImageFormValidationWorker : IFormValidationWorker
    {
        private string _propertyName = "";
        public void SetPropertyName(string propertyName)
        {
            _propertyName = propertyName;
        }

        public void Validate(object? rawInputValue)
        {
            byte[]? input = rawInputValue as byte[];
            if (input == null || input.Length < 10)
                ValidationHelper.AddError(_propertyName, "Input is not of the expected type Byte[]!");
        }
    }
}
