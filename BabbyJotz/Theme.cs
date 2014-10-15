using System;

using Xamarin.Forms;

namespace BabbyJotz {
    public class Theme :BindableObject {
        public static readonly Theme Light = new Theme {
            Title = Color.FromHex("ffddff"),
            IosBackground = Color.FromHex("ccffff"),
            AndroidBackground = Color.FromHex("efffff"),
            ListBackground = Color.White,
            Text = Color.Black
        };

        public static readonly Theme Dark = new Theme {
            Title = Color.FromHex("002200"),
            IosBackground = Color.FromHex("330000"),
            AndroidBackground = Color.FromHex("330000"),
            ListBackground = Color.Black,
            Text = Color.White
        };

        public Color Title { get; private set; }
        private Color IosBackground { get; set; }
        private Color AndroidBackground { get; set; }
        public Color ListBackground { get; private set; }
        public Color Text { get; private set; }

        public Color Background {
            get {
                if (Device.OS == TargetPlatform.iOS) {
                    return IosBackground;
                } else {
                    return AndroidBackground;
                }
            }
        }

        private Theme() {
        }
    }
}

