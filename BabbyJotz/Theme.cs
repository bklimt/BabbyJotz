using System;

using Xamarin.Forms;

namespace BabbyJotz {
    // TODO: Add button text color that's blue or orange.
    public class Theme :BindableObject {
        public static readonly Theme Light = new Theme {
            Title = Color.FromHex("ffddff"),
            IosBackground = Color.FromHex("ccffff"),
            AndroidBackground = Color.FromHex("efffff"),
            ListBackground = Color.White,
            Text = Color.FromHex("333333"),
            ButtonText = Color.FromHex("007aff")
        };

        public static readonly Theme Dark = new Theme {
            Title = Color.FromHex("1E6E39"),
            IosBackground = Color.FromHex("333333"),
            AndroidBackground = Color.FromHex("330000"),
            ListBackground = Color.Black,
            Text = Color.White,
            ButtonText = Color.FromHex("ff8500")
        };

        public Color Title { get; private set; }
        private Color IosBackground { get; set; }
        private Color AndroidBackground { get; set; }
        public Color ListBackground { get; private set; }
        public Color Text { get; private set; }
        public Color ButtonText { get; private set; }

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

