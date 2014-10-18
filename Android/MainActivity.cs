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
    [Activity(
        Label = "Babby Jotz",
        Theme = "@style/BabbyTheme",
        ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation)]
    public class MainActivity : AndroidActivity {
        protected override void OnCreate(Bundle bundle) {
            base.OnCreate(bundle);
            Xamarin.Forms.Forms.Init(this, bundle);
            var app = (BabbyJotzApplication)this.Application;
            SetPage(App.GetMainPage(app.RootViewModel));
        }
    }
}

