using System;
using System.Globalization;
using Xamarin.Forms;

namespace BabbyJotz {
	public class NotConverter : IValueConverter {
		public object Convert(object value, Type targetType, object parameter, CultureInfo culture) {
			if (value is Boolean) {
				return !(bool)value;
			}
			return value;
		}

		public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) {
			if (value is Boolean) {
				return !(bool)value;
			}
			return value;
		}
	}
}

