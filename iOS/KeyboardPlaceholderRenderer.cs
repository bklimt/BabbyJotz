using System;
using System.ComponentModel;
using System.Threading;
using System.Threading.Tasks;
using Xamarin.Forms;
using Xamarin.Forms.Platform.iOS;
using MonoTouch.Foundation;
using MonoTouch.CoreGraphics;
using MonoTouch.UIKit;
using BabbyJotz;
using BabbyJotz.iOS;

[assembly: ExportRenderer(typeof(KeyboardPlaceholderView), typeof(KeyboardPlaceholderRenderer))]
namespace BabbyJotz.iOS {
	public class KeyboardPlaceholderRenderer : BoxRenderer {
		private CancellationTokenSource cancellationToken;

		public KeyboardPlaceholderRenderer() {
			NSNotificationCenter.DefaultCenter.AddObserver(UIKeyboard.DidShowNotification, (notification) => {
				var rectValue = (NSValue)notification.UserInfo[UIKeyboard.FrameBeginUserInfoKey];
				var rect = rectValue.CGRectValue;
				Element.HeightRequest = rect.Height;
				// ResizeAsync(rect.Height, 30, 5);
			});
			NSNotificationCenter.DefaultCenter.AddObserver(UIKeyboard.WillHideNotification, (notification) => {
				// Element.HeightRequest = 0;
				ResizeAsync(0, 30, 10);
			});
		}

		private async void ResizeAsync(double newSize, int delay, int steps) {
			double originalSize = Element.Height;

			if (cancellationToken != null) {
				cancellationToken.Cancel();
			}
			cancellationToken = new CancellationTokenSource();
			var myToken = cancellationToken;
			for (int i = 0; i < steps; i++) {
				double currentSize = originalSize + ((newSize - originalSize) * i) / steps;
				Element.HeightRequest = currentSize;
				await Task.Delay(delay);
				if (myToken.IsCancellationRequested) {
					return;
				}
			}
			Element.HeightRequest = newSize;
		}

		protected override void OnElementChanged(ElementChangedEventArgs<BoxView> e) {
			base.OnElementChanged(e);
		}

		protected override void OnElementPropertyChanged(object sender, PropertyChangedEventArgs e) {
			base.OnElementPropertyChanged(sender, e);
		}
	}
}

