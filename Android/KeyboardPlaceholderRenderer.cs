using System;
using System.ComponentModel;
using System.Threading;
using System.Threading.Tasks;
using Xamarin.Forms;
using Xamarin.Forms.Platform.Android;
using BabbyJotz;
using BabbyJotz.Android;

// TODO: Get rid of this.
[assembly: ExportRenderer(typeof(KeyboardPlaceholderView), typeof(KeyboardPlaceholderRenderer))]
namespace BabbyJotz.Android {
	public class KeyboardPlaceholderRenderer : BoxRenderer {
		public KeyboardPlaceholderRenderer() {
		}

		protected override void OnElementChanged(ElementChangedEventArgs<BoxView> e) {
			base.OnElementChanged(e);
		}

		protected override void OnElementPropertyChanged(object sender, PropertyChangedEventArgs e) {
			base.OnElementPropertyChanged(sender, e);
		}
	}
}

