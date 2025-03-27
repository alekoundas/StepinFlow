using Business.Factories.FormValidationFactory;
using Business.Helpers;
using System.Globalization;
using System.Windows.Controls;

namespace StepinFlow.Rules
{
    public class FormFieldValidationRule : ValidationRule
    {
        public string PropertyPath { get; set; } = "";

        public override ValidationResult Validate(object value, CultureInfo cultureInfo)
        {

            // Get singleton form validation factory.
            IFormValidationFactory? formValidationFactory = App.GetService<IFormValidationFactory>();
            if (formValidationFactory == null)
                return new ValidationResult(false, "FormValidationFactory instance doesnt exist!");

            // Clear previous errors.
            ValidationHelper.ClearErrors(PropertyPath); 

            // Validate input.
            List<string> errors = formValidationFactory.CreateValidator(PropertyPath).Validate(value);
            foreach (var error in errors)
                ValidationHelper.AddError(PropertyPath, error);


            // Check display errors.
            if (ValidationHelper.HasErrors(PropertyPath))
            {
                string error = String.Join("\n", ValidationHelper.GetErrors(PropertyPath));
                return new ValidationResult(false, error);
            }
            else
                return ValidationResult.ValidResult; // Input is valid
        }
    }
}

