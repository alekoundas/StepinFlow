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
            string? input = value as string;

            // Get singleton form validation factory.
            IFormValidationFactory? formValidationFactory = App.GetService<IFormValidationFactory>();
            if (formValidationFactory == null)
                return new ValidationResult(false, "Cant validate right now :(");

            // Clear previous errors.
            ValidationHelper.ClearErrors(PropertyPath); 

            // Validate input.
            List<string> errors = formValidationFactory.CreateValidator(PropertyPath).Validate(input);
            foreach (var error in errors)
                ValidationHelper.AddError(PropertyPath, error);


            // Check if there are any errors.
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

