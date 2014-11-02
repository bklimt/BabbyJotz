using System;
using System.Globalization;
using System.IO;

using Xamarin.Forms;

namespace BabbyJotz {
    public class PhotoConverter : IValueConverter {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture) {
            if (targetType != typeof(ImageSource)) {
                throw new InvalidOperationException();
            }
            if (value == null) {
                if (Device.OS == TargetPlatform.Android) {
                    return ImageSource.FromFile("ic_launcher.png");
                } else {
                    return ImageSource.FromFile("Icon-76.png");
                }
            }
            if (value is Photo) {
                var photo = (Photo)value;
                return ImageSource.FromStream(() => new MemoryStream(photo.Bytes));
            }
            throw new InvalidOperationException();
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) {
            throw new InvalidOperationException();
        }
    }
}

