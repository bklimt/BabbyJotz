using System;

using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using Android.OS;

using Parse;

using Xamarin.Forms.Platform.Android;

namespace BabbyJotz.Android {
	[Activity(Label = "Babby Jotz",
		MainLauncher = true,
		Theme = "@style/BabbyTheme",
		ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation)]
	public class MainActivity : AndroidActivity {
		protected override void OnCreate(Bundle bundle) {
			base.OnCreate(bundle);

			Xamarin.Forms.Forms.Init(this, bundle);

			ParseClient.Initialize(
				"dRJrkKFywmUEYJx10K96Sw848juYyFF01Zlno6Uf",
				"0ICNGpRDtEswmZw8E3nfS08W8RNWbFLExIIw2IvS");
			var cloudStore = new BabbyJotz.iOS.CloudStore();
			var localStore = new BabbyJotz.iOS.LocalStore(cloudStore);

			SetPage(App.GetMainPage(localStore));
		}
	}
}

