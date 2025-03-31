//using Business.Helpers;
//using System.Globalization;
//using System.Windows;
//using System.Windows.Data;

//namespace StepinFlow.Converters
//{
//    public class ValidationErrorConverter : IValueConverter
//    {
//        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
//        {
//            // The parameter will be the property path, like "FlowStep.Name".
//            string propertyPath = parameter as string;

//            if (!string.IsNullOrEmpty(propertyPath))
//                return ValidationHelper.GetError(propertyPath);

//            // Return empty string if no property path is provided.
//            return string.Empty;
//        }

//        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
//        {
//            // We don’t need two-way binding for displaying errors
//            throw new NotImplementedException();
//        }
//    }
//}
