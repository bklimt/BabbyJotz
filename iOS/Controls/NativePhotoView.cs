using System;
using System.Drawing;

using MonoTouch.CoreGraphics;
using MonoTouch.UIKit;

namespace BabbyJotz.iOS {
    public class NativePhotoView : UIView {
        private UIImage image;
        private double gradientY = -1;

        public NativePhotoView() {
        }

        public NativePhotoView(IntPtr handle) : base(handle) {
        }

        public void SetGradientY(double y) {
            gradientY = y;
            SetNeedsDisplay();
        }

        public void SetImage(UIImage newImage) {
            image = newImage;
            SetNeedsDisplay();
        }

        public override void Draw(RectangleF dirtyRect) {
            var context = UIGraphics.GetCurrentContext();
            var drawingRect = Bounds;

            // TODO: Well, that's a gross hack.
            context.SetFillColorWithColor(new CGColor(0.2f, 1.0f));
            context.FillRect(drawingRect);

            if (image != null) {
                // This logic makes the image fill the control, while maintaining the aspect ratio.
                var ratio = drawingRect.Width / drawingRect.Height;
                var x = 0.0f;
                var y = 0.0f;
                var expectedWidthForHeight = image.CGImage.Height * ratio;
                var expectedHeightForWidth = image.CGImage.Width / ratio;
                if (image.CGImage.Width > expectedWidthForHeight) {
                    x = (image.CGImage.Width - expectedWidthForHeight) / 2.0f;
                } else if (image.CGImage.Height > expectedHeightForWidth) {
                    y = (image.CGImage.Height - expectedHeightForWidth) / 2.0f;
                }
                var src = new RectangleF(
                    x, y, (image.CGImage.Width - x * 2), (image.CGImage.Height - y * 2));
                var cropped = UIImage.FromImage(image.CGImage.WithImageInRect(src));
                cropped.Draw(drawingRect);
            }

            // Draw the gradient over the bottom.
            if (gradientY >= 0) {
                var colorspace = CGColorSpace.CreateDeviceRGB();
                var gradient = new CGGradient(colorspace, new CGColor[] {
                    new CGColor(0.2f, 0.0f),
                    new CGColor(0.2f, 1.0f),
                });
                context.DrawLinearGradient(
                    gradient,
                    new PointF(drawingRect.Left, drawingRect.Top + (float)gradientY * drawingRect.Height),
                    new PointF(drawingRect.Left, drawingRect.Bottom),
                    CGGradientDrawingOptions.DrawsAfterEndLocation);
            }
        }
    }
}

