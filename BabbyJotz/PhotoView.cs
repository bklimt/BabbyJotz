using System;

using Xamarin.Forms;

namespace BabbyJotz {
    public class PhotoView : View {
        public static readonly BindableProperty BytesProperty =
            BindableProperty.Create<PhotoView, byte[]>(p => p.Bytes, null);
        public byte[] Bytes {
            get { return (byte[])base.GetValue(BytesProperty); }
            set { SetValue(BytesProperty, value); }
        }

        public static readonly BindableProperty GradientYProperty =
            BindableProperty.Create<PhotoView, double>(p => p.GradientY, -1);
        public double GradientY {
            get { return (double)base.GetValue(GradientYProperty); }
            set { SetValue(GradientYProperty, value); }
        }

        public PhotoView() {
        }
    }
}

