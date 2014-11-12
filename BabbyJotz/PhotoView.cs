using System;

using Xamarin.Forms;

namespace BabbyJotz {
    public class PhotoView : View {
        private event EventHandler HandleTapped;

        public event EventHandler Tapped {
            add {
                HandleTapped += value;
                HandlesTaps = true;
            }
            remove {
                HandleTapped -= value;
                if (HandleTapped == null) {
                    HandlesTaps = false;
                }
            }
        }

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

        public static readonly BindableProperty HandlesTapsProperty =
            BindableProperty.Create<PhotoView, bool>(p => p.HandlesTaps, false);
        public bool HandlesTaps {
            get { return (bool)base.GetValue(HandlesTapsProperty); }
            private set { SetValue(HandlesTapsProperty, value); }
        }

        public void NotifyTapped(object sender, EventArgs e) {
            if (HandleTapped != null) {
                HandleTapped(sender, e);
            }
        }

        public PhotoView() {
        }
    }
}

