using System.Windows.Controls;

namespace Business.Helpers
{
    public static class ValidationHelper
    {
        private static readonly Dictionary<string, List<string>> _validationErrors = new Dictionary<string, List<string>>();

        // Event to notify when errors change
        public static event EventHandler<string> ErrorsChanged;

        public static bool HasErrors(string? propertyPath = null)
        {
            if (propertyPath != null)
                return _validationErrors.Any(x => x.Key == propertyPath);
            else
                return _validationErrors.Any();
        }


        public static void ClearErrors(string? propertyPath = null)
        {
            if (propertyPath != null && _validationErrors.ContainsKey(propertyPath))
                _validationErrors[propertyPath].Clear();
            else
                _validationErrors.Clear();

            ErrorsChanged?.Invoke(null, propertyPath);
        }

        public static void AddError(string propertyPath, string error)
        {
            if (_validationErrors.ContainsKey(propertyPath))
                _validationErrors[propertyPath].Add(error);
            else
                _validationErrors.Add(propertyPath, new List<string> { error });

            ErrorsChanged?.Invoke(null, propertyPath);
        }

        public static Dictionary<string, string> GetAllErrors()
        {
            return _validationErrors.ToDictionary(x=> x.Key,x => string.Join("\n", x.Value));
        }

        public static string GetError(string propertyPath)
        {
            _validationErrors.TryGetValue(propertyPath, out List<string> errors);
            return string.Join("\n", errors);
        }
    }
}