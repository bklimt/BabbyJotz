using System;

using Android.App;
using Android.Runtime;
using Android.Views;
using Android.Gms.Ads;

using Xamarin.Forms.Platform.Android;

using BabbyJotz;
using BabbyJotz.Android;

[assembly: Xamarin.Forms.ExportRenderer(typeof(AdMobBanner), typeof(AdMobBannerRenderer))]
namespace BabbyJotz.Android {
    public class AdMobBannerRenderer : ViewRenderer {
        const string AdMobId = "ca-app-pub-8697950901186247/5657658019";

        protected override void OnElementChanged(ElementChangedEventArgs<Xamarin.Forms.View> e) {
            base.OnElementChanged(e);

            var view = new AdView(base.Context);
            view.AdSize = AdSize.Banner;
            view.AdUnitId = AdMobId;
            var request = new AdRequest.Builder().Build();
            view.LoadAd(request);

            base.SetNativeControl(view);
        }
    }
}

