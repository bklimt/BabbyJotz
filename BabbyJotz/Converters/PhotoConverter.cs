using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;

using Xamarin.Forms;

namespace BabbyJotz {
    public class PhotoConverter : IValueConverter {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture) {
            if (targetType != typeof(ImageSource)) {
                throw new InvalidOperationException();
            }
            if (value is Photo && ((Photo)value).Bytes != null) {
                var photo = (Photo)value;
                return ImageSource.FromStream(() => {
                    try {
                        return new MemoryStream(photo.Bytes);
                    } catch (Exception e) {
                        Debug.WriteLine("Got exception while creating photo memory stream: {0}", e);
                        throw;
                    }
                });
            }
            if (Device.OS == TargetPlatform.Android) {
                return ImageSource.FromFile("ic_launcher.png");
            } else {
                return ImageSource.FromFile("Icon-76.png");
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) {
            throw new InvalidOperationException();
        }
    }
}

