using System;
using System.Globalization;
using Xamarin.Forms;

namespace BabbyJotz {
	public class NullConverter : IValueConverter {
		public object Convert(object value, Type targetType, object parameter, CultureInfo language) {      
			return value == null;     
		}

		public object ConvertBack(object value, Type targetType, object parameter, CultureInfo language) {      
			throw new NotImplementedException();      
		}
	}
}

