using System;
using System.Globalization;
using Xamarin.Forms;

namespace BabbyJotz {
    public class NumberConverter : IValueConverter {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture) {
            if (value is decimal && targetType == typeof(string)) {
                if ((decimal)value == 0.0m) {
                    return "";
                }
                return String.Format("{0}", value);
            }
            if (value is double && targetType == typeof(string)) {
                if ((double)value == 0.0) {
                    return "";
                }
                return String.Format("{0}", value);
            }
            throw new InvalidOperationException();
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) {
            if (value is string && targetType == typeof(decimal)) {
                var result = 0.0m;
                Decimal.TryParse((string)value, out result);
                return result;
            }
            if (value is string && targetType == typeof(double)) {
                var result = 0.0;
                Double.TryParse((string)value, out result);
                return result;
            }
            throw new InvalidOperationException();
        }
    }
}

