using System;
using System.Drawing;

using Xamarin.Forms;
using Xamarin.Forms.Platform.iOS;
using GoogleAdMobAds;
using MonoTouch.UIKit;

using BabbyJotz;
using BabbyJotz.iOS;

[assembly: ExportRenderer(typeof(AdMobBanner), typeof(AdMobBannerRenderer))]
namespace BabbyJotz.iOS {
    public class AdMobBannerRenderer : ViewRenderer {
        const string AdMobId = "ca-app-pub-8697950901186247/2337591611";

        protected override void OnElementChanged(ElementChangedEventArgs<View> e) {
            base.OnElementChanged(e);

            var view = new GADBannerView(size: GADAdSizeCons.Banner, origin: new PointF(0, 0)) {
                AdUnitID = AdMobId,
                RootViewController = UIApplication.SharedApplication.Windows[0].RootViewController
            };

            view.DidReceiveAd += (sender, args) => {
                // Nothing to do right now.
            };

            view.LoadRequest(GADRequest.Request);
            base.SetNativeControl(view);
        }
    }
}