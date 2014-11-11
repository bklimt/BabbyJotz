
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Android.App;
using Android.Content;
using Android.Graphics;
using Android.OS;
using Android.Runtime;
using Android.Util;
using Android.Views;
using Android.Widget;

namespace BabbyJotz.Android {
    public class NativePhotoView : View {
        private Bitmap bitmap;
        private double gradientY = -1;

        public NativePhotoView(Context context) : base(context) {
            Initialize();
        }

        public NativePhotoView(Context context, IAttributeSet attrs) : base(context, attrs) {
            Initialize();
        }

        public NativePhotoView(Context context, IAttributeSet attrs, int defStyle) : base(context, attrs, defStyle) {
            Initialize();
        }

        void Initialize() {
        }

        public void SetImageBitmap(Bitmap newBitmap) {
            bitmap = newBitmap;
            Invalidate();
        }

        public void SetImageResource(int res) {
            Task.Run(async () => {
                var newBitmap = await BitmapFactory.DecodeResourceAsync(Resources, Resource.Drawable.ic_launcher);
                var handler = new Handler(Looper.MainLooper);
                handler.Post(() => {
                    SetImageBitmap(newBitmap);
                });
            });
        }

        public void SetGradientY(double y) {
            gradientY = y;
            Invalidate();
        }

        public override void Draw(Canvas canvas) {
            base.Draw(canvas);
            var paint = new Paint(PaintFlags.FilterBitmap);

            var drawingRect = new Rect();
            GetDrawingRect(drawingRect);

            if (bitmap != null) {
                // This logic makes the bitmap fill the control, while maintaining the aspect ratio.
                var ratio = (double)drawingRect.Width() / drawingRect.Height();
                var x = 0.0;
                var y = 0.0;
                var expectedWidthForHeight = bitmap.Height * ratio;
                var expectedHeightForWidth = bitmap.Width / ratio;
                if (bitmap.Width > expectedWidthForHeight) {
                    x = (bitmap.Width - expectedWidthForHeight) / 2.0;
                } else if (bitmap.Height > expectedHeightForWidth) {
                    y = (bitmap.Height - expectedHeightForWidth) / 2.0;
                }
                var src = new Rect((int)x, (int)y, (int)(bitmap.Width - x), (int)(bitmap.Height - y));

                canvas.DrawBitmap(bitmap, src, drawingRect, paint);

                if (gradientY >= 0) {
                    var gradient = new LinearGradient(
                        drawingRect.Left,
                        drawingRect.Top + (float)gradientY * (drawingRect.Bottom - drawingRect.Top),
                        drawingRect.Left,
                        drawingRect.Bottom,
                        new Color(0x33, 0x33, 0x33, 0x00),
                        new Color(0x33, 0x33, 0x33, 0xFF),
                        Shader.TileMode.Clamp);
                    var p = new Paint();
                    p.SetShader(gradient);
                    p.Dither = true;
                    canvas.DrawRect(drawingRect, p);
                }
            }
        }
    }
}

