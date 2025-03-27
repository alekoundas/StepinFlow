namespace Business.Helpers
{
    public static class ValidationHelper
    {
        private static readonly Dictionary<string, List<string>> _validationErrors = new Dictionary<string, List<string>>();

        public static void ClearErrors(string? propertyPath = null)
        {
            if (propertyPath != null && _validationErrors.Any(x => x.Key == propertyPath))
                _validationErrors[propertyPath].Clear();
            else
                _validationErrors.Clear();
        }

        public static bool HasErrors(string? propertyPath = null)
        {
            if (propertyPath != null)
                return _validationErrors.Any(x => x.Key == propertyPath);
            else
                return _validationErrors.Any();
        }

        public static void AddError(string propertyPath, string error)
        {
            if (_validationErrors.Any(x => x.Key == propertyPath))
                _validationErrors[propertyPath].Add(error);
            else
                _validationErrors.Add(propertyPath, new List<string> { error });
        }

        public static List<string> GetErrors(string propertyPath)
        {
            if (_validationErrors.Any(x => x.Key == propertyPath))
                return _validationErrors[propertyPath];

            return new List<string>();
        }
    }
}