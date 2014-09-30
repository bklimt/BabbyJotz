using System;
using System.Collections.Generic;
using System.Linq;
using MonoTouch.Foundation;
using MonoTouch.UIKit;
using Parse;
using Xamarin.Forms;

namespace BabbyJotz.iOS {
	[Register("AppDelegate")]
	public partial class AppDelegate : UIApplicationDelegate {
		UIWindow window;

		public override bool FinishedLaunching(UIApplication app, NSDictionary options) {
			Forms.Init();

			ParseClient.Initialize(
				"dRJrkKFywmUEYJx10K96Sw848juYyFF01Zlno6Uf",
				"0ICNGpRDtEswmZw8E3nfS08W8RNWbFLExIIw2IvS");
			var cloudStore = new CloudStore();

			window = new UIWindow(UIScreen.MainScreen.Bounds);

			window.RootViewController = App.GetMainPage(cloudStore).CreateViewController();
			window.MakeKeyAndVisible();
			
			return true;
		}
	}
}

