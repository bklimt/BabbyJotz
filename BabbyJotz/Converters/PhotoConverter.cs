using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;

using Xamarin.Forms;

namespace BabbyJotz {
    public class PhotoConverter : IValueConverter {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture) {
            if (targetType == typeof(byte[])) {
                if (value == null) {
                    return null;
                }
                if (value is Photo) {
                    return ((Photo)value).Bytes;
                }
                throw new InvalidOperationException("Invalid value for photo with target type bytes.");
            }

            if (targetType != typeof(ImageSource)) {
                throw new InvalidOperationException("Invalid target type for photo.");
            }
            if (value is Photo && ((Photo)value).Bytes != null) {
                var photo = (Photo)value;
                return ImageSource.FromStream(() => {
                    Debug.WriteLine(
                        String.Format("Creating image source stream from photo {0} with {1} bytes.",
                            photo.Uuid, photo.Bytes.Length));
                    try {
                        return new MemoryStream(photo.Bytes);
                    } catch (Exception e) {
                        Debug.WriteLine("Got exception while creating photo memory stream: {0}", e);
                        throw;
                    }
                });
            }
            if (Device.OS == TargetPlatform.Android) {
                Debug.WriteLine("Creating image source for default icon in Android.");
                return ImageSource.FromFile("ic_launcher.png");
            } else {
                Debug.WriteLine("Creating image source for default icon in iOS.");
                return ImageSource.FromFile("Icon-76.png");
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) {
            throw new InvalidOperationException();
        }
    }
}

