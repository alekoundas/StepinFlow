using System.Collections;
using System.Globalization;
using System.Windows.Data;

namespace StepinFlow.Converters
{
    public class GroupedCollectionConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            var result = new List<object>();

            foreach (var value in values)
            {
                // If the value is a collection, add all items from the collection to the result
                if (value is IEnumerable enumerable)
                    foreach (var item in enumerable)
                        result.Add(item);

                // If the value is a single item (and not null), add it to the result
                else if (value != null)
                    result.Add(value);
            }

            return result;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}