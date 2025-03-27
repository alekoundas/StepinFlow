namespace Business.Factories.FormValidationFactory.Workers
{
    public class ImageFormValidationWorker : IFormValidationWorker
    {
        public List<string> Validate(object? rawInputValue)
        {
            byte[]? input = rawInputValue as byte[];
            List<string> errors = new List<string>();

            // TODO: set correct length size for png image.
            if (input == null || input.Length < 10)
                errors.Add("Input is not of the expected type Byte[]");


            return errors;
        }
    }
}
