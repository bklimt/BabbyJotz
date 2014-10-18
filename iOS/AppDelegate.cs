using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using MonoTouch.Foundation;
using MonoTouch.UIKit;
using Parse;
using Xamarin.Forms;
using Xamarin.Forms.Platform.iOS;

namespace BabbyJotz.iOS {
    [Register("AppDelegate")]
    public partial class AppDelegate : UIApplicationDelegate {
        UIWindow window;
        RootViewModel model;

        private void UpdateForTheme(Theme theme) {
            UITabBar.Appearance.BarTintColor = theme.Title.ToUIColor();
            UITabBar.Appearance.TintColor = theme.Text.ToUIColor();
            // TODO: Does this even do anything? The unselected icons are invisible.
            UITabBar.Appearance.BackgroundColor = theme.Background.ToUIColor();

            UINavigationBar.Appearance.TintColor = theme.ButtonText.ToUIColor();
            UINavigationBar.Appearance.BackgroundColor = theme.Background.ToUIColor();

            if (theme == Theme.Dark) {
                UIApplication.SharedApplication.StatusBarStyle =
                    UIStatusBarStyle.Default;
            } else {
                UIApplication.SharedApplication.StatusBarStyle =
                    UIStatusBarStyle.LightContent;
            }
        }

        public override bool FinishedLaunching(UIApplication app, NSDictionary options) {
            window = new UIWindow(UIScreen.MainScreen.Bounds);

            Forms.Init();

            ParseClient.Initialize(
                "dRJrkKFywmUEYJx10K96Sw848juYyFF01Zlno6Uf",
                "0ICNGpRDtEswmZw8E3nfS08W8RNWbFLExIIw2IvS");
            var cloudStore = new CloudStore();
            var localStore = new LocalStore(cloudStore);

            model = new RootViewModel(localStore, new Preferences());
            model.PropertyChanged += (object sender, PropertyChangedEventArgs e) => {
                if (e.PropertyName == "Theme") {
                    UpdateForTheme(model.Theme);
                }
            };
            UpdateForTheme(model.Theme);

            window.RootViewController = App.GetMainPage(model).CreateViewController();
            window.MakeKeyAndVisible();

            return true;
        }

        public override void DidRegisterUserNotificationSettings(
            UIApplication application, UIUserNotificationSettings notificationSettings) {
            // NOTE: Don't call the base implementation on a Model class
            // see http://docs.xamarin.com/guides/ios/application_fundamentals/delegates,_protocols,_and_events
            // UIApplication.SharedApplication.RegisterForRemoteNotifications();
        }

        public override async void RegisteredForRemoteNotifications(
            UIApplication application, NSData deviceToken) {
            // NOTE: Don't call the base implementation on a Model class
            // see http://docs.xamarin.com/guides/ios/application_fundamentals/delegates,_protocols,_and_events
            try {
                var userDefaultsKey = "ParseInstallationObjectId";
                var objectId = NSUserDefaults.StandardUserDefaults.StringForKey(userDefaultsKey);

                // TODO: Move this logic into CloudStore.
                var obj = (objectId == null)
                    ? ParseObject.Create("_Installation")
                    : ParseObject.CreateWithoutData("_Installation", objectId);
                // From https://groups.google.com/forum/#!topic/parse-developers/pPatDDkzcEc
                // And https://gist.github.com/gfosco/a526cbc3061398d50e8b
                string dt = deviceToken.ToString().Replace("<", "").Replace(">", "").Replace(" ", "");
                obj["deviceToken"] = dt;
                obj["deviceType"] = "ios";
                obj["appIdentifier"] = "com.bklimt.BabbyJotz";
                // obj["timeZone"] = TimeZone.CurrentTimeZone.ToString();
                obj["appName"] = "BabbyJotz";
                obj["appVersion"] = "1.0.0";
                obj["parseVersion"] = "1.3.0";
                obj["userId"] = model.CloudUserId;
                await obj.SaveAsync();

                NSUserDefaults.StandardUserDefaults.SetString(userDefaultsKey, obj.ObjectId);
            } catch (Exception e) {
                Console.WriteLine("Unable to save device token. {0}", e);
            }
        }

        public override void FailedToRegisterForRemoteNotifications(
            UIApplication application, NSError error) {
            new UIAlertView("Error registering for push notifications.",
                error.LocalizedDescription, null, "OK", null).Show();
        }

        public override void DidReceiveRemoteNotification(
            UIApplication application, NSDictionary userInfo, Action<UIBackgroundFetchResult> completionHandler) {
            // NOTE: Don't call the base implementation on a Model class
            // see http://docs.xamarin.com/guides/ios/application_fundamentals/delegates,_protocols,_and_events
            model.TryToSyncEventually();
        }

        public override void HandleAction(
            UIApplication application, string actionIdentifier, NSDictionary remoteNotificationInfo, Action completionHandler) {
            // NOTE: Don't call the base implementation on a Model class
            // see http://docs.xamarin.com/guides/ios/application_fundamentals/delegates,_protocols,_and_events
            model.TryToSyncEventually();
        }
    }
}

