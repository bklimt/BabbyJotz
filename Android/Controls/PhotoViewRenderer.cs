using System;
using System.ComponentModel;
using System.Threading.Tasks;

using Android.App;
using Android.Gms.Ads;
using Android.Graphics;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;

using Xamarin.Forms;
using Xamarin.Forms.Platform.Android;

using BabbyJotz;

[assembly: Xamarin.Forms.ExportRenderer(typeof(PhotoView), typeof(BabbyJotz.Android.PhotoViewRenderer))]
namespace BabbyJotz.Android {
    public class PhotoViewRenderer : ViewRenderer<PhotoView, NativePhotoView> {
        private void SetBytes(byte[] bytes) {
            if (bytes != null) {
                Task.Run(async () => {
                    var bitmap = await BitmapFactory.DecodeByteArrayAsync(bytes, 0, bytes.Length);
                    var handler = new Handler(Looper.MainLooper);
                    handler.Post(() => {
                        Control.SetImageBitmap(bitmap);
                    });
                });
            } else {
                Control.SetImageResource(Resource.Drawable.ic_launcher);
            }
        }

        protected override void OnElementChanged(ElementChangedEventArgs<PhotoView> e) {
            base.OnElementChanged(e);
            var view = new NativePhotoView(Context);
            view.SetGradientY(e.NewElement.GradientY);
            view.Click += (object sender, EventArgs args) => {
                Element.NotifyTapped(sender, args);
            };
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

