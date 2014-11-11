using System;
using System.ComponentModel;
using System.Drawing;
using System.Reflection;
using System.Threading.Tasks;

using Xamarin.Forms;
using Xamarin.Forms.Platform.iOS;
using MonoTouch.CoreFoundation;
using MonoTouch.CoreGraphics;
using MonoTouch.Foundation;
using MonoTouch.UIKit;

using BabbyJotz;
using BabbyJotz.iOS;

[assembly: ExportRenderer(typeof(PhotoView), typeof(PhotoViewRenderer))]
namespace BabbyJotz.iOS {
    public class PhotoViewRenderer : ViewRenderer<PhotoView, NativePhotoView> {
        private void SetBytes(byte[] bytes) {
            if (bytes != null) {
                Task.Run(async () => {
                    var cg = CGImage.FromJPEG(
                        new CGDataProvider(bytes, 0, bytes.Length),
                        null,
                        true,
                        CGColorRenderingIntent.Default);
                    DispatchQueue.MainQueue.DispatchAsync(() => {
                        var image = UIImage.FromImage(cg);
                        Control.SetImage(image);
                    });
                });
            } else {
                var image = UIImage.FromFile("Icon-76.png");
                Control.SetImage(image);
            }
        }

        protected override void OnElementChanged(ElementChangedEventArgs<PhotoView> e) {
            base.OnElementChanged(e);
            var view = new NativePhotoView();
            view.SetGradientY(e.NewElement.GradientY);
            SetNativeControl(view);
            SetBytes(e.NewElement.Bytes);
        }

        protected override void OnElementPropertyChanged(object sender, PropertyChangedEventArgs e) {
            base.OnElementPropertyChanged(sender, e);
            if (Element == null || Control == null) {
                return;
            }
            if (e.PropertyName == PhotoView.BytesProperty.PropertyName) {
                SetBytes(Element.Bytes);
            } else if (e.PropertyName == PhotoView.GradientYProperty.PropertyName) {
                Control.SetGradientY(Element.GradientY);
            }
        }
    }
}

