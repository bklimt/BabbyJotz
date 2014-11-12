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
        private UIGestureRecognizer tappedGestureRecognizer;

        private void SetBytes(NativePhotoView view, byte[] bytes) {
            if (bytes != null) {
                Task.Run(() => {
                    var cg = CGImage.FromJPEG(
                        new CGDataProvider(bytes, 0, bytes.Length),
                        null,
                        true,
                        CGColorRenderingIntent.Default);
                    DispatchQueue.MainQueue.DispatchAsync(() => {
                        var image = UIImage.FromImage(cg);
                        view.SetImage(image);
                    });
                });
            } else {
                var image = UIImage.FromFile("ic_launcher.png");
                view.SetImage(image);
            }
        }

        private void SetHandlesTaps(NativePhotoView view, bool handlesTaps) {
            if (handlesTaps) {
                if (view.GestureRecognizers != null) {
                    int index = Array.IndexOf(view.GestureRecognizers, tappedGestureRecognizer);
                    if (index >= 0) {
                        return;
                    }
                }
                view.AddGestureRecognizer(tappedGestureRecognizer);
            } else {
                view.RemoveGestureRecognizer(tappedGestureRecognizer);
            }
        }

        public PhotoViewRenderer() {
            tappedGestureRecognizer =
                new UITapGestureRecognizer(() => {
                    Element.NotifyTapped(this, EventArgs.Empty);
                });
        }

        protected override void OnElementChanged(ElementChangedEventArgs<PhotoView> e) {
            base.OnElementChanged(e);
            var view = new NativePhotoView();
            SetHandlesTaps(view, e.NewElement.HandlesTaps);
            view.SetGradientY(e.NewElement.GradientY);
            SetBytes(view, e.NewElement.Bytes);
            SetNativeControl(view);
        }

        protected override void OnElementPropertyChanged(object sender, PropertyChangedEventArgs e) {
            base.OnElementPropertyChanged(sender, e);
            if (Element == null || Control == null) {
                return;
            }
            if (e.PropertyName == PhotoView.BytesProperty.PropertyName) {
                SetBytes(Control, Element.Bytes);
            } else if (e.PropertyName == PhotoView.GradientYProperty.PropertyName) {
                Control.SetGradientY(Element.GradientY);
            } else if (e.PropertyName == PhotoView.HandlesTapsProperty.PropertyName) {
                SetHandlesTaps(Control, Element.HandlesTaps);
            }
        }
    }
}

