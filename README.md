BabbyJotz
=========

A Xamarin app for noting baby information. This app was hacked together in my spare time, but it does show off lots of useful techniques for mobile developers.

* Xamarin Forms with a [Master Detail Page](BabbyJotz/Pages/MainPage.xaml) and several Navigation Pages.
* Storing data locally with [Mono.Data.Sqlite](iOS/Shared/LocalStore.cs).
* Syncing data to the cloud with [Facebook's Parse](iOS/Shared/ParseStore.cs).
    * Custom [analytics](parse/cloud/logging.js).
    * Push for notifications and live data updates in [iOS](iOS/AppDelegate.cs) and [Android](Android/BabbyJotzIntentService.cs).
    * [Config](iOS/Shared/ParseStore.cs) for remotely disabling analytics.
    * Scripts for [data migrations](scripts/fix_dates.js) using node.js.
* Parse [Cloud Code](parse/cloud) for security enforcement.
    * The Parse Image Cloud Module for [resizing uploaded photos](parse/cloud/photo.js) to thumbnail size.
    * Role-based Access Controls for [securely sharing babies](parse/cloud/baby.js) between users.
* Ads served by Google's AdMob.
    * A [custom view](BabbyJotz/AdMobBanner.cs) with renderers for [iOS](iOS/Controls/AdMobBannerRenderer.cs) and [Android](Android/Controls/AdMobBannerRenderer.cs).
