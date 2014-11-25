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
        private void ReportException(Exception e) {
            try {
                // This might just not work at all, but meh.
                var app = (BabbyJotzApplication)Context.ApplicationContext;
                var model = app.RootViewModel;
                model.CloudStore.LogException("PhotoViewRenderer", e);
            } catch (Exception) {
                // Well, we tried.
            }
        }

        private void SetBytes(byte[] bytes) {
            if (bytes != null) {
                Task.Run(async () => {
                    if (bytes == null) {
                        throw new InvalidOperationException("Bytes was null when it shouldn't be possible.");
                    }
                    Bitmap bitmap = null;
                    try {
                        bitmap = await BitmapFactory.DecodeByteArrayAsync(bytes, 0, bytes.Length);
                    } catch (Exception e) {
                        ReportException(e);
                        throw new AggregateException("Unable to decode byte array.", e);
                    }
                    var looper = Looper.MainLooper;
                    if (looper == null) {
                        throw new AggregateException("MainLooper is null.");
                    }
                    Handler handler = null;
                    try {
                        handler = new Handler(looper);
                    } catch (Exception e) {
                        ReportException(e);
                        throw new AggregateException("Unable to create Handler.", e);
                    }
                    try {
                        handler.Post(() => {
                            if (Control != null && bitmap != null) {
                                try {
                                    Control.SetImageBitmap(bitmap);
                                } catch (Exception e) {
                                    ReportException(e);
                                    throw new AggregateException("Unable to set image bitmap.", e);
                                }
                            }
                        });
                    } catch (Exception e) {
                        ReportException(e);
                        throw new AggregateException("Unable to post to Handler.", e);
                    }
                });
            } else {
                if (Control != null) {
                    Control.SetImageResource(Resource.Drawable.ic_launcher);
                }
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

